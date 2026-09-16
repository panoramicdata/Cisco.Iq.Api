using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ CollectionMeta response.</summary>
public class CollectionMeta
{
	/// <summary>The count value, when included in the response.</summary>
	[JsonProperty("count")]
	public long? Count { get; set; }

	/// <summary>The max value, when included in the response.</summary>
	[JsonProperty("max")]
	public int? Max { get; set; }

	/// <summary>The offset value, when included in the response.</summary>
	[JsonProperty("offset")]
	public int? Offset { get; set; }
}
