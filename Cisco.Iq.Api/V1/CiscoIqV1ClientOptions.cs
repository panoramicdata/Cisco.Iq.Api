namespace Cisco.Iq.Api.V1;

/// <summary>Options for the Cisco IQ GA client.</summary>
public class CiscoIqV1ClientOptions
{
	internal static readonly Uri GlobalBaseAddress = new("https://iq.cisco.com");

	/// <summary>The long-lived Cisco IQ API key, used directly as the bearer token.</summary>
	public required string ApiKey { get; set; }

	/// <summary>The region that holds the account.</summary>
	public required CiscoIqV1Region Region { get; set; }

	/// <summary>Overrides the host, for a regional host or a test server. Defaults to the global host.</summary>
	public Uri? BaseAddress { get; set; }

	/// <summary>Whether to send the X-IQ-Region header. Required by the global host. Defaults to true.</summary>
	public bool SendRegionHeader { get; set; } = true;

	/// <summary>The HTTP timeout in seconds. Defaults to 100.</summary>
	public int HttpClientTimeoutSeconds { get; set; } = 100;

	/// <summary>Whether to retry rate-limited requests. Defaults to true.</summary>
	public bool RetryRateLimitedRequests { get; set; } = true;

	/// <summary>The maximum number of attempts, including the first. Defaults to three.</summary>
	public int MaxAttemptCount { get; set; } = 3;

	/// <summary>An optional User-Agent override. When blank the client sends Cisco.Iq.Api and its version.</summary>
	public string? UserAgent { get; set; }

	private static readonly string DefaultUserAgent = "Cisco.Iq.Api/" + typeof(CiscoIqV1ClientOptions).Assembly.GetName().Version!.ToString(3);

	internal string EffectiveUserAgent => string.IsNullOrWhiteSpace(UserAgent) ? DefaultUserAgent : UserAgent;

	internal Uri EffectiveBaseAddress => BaseAddress ?? GlobalBaseAddress;

	/// <summary>Validates the options without disclosing the key.</summary>
	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Any(char.IsWhiteSpace))
		{
			throw new ArgumentException("ApiKey must be nonempty and contain no whitespace.", nameof(ApiKey));
		}

		if (!Enum.IsDefined(Region))
		{
			throw new ArgumentException("Region must be a defined region.", nameof(Region));
		}

		if (BaseAddress is not null && (!BaseAddress.IsAbsoluteUri || (BaseAddress.Scheme != Uri.UriSchemeHttps && BaseAddress.Scheme != Uri.UriSchemeHttp)))
		{
			throw new ArgumentException("BaseAddress must be an absolute http or https URI.", nameof(BaseAddress));
		}

		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(HttpClientTimeoutSeconds);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxAttemptCount);

		using var request = new HttpRequestMessage();
		if (!request.Headers.UserAgent.TryParseAdd(EffectiveUserAgent))
		{
			throw new ArgumentException("UserAgent must be a valid HTTP User-Agent.", nameof(UserAgent));
		}
	}
}
