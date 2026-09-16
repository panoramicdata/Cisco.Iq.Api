using Cisco.Iq.Api.Data;
using Newtonsoft.Json;

namespace Cisco.Iq.Api;

/// <summary>A page of collection results.</summary>
/// <typeparam name="T">The item type.</typeparam>
public class CiscoIqPage<T>
{
	/// <summary>The items on this page.</summary>
	[JsonProperty("items")]
	public List<T> Items { get; set; } = [];

	/// <summary>The collection metadata; a null count means the total is unknown.</summary>
	[JsonProperty("meta")]
	public CollectionMeta Meta { get; set; } = new();
}
