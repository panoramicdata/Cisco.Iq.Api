using System.Globalization;

namespace Cisco.Iq.Api;

/// <summary>The limits and remaining capacity for one rate-limit window.</summary>
public sealed record CiscoIqRateLimitWindow(long? Limit, long? Remaining, long? ResetSeconds);

/// <summary>The four principal and account rate-limit windows in a response.</summary>
public sealed record CiscoIqRateLimitStatus(
	CiscoIqRateLimitWindow PrincipalSecond,
	CiscoIqRateLimitWindow PrincipalDay,
	CiscoIqRateLimitWindow AccountSecond,
	CiscoIqRateLimitWindow AccountDay)
{
	/// <summary>The maximum reset time supplied across all windows.</summary>
	public long? ResetSeconds => new[] { PrincipalSecond.ResetSeconds, PrincipalDay.ResetSeconds, AccountSecond.ResetSeconds, AccountDay.ResetSeconds }.Max();

	internal static CiscoIqRateLimitStatus? Read(HttpResponseMessage response)
	{
		long? ReadHeader(string name)
			=> response.Headers.TryGetValues(name, out var values)
				&& long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
				&& value >= 0 ? value : null;
		CiscoIqRateLimitWindow Window(string scope, string window)
		{
			var prefix = $"x-{scope}-{window}-ratelimit-";
			return new(ReadHeader(prefix + "limit"), ReadHeader(prefix + "remaining"), ReadHeader(prefix + "reset"));
		}
		var status = new CiscoIqRateLimitStatus(Window("principal", "second"), Window("principal", "day"), Window("account", "second"), Window("account", "day"));
		return new[] { status.PrincipalSecond, status.PrincipalDay, status.AccountSecond, status.AccountDay }
			.Any(w => w.Limit.HasValue || w.Remaining.HasValue || w.ResetSeconds.HasValue) ? status : null;
	}
}
