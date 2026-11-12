using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Http;
using Microsoft.Extensions.Logging;

namespace Cisco.Iq.Api.Test.V1;

public partial class PipelineTests
{
	private const string EmptyPage = "{\"items\":[{\"id\":\"a\"}]}";

	[Fact]
	public async Task Request_CarriesKeyRegionAndTraceHeaders()
	{
		HttpRequestMessage? seen = null;
		using var client = V1Support.Client(request =>
		{
			seen = request;
			return V1Support.Json(EmptyPage);
		});

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();

		response.Content!.Items![0].Id.Should().Be("a");
		seen!.Method.Should().Be(HttpMethod.Get);
		seen.RequestUri!.AbsoluteUri.Should().Be("https://iq.cisco.com/api/asset/v1/physicalAssets");
		seen.Headers.Authorization!.Scheme.Should().Be("Bearer");
		seen.Headers.Authorization.Parameter.Should().Be(V1Support.ApiKey);
		seen.Headers.GetValues("X-IQ-Region").Should().Equal("EMEA");
		seen.Headers.Accept.Single().MediaType.Should().Be("application/json");
		seen.Headers.UserAgent.ToString().Should().StartWith("Cisco.Iq.Api/");
		TraceparentPattern().IsMatch(seen.Headers.GetValues("traceparent").Single()).Should().BeTrue();
	}

	[Fact]
	public async Task Request_OmitsRegionHeaderWhenDisabledAndHonoursBaseAddressAndUserAgent()
	{
		HttpRequestMessage? seen = null;
		using var client = V1Support.Client(
			request =>
			{
				seen = request;
				return V1Support.Json(EmptyPage);
			},
			o =>
			{
				o.SendRegionHeader = false;
				o.BaseAddress = new Uri("https://eu-central-1.iq.cisco.com");
				o.UserAgent = "Contoso/2.0";
			});

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();

		seen!.Headers.Contains("X-IQ-Region").Should().BeFalse();
		seen.RequestUri!.Host.Should().Be("eu-central-1.iq.cisco.com");
		seen.Headers.UserAgent.ToString().Should().Be("Contoso/2.0");
	}

	[Fact]
	public async Task RequestHandler_KeepsACallerSuppliedTraceparent()
	{
		HttpRequestMessage? seen = null;
		var handler = new CiscoIqV1RequestHandler(V1Support.Options())
		{
			InnerHandler = new TestTransport((request, _) =>
			{
				seen = request;
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			})
		};
		using var invoker = new HttpMessageInvoker(handler);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://iq.cisco.com/api/x");
		const string Trace = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
		request.Headers.Add("traceparent", Trace);

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		seen!.Headers.GetValues("traceparent").Should().Equal(Trace);
	}

