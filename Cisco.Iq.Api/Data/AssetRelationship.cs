using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ AssetRelationship response.</summary>
public class AssetRelationship
{
	/// <summary>The assetId value, when included in the response.</summary>
	[JsonProperty("assetId")]
	public string? AssetId { get; set; }

	/// <summary>The parentAssetId value, when included in the response.</summary>
	[JsonProperty("parentAssetId")]
	public string? ParentAssetId { get; set; }

	/// <summary>The relationshipType value, when included in the response.</summary>
	[JsonProperty("relationshipType")]
	public string? RelationshipType { get; set; }

	/// <summary>The equipmentType value, when included in the response.</summary>
	[JsonProperty("equipmentType")]
	public string? EquipmentType { get; set; }

	/// <summary>The productType value, when included in the response.</summary>
	[JsonProperty("productType")]
	public string? ProductType { get; set; }

	/// <summary>The productId value, when included in the response.</summary>
	[JsonProperty("productId")]
	public string? ProductId { get; set; }

	/// <summary>The serialNumber value, when included in the response.</summary>
	[JsonProperty("serialNumber")]
	public string? SerialNumber { get; set; }
}
