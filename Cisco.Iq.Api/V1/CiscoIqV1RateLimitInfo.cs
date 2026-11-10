namespace Cisco.Iq.Api.V1;

/// <summary>The raw RateLimit and RateLimit-Policy header values from the latest response.</summary>
/// <param name="RateLimit">The RateLimit header value, or null.</param>
/// <param name="RateLimitPolicy">The RateLimit-Policy header value, or null.</param>
public sealed record CiscoIqV1RateLimitInfo(string? RateLimit, string? RateLimitPolicy)
{
	internal static CiscoIqV1RateLimitInfo? Read(HttpResponseMessage response)
	{
		var limit = Header(response, "RateLimit");
		var policy = Header(response, "RateLimit-Policy");
		return limit is null && policy is null ? null : new CiscoIqV1RateLimitInfo(limit, policy);
	}

	private static string? Header(HttpResponseMessage response, string name)
		=> response.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;
}
