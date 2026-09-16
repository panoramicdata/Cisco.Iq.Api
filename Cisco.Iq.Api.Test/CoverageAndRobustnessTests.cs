using System.Net;
using System.Net.Sockets;
using System.Text;
using Cisco.Iq.Api.Internal;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.Test;

public class CoverageAndRobustnessTests
{
	[Theory]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	[InlineData(HttpStatusCode.BadGateway)]
	public async Task GatewayErrorsAndMissingBodies_PreserveStatusWithoutRequiringJson(HttpStatusCode status)
	{
		foreach (var payload in new[] { "null", "{}", "<html>error</html>" })
		{
			using var response = TestTransport.Json(payload, status);
			var error = await CiscoIqApiException.FromResponseAsync(response, CancellationToken.None);
			error.StatusCode.Should().Be(status);
			error.TrackingId.Should().BeNull();
		}
	}

	[Theory]
	[InlineData("limit", "10", true)]
	[InlineData("remaining", "0", true)]
	[InlineData("reset", "2", true)]
	[InlineData("reset", "bad", false)]
	[InlineData("reset", "-1", false)]
	public void RateLimits_PartialAndMalformedHeadersAreHandled(string field, string value, bool known)
	{
		using var response = TestTransport.Json("{}");
		response.Headers.Add("x-principal-second-ratelimit-" + field, value);
		(CiscoIqRateLimitStatus.Read(response) is not null).Should().Be(known);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RateLimitWithoutReset_UsesFixedFallback_AndCanRecover(bool partial)
	{
		var calls = 0;
		var delays = new List<TimeSpan>();
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) =>
		{
			calls++;
			if (calls > 1) { return Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}")); }
			var response = TestTransport.Json("{}", HttpStatusCode.TooManyRequests);
			if (partial) { response.Headers.Add("x-principal-second-ratelimit-remaining", "0"); }
			return Task.FromResult(response);
		}), PagingAndRetryTests.Exchange, delay: (duration, _) => { delays.Add(duration); return Task.CompletedTask; });
		await client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None);
		delays.Should().Equal(TimeSpan.FromSeconds(1));
	}

	[Theory]
	[InlineData("null", true)]
	[InlineData("{\"items\":[],\"meta\":{}}", false)]
	public async Task NextPage_CanBeEmpty_ButCannotBeNull(string payload, bool invalid)
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) =>
		{
			calls++;
			if (calls > 1) { return Task.FromResult(TestTransport.Json(payload)); }
			var response = TestTransport.Json("{\"items\":[],\"meta\":{}}");
			response.Headers.Add("Link", "<?cursor=next>; rel=next");
			return Task.FromResult(response);
		}), PagingAndRetryTests.Exchange);
		Func<Task> act = async () => { await foreach (var item in client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None)) { item.Should().NotBeNull(); } };
		if (invalid) { await act.Should().ThrowAsync<JsonSerializationException>(); }
		else { await act(); }
		calls.Should().Be(2);
	}

	[Fact]
	public void Formatter_HandlesBothBooleanValuesAndInvariantNumbers()
	{
		var formatter = new CiscoIqUrlParameterFormatter();
		formatter.Format(true, typeof(bool), typeof(bool)).Should().Be("true");
		formatter.Format(false, typeof(bool), typeof(bool)).Should().Be("false");
		formatter.Format(12.5m, typeof(decimal), typeof(decimal)).Should().Be("12.5");
		formatter.Format("hello", typeof(string), typeof(string)).Should().Be("hello");
		foreach (var invalid in new Enum[] { (CiscoIqSortOrder)99, (CiscoIqMilestoneType)99, (CiscoIqAccountRegion)99 })
		{
			Action act = () => formatter.Format(invalid, typeof(Enum), invalid.GetType());
			act.Should().Throw<ArgumentException>();
		}
	}

	[Fact]
	public async Task Paging_HandlesDelayedAndFailedSubsequentResponses()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport(async (_, cancellationToken) =>
		{
			calls++;
			await Task.Delay(1, cancellationToken);
			var response = TestTransport.Json("{\"items\":[{}],\"meta\":{}}");
			response.Headers.Add("Link", "<?cursor=next>; rel=next");
			if (calls == 3) { response.StatusCode = HttpStatusCode.Forbidden; }
			return response;
		}), PagingAndRetryTests.Exchange);
		Func<Task> act = async () => { await foreach (var item in client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None)) { item.Should().NotBeNull(); } };
		await act.Should().ThrowAsync<CiscoIqAuthorizationException>();
		calls.Should().Be(3);
	}

	[Fact]
	public async Task CookieHeader_ReachesActualHttpWire()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var server = Task.Run(async () =>
		{
			using var socket = await listener.AcceptTcpClientAsync(cancellation.Token);
			await using var stream = socket.GetStream();
			using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
			var headers = new List<string>();
			var line = await reader.ReadLineAsync(cancellation.Token);
			while (!string.IsNullOrEmpty(line))
			{
				headers.Add(line);
				line = await reader.ReadLineAsync(cancellation.Token);
			}
			headers.Should().Contain("Cookie: account_region=EMEA");
			await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"), cancellation.Token);
		}, cancellation.Token);
		using var http = new HttpClient(CiscoIqClient.CreateTransport());
		using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/");
		request.Headers.Add("Cookie", "account_region=EMEA");
		using var response = await http.SendAsync(request, cancellation.Token);
		response.IsSuccessStatusCode.Should().BeTrue();
		await server;
	}

	[Fact]
	public async Task Paging_CanBeDisposedWhileYieldingSecondPage()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((_, _) =>
		{
			calls++;
			var response = TestTransport.Json(calls == 1 ? "{\"items\":[],\"meta\":{}}" : "{\"items\":[{},{}],\"meta\":{}}");
			response.Headers.Add("Link", "<?cursor=next>; rel=next");
			return Task.FromResult(response);
		}), PagingAndRetryTests.Exchange);
		await using var enumerator = client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None).GetAsyncEnumerator();
		(await enumerator.MoveNextAsync()).Should().BeTrue();
		calls.Should().Be(2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Paging_CombinesMethodAndEnumeratorCancellationTokens(bool cancelMethod)
	{
		using var methodCancellation = new CancellationTokenSource();
		using var enumeratorCancellation = new CancellationTokenSource();
		using var client = new CiscoIqClient(AuthenticationTests.Options,
			new TestTransport((_, _) => Task.FromResult(TestTransport.Json("{\"items\":[{},{}],\"meta\":{}}"))), PagingAndRetryTests.Exchange);
		await using var enumerator = client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), methodCancellation.Token)
			.GetAsyncEnumerator(enumeratorCancellation.Token);
		(await enumerator.MoveNextAsync()).Should().BeTrue();
		if (cancelMethod) { methodCancellation.Cancel(); }
		else { enumeratorCancellation.Cancel(); }
		Func<Task> act = async () => await enumerator.MoveNextAsync();
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Paging_CombinedCancellationTokens_CompleteNormally()
	{
		using var methodCancellation = new CancellationTokenSource();
		using var enumeratorCancellation = new CancellationTokenSource();
		using var client = new CiscoIqClient(AuthenticationTests.Options,
			new TestTransport((_, _) => Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}"))), PagingAndRetryTests.Exchange);
		await using var enumerator = client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), methodCancellation.Token)
			.GetAsyncEnumerator(enumeratorCancellation.Token);
		(await enumerator.MoveNextAsync()).Should().BeFalse();
	}
}
