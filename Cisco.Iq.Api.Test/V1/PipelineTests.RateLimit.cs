using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cisco.Iq.Api.V1;

namespace Cisco.Iq.Api.Test.V1;

public partial class PipelineTests
{
	[Fact]
	public async Task RateLimit_RetriesAfterTheAdvisedDelayThenSucceeds()
	{
		var calls = 0;
		var delays = new List<TimeSpan>();
		using var client = V1Support.Client(_ => ++calls == 1
			? Throttled().With(r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2)))
			: V1Support.Json(EmptyPage), delays: delays);

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();

		calls.Should().Be(2);
		delays.Should().Equal(TimeSpan.FromSeconds(2));
	}

	[Fact]
	public async Task RateLimit_BacksOffExponentiallyWithoutAdvice_AndGivesUpAfterTheAttemptLimit()
	{
		var calls = 0;
		var delays = new List<TimeSpan>();
		using var client = V1Support.Client(_ =>
		{
			calls++;
			return Throttled();
		}, delays: delays);

		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();

		var exception = (await act.Should().ThrowAsync<CiscoIqV1RateLimitException>()).Which;
		exception.RetryAfter.Should().BeNull();
		calls.Should().Be(3);
		delays.Should().HaveCount(2);
		delays[0].TotalSeconds.Should().BeInRange(1, 2);
		delays[1].TotalSeconds.Should().BeInRange(2, 3);
	}

	[Fact]
	public async Task RateLimit_UsesAnHttpDateAndCapsTheWait()
	{
		var calls = 0;
		var delays = new List<TimeSpan>();
		using var client = V1Support.Client(_ => ++calls switch
		{
			1 => Throttled().With(r => r.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(30))),
			2 => Throttled().With(r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(1))),
			_ => V1Support.Json(EmptyPage)
		}, o => o.MaxAttemptCount = 3, delays);

		using var response = await client.Assets.ListAssetPhysicalAssetsAsync();

		delays[0].TotalSeconds.Should().BeInRange(25, 31);
		delays[1].Should().Be(TimeSpan.FromSeconds(60));
	}

	[Fact]
	public async Task RateLimit_PastDateMeansNoWait_AndExposesRetryAfterOnTheException()
	{
		var delays = new List<TimeSpan>();
		using var client = V1Support.Client(
			_ => Throttled().With(r => r.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(-5))),
			o => o.MaxAttemptCount = 2,
			delays);

		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();

		(await act.Should().ThrowAsync<CiscoIqV1RateLimitException>()).Which.RetryAfter.Should().Be(TimeSpan.Zero);
		delays.Should().Equal(TimeSpan.Zero);
	}

	[Fact]
	public async Task RateLimit_DoesNotRetryWhenDisabled()
	{
		var calls = 0;
		using var client = V1Support.Client(_ =>
		{
			calls++;
			return Throttled();
		}, o => o.RetryRateLimitedRequests = false);
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		await act.Should().ThrowAsync<CiscoIqV1RateLimitException>();
		calls.Should().Be(1);
	}
}
