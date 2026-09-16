namespace Cisco.Iq.Api;

/// <summary>
/// Options for the Cisco IQ client.
/// </summary>
public class CiscoIqClientOptions
{
	/// <summary>The long-lived Personal Access Token or Service Account Token.</summary>
	public required string Token { get; set; }

	/// <summary>The account's data storage region.</summary>
	public required CiscoIqAccountRegion AccountRegion { get; set; }

	/// <summary>Required for a personal token; optional for a service account token.</summary>
	public string? AccountId { get; set; }

	/// <summary>The HTTP timeout in seconds. Defaults to 100.</summary>
	public int HttpClientTimeoutSeconds { get; set; } = 100;

	/// <summary>The margin before access token expiry at which to refresh. Defaults to 60 seconds.</summary>
	public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>Whether to retry rate-limited requests. Defaults to true.</summary>
	public bool RetryRateLimitedRequests { get; set; } = true;

	/// <summary>The maximum number of attempts, including the initial request. Defaults to three.</summary>
	public int MaxAttemptCount { get; set; } = 3;

	/// <summary>
	/// An optional User-Agent override for token exchanges and product requests.
	/// When omitted or blank, the client uses Cisco.Iq.Api followed by its assembly version.
	/// </summary>
	public string? UserAgent { get; set; }

	private static readonly string DefaultUserAgent = "Cisco.Iq.Api/" + typeof(CiscoIqClientOptions).Assembly.GetName().Version!.ToString(3);
	internal string EffectiveUserAgent => string.IsNullOrWhiteSpace(UserAgent) ? DefaultUserAgent : UserAgent;

	/// <summary>Validates the configuration without disclosing credentials.</summary>
	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(Token) || Token.Any(char.IsWhiteSpace))
		{
			throw new ArgumentException("Token must be nonempty and contain no whitespace.", nameof(Token));
		}

		if (!Enum.IsDefined(AccountRegion))
		{
			throw new ArgumentException("AccountRegion must be a defined region.", nameof(AccountRegion));
		}

		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(HttpClientTimeoutSeconds);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxAttemptCount);
		ArgumentOutOfRangeException.ThrowIfLessThan(TokenRefreshMargin, TimeSpan.Zero);

		if (AccountId is not null && string.IsNullOrWhiteSpace(AccountId))
		{
			throw new ArgumentException("AccountId must be nonempty when supplied.", nameof(AccountId));
		}

		using var request = new HttpRequestMessage();
		if (!request.Headers.UserAgent.TryParseAdd(EffectiveUserAgent))
		{
			throw new ArgumentException("UserAgent must be a valid HTTP User-Agent.", nameof(UserAgent));
		}
	}
}
