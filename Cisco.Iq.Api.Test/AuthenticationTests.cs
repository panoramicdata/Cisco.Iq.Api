using System.Net;

namespace Cisco.Iq.Api.Test;

public class AuthenticationTests
{
	internal static CiscoIqClientOptions Options => new()
	{
		Token = "raw-pat",
		AccountId = "account",
		AccountRegion = CiscoIqAccountRegion.Emea
	};

	[Fact]
	public async Task Exchange_UsesRawBasicCookieAndDefaultUserAgent_AndCachesToken()
	{
		var exchanges = 0;
		using var client = new CiscoIqClient(Options,
			new TestTransport((request, _) =>
			{
				request.Headers.Authorization!.ToString().Should().Be("Bearer access");
				request.Headers.GetValues("Cookie").Should().ContainSingle().Which.Should().Be("account_region=EMEA");
				request.Headers.UserAgent.ToString().Should().MatchRegex(@"^Cisco\.Iq\.Api/\d+\.\d+\.\d+$");
				return Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{\"count\":null}}"));
			}),
			new TestTransport(async (request, cancellationToken) =>
			{
				Interlocked.Increment(ref exchanges);
				request.Headers.Authorization!.ToString().Should().Be("Basic raw-pat");
				request.Headers.GetValues("Cookie").Should().ContainSingle().Which.Should().Be("account_region=EMEA");
				request.Headers.UserAgent.ToString().Should().MatchRegex(@"^Cisco\.Iq\.Api/\d+\.\d+\.\d+$");
				(await request.Content!.ReadAsStringAsync(cancellationToken)).Should().Be("{\"accountId\":\"account\"}");
				await Task.Delay(10, cancellationToken);
				return TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":3600}");
			}));
		await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None)));
		await client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None);
		exchanges.Should().Be(1);
	}

	[Fact]
	public async Task Unauthorized_ReexchangesOnce_ThenPropagatesSecondUnauthorized()
	{
		var exchanges = 0;
		var calls = 0;
		using var client = new CiscoIqClient(Options,
			new TestTransport((_, _) =>
			{
				calls++;
				return Task.FromResult(TestTransport.Json("{\"message\":\"revoked\"}", HttpStatusCode.Unauthorized));
			}), new TestTransport((_, _) =>
			{
				exchanges++;
				return Task.FromResult(TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":3600}"));
			}));
		Func<Task> act = () => client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None);
		await act.Should().ThrowAsync<CiscoIqAuthenticationException>();
		exchanges.Should().Be(2);
		calls.Should().Be(2);
	}

	[Fact]
	public async Task ConcurrentUnauthorizedResponses_ShareOneRefresh_AndRetryWithNewToken()
	{
		var exchanges = 0;
		var rejected = 0;
		var succeeded = 0;
		var allRejected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var client = new CiscoIqClient(Options, new TestTransport(async (request, cancellationToken) =>
		{
			if (request.Headers.Authorization!.Parameter == "first")
			{
				if (Interlocked.Increment(ref rejected) == 5) { allRejected.SetResult(); }
				await allRejected.Task.WaitAsync(cancellationToken);
				return TestTransport.Json("{}", HttpStatusCode.Unauthorized);
			}
			request.Headers.Authorization.Parameter.Should().Be("second");
			Interlocked.Increment(ref succeeded);
			return TestTransport.Json("{\"items\":[],\"meta\":{}}");
		}), new TestTransport(async (_, cancellationToken) =>
		{
			var exchange = Interlocked.Increment(ref exchanges);
			await Task.Delay(10, cancellationToken);
			return TestTransport.Json($"{{\"accessToken\":\"{(exchange == 1 ? "first" : "second")}\",\"expiresInSeconds\":3600}}");
		}));
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.Assets.GetAssetsAsync(new GetAssetsRequest(), cancellation.Token)));
		exchanges.Should().Be(2);
		rejected.Should().Be(5);
		succeeded.Should().Be(5);
	}

	[Fact]
	public async Task CancelledRefresh_ReleasesSemaphore_ForSubsequentCall()
	{
		var exchanges = 0;
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var client = new CiscoIqClient(Options,
			new TestTransport((_, _) => Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}"))),
			new TestTransport(async (_, cancellationToken) =>
			{
				if (Interlocked.Increment(ref exchanges) == 1)
				{
					started.SetResult();
					await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
				}
				return TestTransport.Json("{\"accessToken\":\"access\",\"expiresInSeconds\":3600}");
			}));
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var pending = client.Assets.GetAssetsAsync(new GetAssetsRequest(), cancellation.Token);
		await started.Task.WaitAsync(cancellation.Token);
		cancellation.Cancel();
		Func<Task> act = () => pending;
		await act.Should().ThrowAsync<OperationCanceledException>();
		await client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None);
		exchanges.Should().Be(2);
	}
}
