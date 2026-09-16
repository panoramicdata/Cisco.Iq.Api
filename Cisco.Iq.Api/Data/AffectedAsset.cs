using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ AffectedAsset response.</summary>
public class AffectedAsset : AssetBase
{
	/// <summary>The bulletinId value, when included in the response.</summary>
	[JsonProperty("bulletinId")]
	public int? BulletinId { get; set; }

	/// <summary>The vulnerabilityStatus value, when included in the response.</summary>
	[JsonProperty("vulnerabilityStatus")]
	public string? VulnerabilityStatus { get; set; }

	/// <summary>The vulnerabilityReasons value, when included in the response.</summary>
	[JsonProperty("vulnerabilityReasons")]
	public List<string>? VulnerabilityReasons { get; set; }

	/// <summary>The assetType value, when included in the response.</summary>
	[JsonProperty("assetType")]
	public string? AssetType { get; set; }
}
