using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>The fields shared by Cisco IQ asset responses.</summary>
public abstract class AssetBase
{
	/// <summary>The assetId value, when included in the response.</summary>
	[JsonProperty("assetId")]
	public string? AssetId { get; set; }

	/// <summary>The customerId value, when included in the response.</summary>
	[JsonProperty("customerId")]
	public string? CustomerId { get; set; }

	/// <summary>The serialNumber value, when included in the response.</summary>
	[JsonProperty("serialNumber")]
	public string? SerialNumber { get; set; }

	/// <summary>The productId value, when included in the response.</summary>
	[JsonProperty("productId")]
	public string? ProductId { get; set; }

	/// <summary>The productFamily value, when included in the response.</summary>
	[JsonProperty("productFamily")]
	public string? ProductFamily { get; set; }

	/// <summary>The productType value, when included in the response.</summary>
	[JsonProperty("productType")]
	public string? ProductType { get; set; }

	/// <summary>The equipmentType value, when included in the response.</summary>
	[JsonProperty("equipmentType")]
	public string? EquipmentType { get; set; }

	/// <summary>The hostname value, when included in the response.</summary>
	[JsonProperty("hostname")]
	public string? Hostname { get; set; }

	/// <summary>The ipAddress value, when included in the response.</summary>
	[JsonProperty("ipAddress")]
	public string? IpAddress { get; set; }

	/// <summary>The softwareVersion value, when included in the response.</summary>
	[JsonProperty("softwareVersion")]
	public string? SoftwareVersion { get; set; }

	/// <summary>The softwareType value, when included in the response.</summary>
	[JsonProperty("softwareType")]
	public string? SoftwareType { get; set; }

	/// <summary>The location value, when included in the response.</summary>
	[JsonProperty("location")]
	public string? Location { get; set; }

	/// <summary>The tags value, when included in the response.</summary>
	[JsonProperty("tags")]
	public List<string>? Tags { get; set; }

	/// <summary>The coverageStatus value, when included in the response.</summary>
	[JsonProperty("coverageStatus")]
	public string? CoverageStatus { get; set; }

	/// <summary>The contractNumber value, when included in the response.</summary>
	[JsonProperty("contractNumber")]
	public string? ContractNumber { get; set; }

	/// <summary>The coverageEndDate value, when included in the response.</summary>
	[JsonProperty("coverageEndDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? CoverageEndDate { get; set; }

	/// <summary>The supportType value, when included in the response.</summary>
	[JsonProperty("supportType")]
	public string? SupportType { get; set; }

	/// <summary>The supportTier value, when included in the response.</summary>
	[JsonProperty("supportTier")]
	public string? SupportTier { get; set; }

	/// <summary>The partnerName value, when included in the response.</summary>
	[JsonProperty("partnerName")]
	public string? PartnerName { get; set; }

	/// <summary>The currentHardwareMilestone value, when included in the response.</summary>
	[JsonProperty("currentHardwareMilestone")]
	public string? CurrentHardwareMilestone { get; set; }

	/// <summary>The nextHardwareMilestone value, when included in the response.</summary>
	[JsonProperty("nextHardwareMilestone")]
	public string? NextHardwareMilestone { get; set; }

	/// <summary>The nextHardwareMilestoneDate value, when included in the response.</summary>
	[JsonProperty("nextHardwareMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? NextHardwareMilestoneDate { get; set; }

	/// <summary>The hardwareLastDateOfSupport value, when included in the response.</summary>
	[JsonProperty("hardwareLastDateOfSupport")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? HardwareLastDateOfSupport { get; set; }

	/// <summary>The currentSoftwareMilestone value, when included in the response.</summary>
	[JsonProperty("currentSoftwareMilestone")]
	public string? CurrentSoftwareMilestone { get; set; }

	/// <summary>The nextSoftwareMilestone value, when included in the response.</summary>
	[JsonProperty("nextSoftwareMilestone")]
	public string? NextSoftwareMilestone { get; set; }

	/// <summary>The nextSoftwareMilestoneDate value, when included in the response.</summary>
	[JsonProperty("nextSoftwareMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? NextSoftwareMilestoneDate { get; set; }

	/// <summary>The softwareLastDateOfSupport value, when included in the response.</summary>
	[JsonProperty("softwareLastDateOfSupport")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? SoftwareLastDateOfSupport { get; set; }

	/// <summary>The endOfSoftwareMaintenance value, when included in the response.</summary>
	[JsonProperty("endOfSoftwareMaintenance")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfSoftwareMaintenance { get; set; }

	/// <summary>The lastSignalDate value, when included in the response.</summary>
	[JsonProperty("lastSignalDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? LastSignalDate { get; set; }

	/// <summary>The lastSignalType value, when included in the response.</summary>
	[JsonProperty("lastSignalType")]
	public string? LastSignalType { get; set; }

	/// <summary>The telemetryStatus value, when included in the response.</summary>
	[JsonProperty("telemetryStatus")]
	public string? TelemetryStatus { get; set; }

	/// <summary>The dataSource value, when included in the response.</summary>
	[JsonProperty("dataSource")]
	public string? DataSource { get; set; }

	/// <summary>The shipDate value, when included in the response.</summary>
	[JsonProperty("shipDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? ShipDate { get; set; }
}
