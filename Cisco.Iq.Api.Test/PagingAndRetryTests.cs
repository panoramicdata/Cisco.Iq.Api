using System.Net;
using Cisco.Iq.Api.Internal;

namespace Cisco.Iq.Api.Test;

public class PagingAndRetryTests
{
	internal static TestTransport Exchange => new((_, _) => Task.FromResult(TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":3600}")));

	[Fact]
	public async Task Paging_FollowsOpaqueRelativeLink_WhenCountIsUnknown_AndFirstPageEmpty()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((request, _) =>
		{
			calls++;
			if (calls == 1)
			{
				var first = TestTransport.Json("{\"items\":[],\"meta\":{\"count\":null}}");
				first.Headers.Add("Link", "<?cursor=opaque&max=1>; title=\"page, two\"; rel=\"prev next\"");
				return Task.FromResult(first);
			}
			request.RequestUri!.PathAndQuery.Should().Be("/ciq-rest/api/v0/assets?cursor=opaque&max=1");
			return Task.FromResult(TestTransport.Json("{\"items\":[{\"assetId\":\"a\"}],\"meta\":{\"count\":null}}"));
		}), Exchange);
		var items = new List<string?>();
		await foreach (var asset in client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None))
		{
			items.Add(asset.AssetId);
		}
		items.Should().Equal("a");
		calls.Should().Be(2);
	}

	[Fact]
	public async Task Paging_RejectsLinkToDifferentOrigin_BeforeSendingCredentials()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) =>
		{
			calls++;
			var response = TestTransport.Json("{\"items\":[],\"meta\":{}}");
			response.Headers.Add("Link", "<https://example.com/assets>; rel=next");
			return Task.FromResult(response);
		}), Exchange);
		Func<Task> act = async () => { await foreach (var item in client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None)) { item.Should().NotBeNull(); } };
		await act.Should().ThrowAsync<InvalidDataException>();
		calls.Should().Be(1);
	}

	[Theory]
	[InlineData(HttpStatusCode.TooManyRequests, true, 2)]
	[InlineData(HttpStatusCode.TooManyRequests, false, 1)]
	[InlineData(HttpStatusCode.BadGateway, true, 2)]
	[InlineData(HttpStatusCode.BadRequest, true, 1)]
	[InlineData(HttpStatusCode.Forbidden, true, 1)]
	[InlineData(HttpStatusCode.NotFound, true, 1)]
	[InlineData(HttpStatusCode.NotAcceptable, true, 1)]
	public async Task Retry_IsBounded_AndOnlyRetries429And502(HttpStatusCode status, bool retryRateLimited, int expectedCalls)
	{
		var options = AuthenticationTests.Options;
		options.MaxAttemptCount = 2;
		options.RetryRateLimitedRequests = retryRateLimited;
		var delays = new List<TimeSpan>();
		var calls = 0;
		using var client = new CiscoIqClient(options, new TestTransport((_, _) =>
		{
			calls++;
			var response = TestTransport.Json("{\"message\":\"test error\",\"trackingId\":\"body-id\"}", status);
			response.Headers.Add("TrackingID", "header-id");
			response.Headers.Add("x-principal-second-ratelimit-reset", "2");
			response.Headers.Add("x-account-day-ratelimit-reset", "7");
			return Task.FromResult(response);
		}), Exchange, delay: (duration, _) => { delays.Add(duration); return Task.CompletedTask; });
		Func<Task> act = () => client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None);
		var error = (await act.Should().ThrowAsync<CiscoIqApiException>()).Which;
		error.StatusCode.Should().Be(status);
		error.BodyTrackingId.Should().Be("body-id");
		error.HeaderTrackingId.Should().Be("header-id");
		error.TrackingId.Should().Be("body-id");
		calls.Should().Be(expectedCalls);
		delays.Should().HaveCount(expectedCalls - 1);
		client.LastRateLimitStatus!.AccountDay.ResetSeconds.Should().Be(7);
		if (status == HttpStatusCode.TooManyRequests)
		{
			error.Should().BeOfType<CiscoIqRateLimitException>().Which.ResetSeconds.Should().Be(7);
			if (retryRateLimited) { delays.Should().Equal(TimeSpan.FromSeconds(7)); }
		}
		if (status == HttpStatusCode.BadGateway) { delays.Single().TotalSeconds.Should().BeInRange(1, 2); }
	}

	[Fact]
	public void DefaultTransport_PreservesManualCookies_AndDoesNotRedirectCredentials()
	{
		using var handler = CiscoIqClient.CreateTransport();
		handler.UseCookies.Should().BeFalse();
		handler.AllowAutoRedirect.Should().BeFalse();
	}

	[Theory]
	[InlineData("</previous>; rel=prev", null)]
	[InlineData("</previous>; rel=prev, </next>; rel=next", "https://iq.cisco.com/next")]
	[InlineData("</next>; REL=\"next\"", "https://iq.cisco.com/next")]
	[InlineData("</next>; title=\"no relation\"", null)]
	[InlineData("</previous>; title=\"; rel=next\"; rel=prev", null)]
	[InlineData("</next>; title=\"<quoted> page\"; rel=next", "https://iq.cisco.com/next")]
	[InlineData("</next>; rel=\"NEXT\"", "https://iq.cisco.com/next")]
	public void LinkParser_RecognizesNextRelation(string link, string? expected)
	{
		using var response = TestTransport.Json("{}");
		response.Headers.Add("Link", link);
		PageReader.Next(response.Headers, new Uri("https://iq.cisco.com/assets")).Should().Be(expected is null ? null : new Uri(expected));
	}

	[Fact]
	public async Task Cancellation_InterruptsRateLimitWait_WithoutAnotherRequest()
	{
		var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) =>
		{
			calls++;
			return Task.FromResult(TestTransport.Json("{}", HttpStatusCode.TooManyRequests));
		}), Exchange, delay: async (_, cancellationToken) =>
		{
			waiting.SetResult();
			await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
		});
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var pending = client.Assets.GetAssetsAsync(new GetAssetsRequest(), cancellation.Token);
		await waiting.Task.WaitAsync(cancellation.Token);
		cancellation.Cancel();
		Func<Task> act = () => pending;
		await act.Should().ThrowAsync<OperationCanceledException>();
		calls.Should().Be(1);
	}
}
