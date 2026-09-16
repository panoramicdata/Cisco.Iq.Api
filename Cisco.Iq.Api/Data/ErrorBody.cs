using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ ErrorBody response.</summary>
public class ErrorBody
{
	/// <summary>The message value, when included in the response.</summary>
	[JsonProperty("message")]
	public string? Message { get; set; }

	/// <summary>The trackingId value, when included in the response.</summary>
	[JsonProperty("trackingId")]
	public string? TrackingId { get; set; }
}
