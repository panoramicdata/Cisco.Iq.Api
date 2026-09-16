using System.Net;

namespace Cisco.Iq.Api.Internal;

internal sealed class CiscoIqRateLimitHandler(CiscoIqClientOptions options, Action<CiscoIqRateLimitStatus> observe, Func<TimeSpan, CancellationToken, Task> delay) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		for (var attempt = 1; ; attempt++)
		{
			using var copy = RequestCopy.Create(request);
			var response = await base.SendAsync(copy, cancellationToken).ConfigureAwait(false);
			var status = CiscoIqRateLimitStatus.Read(response);
			if (status is not null)
			{
				observe(status);
			}
			var rateLimited = response.StatusCode == HttpStatusCode.TooManyRequests && options.RetryRateLimitedRequests;
			if (attempt >= options.MaxAttemptCount || (!rateLimited && response.StatusCode != HttpStatusCode.BadGateway))
			{
				return response;
			}
			var seconds = rateLimited ? status?.ResetSeconds ?? 1 : Math.Min(30, Math.Pow(2, attempt - 1)) + System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000) / 1000.0;
			response.Dispose();
			await delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);
		}
	}
}
