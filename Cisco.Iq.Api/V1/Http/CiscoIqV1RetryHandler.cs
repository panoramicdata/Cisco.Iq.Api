using System.Net;
using System.Security.Cryptography;

namespace Cisco.Iq.Api.V1.Http;

internal sealed class CiscoIqV1RetryHandler(CiscoIqV1ClientOptions options, Action<CiscoIqV1RateLimitInfo> observe, Func<TimeSpan, CancellationToken, Task> delay) : DelegatingHandler
{
	private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(60);

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var attempt = 1;
		while (true)
		{
			using var copy = await CiscoIqV1RequestCopy.CreateAsync(request, cancellationToken).ConfigureAwait(false);
			var response = await base.SendAsync(copy, cancellationToken).ConfigureAwait(false);
			var info = CiscoIqV1RateLimitInfo.Read(response);
			if (info is not null)
			{
				observe(info);
			}

			if (!ShouldRetry(request.Method, response.StatusCode, attempt))
			{
				return response;
			}

			var wait = WaitFor(response, attempt);
			response.Dispose();
			await delay(wait, cancellationToken).ConfigureAwait(false);
			attempt++;
		}
	}

	private bool ShouldRetry(HttpMethod method, HttpStatusCode status, int attempt)
		=> attempt < options.MaxAttemptCount
			&& ((status == HttpStatusCode.TooManyRequests && options.RetryRateLimitedRequests)
				|| (method == HttpMethod.Get && status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable));

	private static TimeSpan WaitFor(HttpResponseMessage response, int attempt)
	{
		var advised = CiscoIqV1ApiException.ReadRetryAfter(response);
		var wait = advised ?? TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) + (RandomNumberGenerator.GetInt32(1000) / 1000.0));
		return wait > MaxWait ? MaxWait : wait;
	}
}
