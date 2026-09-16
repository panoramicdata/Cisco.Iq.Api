using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ Asset response.</summary>
public class Asset : AssetBase
{
	/// <summary>The productDescription value, when included in the response.</summary>
	[JsonProperty("productDescription")]
	public string? ProductDescription { get; set; }

	/// <summary>The role value, when included in the response.</summary>
	[JsonProperty("role")]
	public string? Role { get; set; }

	/// <summary>The importance value, when included in the response.</summary>
	[JsonProperty("importance")]
	public string? Importance { get; set; }

	/// <summary>The salesOrderNumber value, when included in the response.</summary>
	[JsonProperty("salesOrderNumber")]
	public string? SalesOrderNumber { get; set; }

	/// <summary>The warrantyType value, when included in the response.</summary>
	[JsonProperty("warrantyType")]
	public string? WarrantyType { get; set; }

	/// <summary>The warrantyEndDate value, when included in the response.</summary>
	[JsonProperty("warrantyEndDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? WarrantyEndDate { get; set; }

	/// <summary>The currentHardwareMilestoneDate value, when included in the response.</summary>
	[JsonProperty("currentHardwareMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? CurrentHardwareMilestoneDate { get; set; }

	/// <summary>The currentSoftwareMilestoneDate value, when included in the response.</summary>
	[JsonProperty("currentSoftwareMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? CurrentSoftwareMilestoneDate { get; set; }

	/// <summary>The securityAdvisoryCount value, when included in the response.</summary>
	[JsonProperty("securityAdvisoryCount")]
	public int? SecurityAdvisoryCount { get; set; }
}
