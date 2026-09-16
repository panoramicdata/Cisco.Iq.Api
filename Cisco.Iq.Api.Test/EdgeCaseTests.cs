using System.Net;
using Cisco.Iq.Api.Data;
using Cisco.Iq.Api.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.Test;

public class EdgeCaseTests
{
	[Fact]
	public void PublicConstructor_CreatesAndDisposesDefaultPipeline_WithOptionalLogger()
	{
		using var client = new CiscoIqClient(AuthenticationTests.Options, NullLogger.Instance);
		client.LastRateLimitStatus.Should().BeNull();
	}

	[Theory]
	[InlineData(CiscoIqAccountRegion.Us, "US")]
	[InlineData(CiscoIqAccountRegion.Emea, "EMEA")]
	[InlineData(CiscoIqAccountRegion.Apjc, "APJC")]
	public async Task UserAgentOverride_IsAppliedToBothStages_AndServiceAccountUsesEmptyBody(CiscoIqAccountRegion region, string wireRegion)
	{
		var options = AuthenticationTests.Options;
		options.AccountRegion = region;
		options.AccountId = null;
		options.UserAgent = "ExampleProduct/2.0 ExampleCompany";
		using var client = new CiscoIqClient(options,
			new TestTransport((request, _) =>
			{
				request.Headers.UserAgent.ToString().Should().Be("ExampleProduct/2.0 ExampleCompany");
				request.Headers.GetValues("Cookie").Should().Equal("account_region=" + wireRegion);
				return Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}"));
			}), new TestTransport(async (request, cancellationToken) =>
			{
				request.Headers.UserAgent.ToString().Should().Be("ExampleProduct/2.0 ExampleCompany");
				request.Headers.GetValues("Cookie").Should().Equal("account_region=" + wireRegion);
				(await request.Content!.ReadAsStringAsync(cancellationToken)).Should().Be("{}");
				return TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":3600}");
			}));
		options.UserAgent = "Changed/3.0";
		options.AccountRegion = (CiscoIqAccountRegion)99;
		await client.Assets.GetAssetsAsync();
	}

	[Theory]
	[InlineData("null")]
	[InlineData("{}")]
	[InlineData("{\"accessToken\":\"\",\"expiresInSeconds\":3600}")]
	[InlineData("{\"accessToken\":\"access\",\"expiresInSeconds\":0}")]
	public async Task InvalidExchangePayload_IsAuthenticationFailure(string payload)
	{
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) => throw new InvalidOperationException("Product call must not run.")),
			new TestTransport((_, _) => Task.FromResult(TestTransport.Json(payload))));
		Func<Task> act = () => client.Assets.GetAssetsAsync();
		await act.Should().ThrowAsync<CiscoIqAuthenticationException>();
	}

	[Fact]
	public async Task ExchangeFailure_DoesNotRecurse_AndHeaderOnlyTrackingIdIsPreserved()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) => throw new InvalidOperationException("Product call must not run.")),
			new TestTransport((_, _) =>
			{
				calls++;
				var response = TestTransport.Json("<html>Forbidden</html>", HttpStatusCode.Forbidden);
				response.Headers.Add("TrackingID", "header-only");
				return Task.FromResult(response);
			}));
		Func<Task> act = () => client.Assets.GetAssetsAsync();
		var error = (await act.Should().ThrowAsync<CiscoIqAuthorizationException>()).Which;
		error.Message.Should().Be("Cisco IQ returned HTTP 403.");
		error.TrackingId.Should().Be("header-only");
		error.BodyTrackingId.Should().BeNull();
		calls.Should().Be(1);
	}

	[Fact]
	public async Task TokenExpiry_UsesResponseLifetimeAndRefreshMargin()
	{
		var clock = new MutableClock();
		var exchanges = 0;
		var options = AuthenticationTests.Options;
		options.TokenRefreshMargin = TimeSpan.FromSeconds(2);
		using var client = new CiscoIqClient(options, new TestTransport((_, _) => Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}"))),
			new TestTransport((_, _) => { exchanges++; return Task.FromResult(TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":10}")); }), clock);
		await client.Assets.GetAssetsAsync();
		clock.Now += TimeSpan.FromSeconds(7);
		await client.Assets.GetAssetsAsync();
		exchanges.Should().Be(1);
		clock.Now += TimeSpan.FromSeconds(1);
		await client.Assets.GetAssetsAsync();
		exchanges.Should().Be(2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InvalidJson_IsSurfacedForPageAndEnumeration(bool enumerate)
	{
		using var client = new CiscoIqClient(AuthenticationTests.Options,
			new TestTransport((_, _) => Task.FromResult(TestTransport.Json("invalid-json"))), PagingAndRetryTests.Exchange);
		Func<Task> act = enumerate
			? async () => { await foreach (var _ in client.Assets.GetAssetsAllAsync()) { } }
			: () => client.Assets.GetAssetsAsync();
		await act.Should().ThrowAsync<Refit.ApiException>();
	}

	[Fact]
	public void NullableDateConverter_WritesNull()
	{
		using var output = new StringWriter();
		using var writer = new JsonTextWriter(output);
		new UnixMillisecondsConverter().WriteJson(writer, null, JsonSerializer.CreateDefault());
		output.ToString().Should().Be("null");
	}

	[Theory]
	[InlineData(0, 1, 0)]
	[InlineData(100, 1, -1)]
	[InlineData(100, 0, 0)]
	public void Options_RejectInvalidTimeoutAttemptsAndRefreshMargin(int timeout, int attempts, int margin)
	{
		var options = AuthenticationTests.Options;
		options.HttpClientTimeoutSeconds = timeout;
		options.MaxAttemptCount = attempts;
		options.TokenRefreshMargin = TimeSpan.FromSeconds(margin);
		Action act = options.Validate;
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Options_RejectBlankSuppliedAccountId()
	{
		var options = AuthenticationTests.Options;
		options.AccountId = " ";
		Action act = options.Validate;
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Formatter_UsesWireValuesAndRejectsUndefinedEnums()
	{
		var formatter = new CiscoIqUrlParameterFormatter();
		string? Format(object? value) => formatter.Format(value, typeof(AssetFilter), value?.GetType() ?? typeof(string));
		Format(null).Should().BeNull();
		Format(CiscoIqSortOrder.Ascending).Should().Be("ASC");
		Format(CiscoIqSortOrder.Descending).Should().Be("DESC");
		Format(CiscoIqMilestoneType.Hardware).Should().Be("hardware");
		Format(CiscoIqMilestoneType.Software).Should().Be("software");
		Format(CiscoIqAccountRegion.Us).Should().Be("US");
		Format(CiscoIqAccountRegion.Emea).Should().Be("EMEA");
		Format(CiscoIqAccountRegion.Apjc).Should().Be("APJC");
		Format(false).Should().Be("false");
		Action act = () => Format((CiscoIqSortOrder)99);
		act.Should().Throw<ArgumentException>();
	}

	private sealed class MutableClock : TimeProvider
	{
		internal DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
		public override DateTimeOffset GetUtcNow() => Now;
	}
}