	[Fact]
	public async Task Request_AcceptsAQueryOfExactlyTheLimitAndRefusesOneByteMore()
	{
		var sent = 0;
		using var client = V1Support.Client(_ =>
		{
			sent++;
			return V1Support.Json(EmptyPage);
		});

		using var ok = await client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest { Filter = new string('a', 2041) });
		Func<Task> tooLong = () => client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest { Filter = new string('a', 2042) });

		(await tooLong.Should().ThrowAsync<CiscoIqV1QueryTooLongException>()).Which.Length.Should().Be(2049);
		sent.Should().Be(1);
	}

	[Fact]
	public async Task Request_CountsQueryBytesNotCharacters()
	{
		using var client = V1Support.Client(_ => V1Support.Json(EmptyPage));
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest { Filter = new string('é', 700) });
		await act.Should().ThrowAsync<CiscoIqV1QueryTooLongException>();
	}

	[Fact]
	public async Task Request_FormatsQueryOptions()
	{
		HttpRequestMessage? seen = null;
		using var client = V1Support.Client(request =>
		{
			seen = request;
			return V1Support.Json(EmptyPage);
		});

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest
		{
			Max = 10,
			Count = true,
			OrderBy = "model",
			Inline = ["children", "coverages"]
		});

		var query = Uri.UnescapeDataString(seen!.RequestUri!.Query);
		query.Should().Contain("max=10").And.Contain("count=true").And.Contain("orderby=model").And.Contain("inline=children,coverages");
	}

	[Fact]
	public async Task Request_SendsConditionalHeaders()
	{
		HttpRequestMessage? seen = null;
		using var client = V1Support.Client(request =>
		{
			seen = request;
			return V1Support.Json("{\"id\":\"a\"}");
		});

		using var response = await client.Assets.GetAssetPhysicalAssetAsync("a", ifNoneMatch: "\"etag-1\"");

		seen!.Headers.GetValues("If-None-Match").Should().Equal("\"etag-1\"");
	}

	[Theory]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task TransientGatewayErrors_AreRetriedForReads(HttpStatusCode status)
	{
		var calls = 0;
		using var client = V1Support.Client(_ => ++calls == 1 ? new HttpResponseMessage(status) { Content = new StringContent(string.Empty) } : V1Support.Json(EmptyPage));
		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();
		calls.Should().Be(2);
	}

	[Fact]
	public async Task RetryHandler_ResendsBodiesAndOptionsAndDoesNotRetryUnsafeMethodsOnGatewayErrors()
	{
		var bodies = new List<string>();
		var seenOption = new List<object?>();
		var calls = 0;
		var handler = new CiscoIqV1RetryHandler(V1Support.Options(), _ => { }, (_, _) => Task.CompletedTask)
		{
			InnerHandler = new TestTransport(async (request, cancellationToken) =>
			{
				bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
				request.Options.TryGetValue(new HttpRequestOptionsKey<object?>("marker"), out var marker);
				seenOption.Add(marker);
				calls++;
				return calls == 1 ? Throttled() : new HttpResponseMessage(HttpStatusCode.OK);
			})
		};
		using var invoker = new HttpMessageInvoker(handler);
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://iq.cisco.com/api/x") { Content = new StringContent("{\"a\":1}", Encoding.UTF8, "application/json") };
		request.Options.Set(new HttpRequestOptionsKey<object?>("marker"), "m");

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		bodies.Should().Equal("{\"a\":1}", "{\"a\":1}");
		seenOption.Should().Equal("m", "m");

		var gateway = 0;
		var postHandler = new CiscoIqV1RetryHandler(V1Support.Options(), _ => { }, (_, _) => Task.CompletedTask)
		{
			InnerHandler = new TestTransport((_, _) =>
			{
				gateway++;
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
			})
		};
		using var postInvoker = new HttpMessageInvoker(postHandler);
		using var post = new HttpRequestMessage(HttpMethod.Post, "https://iq.cisco.com/api/x");
		using var postResponse = await postInvoker.SendAsync(post, CancellationToken.None);
		gateway.Should().Be(1);
		postResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Fact]
	public async Task LastRateLimit_CapturesRawHeaders()
	{
		var calls = 0;
		using var client = V1Support.Client(_ => ++calls switch
		{
			1 => V1Support.Json(EmptyPage),
			2 => V1Support.Json(EmptyPage).WithHeader("RateLimit", "limit=10, remaining=9").WithHeader("RateLimit-Policy", "10;w=1"),
			_ => V1Support.Json(EmptyPage).WithHeader("RateLimit-Policy", "10;w=1")
		});

		client.LastRateLimit.Should().BeNull();
		using var first = await client.Assets.ListAssetPhysicalAssetsAsync();
		client.LastRateLimit.Should().BeNull();
		using var second = await client.Assets.ListAssetPhysicalAssetsAsync();
		client.LastRateLimit.Should().Be(new CiscoIqV1RateLimitInfo("limit=10, remaining=9", "10;w=1"));
		using var third = await client.Assets.ListAssetPhysicalAssetsAsync();
		client.LastRateLimit.Should().Be(new CiscoIqV1RateLimitInfo(null, "10;w=1"));
	}

	[Fact]
	public void Client_ValidatesOptionsAndLogsWithoutTheKey()
	{
		Action nullOptions = () => _ = new CiscoIqV1Client(null!);
		nullOptions.Should().Throw<ArgumentNullException>();
		Action invalid = () => _ = new CiscoIqV1Client(V1Support.Options(o => o.ApiKey = " "));
		invalid.Should().Throw<ArgumentException>();

		var logger = new CapturingLogger();
		using var client = new CiscoIqV1Client(V1Support.Options(), logger);
		using var plain = new CiscoIqV1Client(V1Support.Options());

		logger.Messages.Should().ContainSingle().Which.Should().Contain("Emea", Exactly.Once()).And.NotContain(V1Support.ApiKey);
		logger.Levels.Should().Equal(LogLevel.Debug);
		logger.EventIds.Should().HaveCount(1);
		logger.Scopes.Should().BeEmpty();
		client.Products.Should().NotBeNull();
		client.Assessments.Should().NotBeNull();
		client.Assets.Should().NotBeNull();
	}

	[Fact]
	public void Client_UsesATransportThatDoesNotFollowRedirectsOrKeepCookies()
	{
		using var transport = CiscoIqV1Client.CreateTransport();
		transport.AllowAutoRedirect.Should().BeFalse();
		transport.UseCookies.Should().BeFalse();
	}

	[Fact]
	public async Task Client_SnapshotsOptionsSoLaterChangesDoNotApply()
	{
		HttpRequestMessage? seen = null;
		var options = V1Support.Options();
		using var client = new CiscoIqV1Client(options, new TestTransport((request, _) =>
		{
			seen = request;
			return Task.FromResult(V1Support.Json(EmptyPage));
		}), null, null);
		options.ApiKey = "changed-after-construction";

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();

		seen!.Headers.Authorization!.Parameter.Should().Be(V1Support.ApiKey);
	}

	private static HttpResponseMessage Throttled()
		=> new(HttpStatusCode.TooManyRequests) { Content = new StringContent("{\"title\":\"Too Many Requests\",\"status\":429}", Encoding.UTF8, "application/json") };

	[GeneratedRegex("^00-[0-9a-f]{32}-[0-9a-f]{16}-01$")]
	private static partial Regex TraceparentPattern();

	private sealed class CapturingLogger : ILogger
	{
		internal List<string> Messages { get; } = [];

		internal List<LogLevel> Levels { get; } = [];

		internal List<EventId> EventIds { get; } = [];

		internal List<object> Scopes { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			Scopes.Add(state);
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			Levels.Add(logLevel);
			EventIds.Add(eventId);
			Messages.Add(formatter(state, exception));
		}
	}
}

internal static class ResponseExtensions
{
	internal static HttpResponseMessage With(this HttpResponseMessage response, Action<HttpResponseMessage> change)
	{
		change(response);
		return response;
	}
}
