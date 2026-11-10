using System.Net;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.V1;

/// <summary>A Cisco IQ GA API error.</summary>
public class CiscoIqV1ApiException : Exception
{
	/// <summary>Creates an API error.</summary>
	public CiscoIqV1ApiException(HttpStatusCode statusCode, string? message, CiscoIqV1Problem? problem, string? traceparent)
		: base(message ?? problem?.Detail ?? problem?.Title ?? $"Cisco IQ returned HTTP {(int)statusCode}.")
	{
		StatusCode = statusCode;
		Problem = problem;
		Traceparent = traceparent;
	}

	/// <summary>The response status code.</summary>
	public HttpStatusCode StatusCode { get; }

	/// <summary>The problem document, or null when the response carried none.</summary>
	public CiscoIqV1Problem? Problem { get; }

	/// <summary>The traceparent response header, useful in a Cisco support case.</summary>
	public string? Traceparent { get; }

	/// <summary>Whether the problem document contains the given error code.</summary>
	public bool HasErrorCode(string code)
		=> Problem?.Errors.Any(e => string.Equals(e.Code, code, StringComparison.Ordinal)) == true;

	internal static async Task<CiscoIqV1ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var traceparent = response.Headers.TryGetValues("traceparent", out var values) ? values.FirstOrDefault() : null;
		var problem = ReadProblem(body);
		var isHtml = (response.Content.Headers.ContentType?.ToString() ?? string.Empty).Contains("html", StringComparison.OrdinalIgnoreCase);
		var status = response.StatusCode;
		if (status == HttpStatusCode.BadRequest && problem is null && body.Contains("region", StringComparison.OrdinalIgnoreCase))
		{
			return new CiscoIqV1ApiException(status, "Cisco IQ rejected the request because no region was supplied. Set Region and keep SendRegionHeader enabled on the global host.", null, traceparent);
		}

		return status switch
		{
			HttpStatusCode.Unauthorized => new CiscoIqV1AuthenticationException(problem, traceparent, response.Headers.WwwAuthenticate.ToString()),
			HttpStatusCode.Forbidden => new CiscoIqV1AuthorizationException(problem, traceparent, isHtml && traceparent is null),
			HttpStatusCode.NotFound => new CiscoIqV1NotFoundException(problem, traceparent),
			HttpStatusCode.TooManyRequests => new CiscoIqV1RateLimitException(problem, traceparent, ReadRetryAfter(response)),
			_ => new CiscoIqV1ApiException(status, null, problem, traceparent)
		};
	}

	internal static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
	{
		var retry = response.Headers.RetryAfter;
		if (retry?.Delta is { } delta)
		{
			return delta;
		}

		return retry?.Date is { } date ? TimeSpan.FromSeconds(Math.Max(0, (date - DateTimeOffset.UtcNow).TotalSeconds)) : null;
	}

	private static CiscoIqV1Problem? ReadProblem(string body)
	{
		try
		{
			var problem = JsonConvert.DeserializeObject<CiscoIqV1Problem>(body);
			return problem is { Title: not null } or { Detail: not null } or { Status: not null } ? problem : null;
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

/// <summary>The API key was rejected: it is wrong, expired, revoked, or for another region.</summary>
public sealed class CiscoIqV1AuthenticationException(CiscoIqV1Problem? problem, string? traceparent, string? challenge)
	: CiscoIqV1ApiException(HttpStatusCode.Unauthorized, problem is null ? "Cisco IQ rejected the API key. It may be wrong, expired, revoked, or for a different region." : null, problem, traceparent)
{
	/// <summary>The WWW-Authenticate challenge, or empty.</summary>
	public string? Challenge { get; } = challenge;
}

/// <summary>The identity may not access the resource, or the query string was too long.</summary>
public sealed class CiscoIqV1AuthorizationException(CiscoIqV1Problem? problem, string? traceparent, bool probablyQueryTooLong)
	: CiscoIqV1ApiException(HttpStatusCode.Forbidden,
		probablyQueryTooLong ? "Cisco IQ refused the request before the API saw it. The usual cause is a query string over 2,048 bytes after encoding; use the System query endpoint for long filters." : null,
		problem, traceparent);

/// <summary>The resource was not found.</summary>
public sealed class CiscoIqV1NotFoundException(CiscoIqV1Problem? problem, string? traceparent)
	: CiscoIqV1ApiException(HttpStatusCode.NotFound, null, problem, traceparent);

/// <summary>The request was rate limited and the retries, if any, were exhausted.</summary>
public sealed class CiscoIqV1RateLimitException(CiscoIqV1Problem? problem, string? traceparent, TimeSpan? retryAfter)
	: CiscoIqV1ApiException(HttpStatusCode.TooManyRequests, null, problem, traceparent)
{
	/// <summary>How long Cisco asked the caller to wait, or null.</summary>
	public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>The encoded query string exceeds what Cisco IQ accepts, so the request was not sent.</summary>
public sealed class CiscoIqV1QueryTooLongException(int length)
	: Exception($"The encoded query string is {length} bytes; Cisco IQ accepts at most 2,048. Use POST /api/system/v1/query for long filters.")
{
	/// <summary>The encoded query length in bytes.</summary>
	public int Length { get; } = length;
}
