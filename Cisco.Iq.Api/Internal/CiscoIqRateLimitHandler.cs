using System.Net;

namespace Cisco.Iq.Api.Internal;

internal sealed class CiscoIqRateLimitHandler(CiscoIqClientOptions options, Action<CiscoIqRateLimitStatus> observe, Func<TimeSpan, CancellationToken, Task> delay) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var attempt = 1;
		while (true)
		{
			using var copy = RequestCopy.Create(request);
			var response = await base.SendAsync(copy, cancellationToken).ConfigureAwait(false);
			var status = CiscoIqRateLimitStatus.Read(response);
			if (status is not null)
			{
				observe(status);
			}
			if (!ShouldRetry(response.StatusCode, attempt))
			{
				return response;
			}
			var seconds = RetrySeconds(response.StatusCode, status, attempt);
			response.Dispose();
			await delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);
			attempt++;
		}
	}

	private bool ShouldRetry(HttpStatusCode statusCode, int attempt)
		=> attempt < options.MaxAttemptCount && (statusCode == HttpStatusCode.BadGateway
			|| (statusCode == HttpStatusCode.TooManyRequests && options.RetryRateLimitedRequests));

	private static double RetrySeconds(HttpStatusCode statusCode, CiscoIqRateLimitStatus? status, int attempt)
		=> statusCode == HttpStatusCode.TooManyRequests ? status?.ResetSeconds ?? 1
			: Math.Min(30, Math.Pow(2, attempt - 1)) + System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000) / 1000.0;
}
