using Refit;

namespace Cisco.Iq.Api;

/// <summary>Filters for Cisco IQ Asset requests.</summary>
public class AssetFilter : CiscoIqFilter
{
	/// <summary>The productFamily filter; omitted when null.</summary>
	[AliasAs("productFamily")]
	[Query(CollectionFormat.Multi)]
	public string[]? ProductFamily { get; set; }

	/// <summary>The productId filter; omitted when null.</summary>
	[AliasAs("productId")]
	[Query(CollectionFormat.Multi)]
	public string[]? ProductId { get; set; }

	/// <summary>The serialNumber filter; omitted when null.</summary>
	[AliasAs("serialNumber")]
	[Query(CollectionFormat.Multi)]
	public string[]? SerialNumber { get; set; }

	/// <summary>The hostname filter; omitted when null.</summary>
	[AliasAs("hostname")]
	[Query(CollectionFormat.Multi)]
	public string[]? Hostname { get; set; }

	/// <summary>The ipAddress filter; omitted when null.</summary>
	[AliasAs("ipAddress")]
	[Query(CollectionFormat.Multi)]
	public string[]? IpAddress { get; set; }

	/// <summary>The equipmentType filter; omitted when null.</summary>
	[AliasAs("equipmentType")]
	[Query(CollectionFormat.Multi)]
	public string[]? EquipmentType { get; set; }

	/// <summary>The productType filter; omitted when null.</summary>
	[AliasAs("productType")]
	[Query(CollectionFormat.Multi)]
	public string[]? ProductType { get; set; }

	/// <summary>The softwareType filter; omitted when null.</summary>
	[AliasAs("softwareType")]
	[Query(CollectionFormat.Multi)]
	public string[]? SoftwareType { get; set; }

	/// <summary>The softwareVersion filter; omitted when null.</summary>
	[AliasAs("softwareVersion")]
	[Query(CollectionFormat.Multi)]
	public string[]? SoftwareVersion { get; set; }

	/// <summary>The role filter; omitted when null.</summary>
	[AliasAs("role")]
	[Query(CollectionFormat.Multi)]
	public string[]? Role { get; set; }

	/// <summary>The importance filter; omitted when null.</summary>
	[AliasAs("importance")]
	[Query(CollectionFormat.Multi)]
	public string[]? Importance { get; set; }

	/// <summary>The location filter; omitted when null.</summary>
	[AliasAs("location")]
	[Query(CollectionFormat.Multi)]
	public string[]? Location { get; set; }

	/// <summary>The assetTags filter; omitted when null.</summary>
	[AliasAs("assetTags")]
	[Query(CollectionFormat.Multi)]
	public string[]? AssetTags { get; set; }

	/// <summary>The dataSource filter; omitted when null.</summary>
	[AliasAs("dataSource")]
	[Query(CollectionFormat.Multi)]
	public string[]? DataSource { get; set; }

	/// <summary>The contractNumber filter; omitted when null.</summary>
	[AliasAs("contractNumber")]
	[Query(CollectionFormat.Multi)]
	public string[]? ContractNumber { get; set; }

	/// <summary>The contractId filter; omitted when null.</summary>
	[AliasAs("contractId")]
	[Query(CollectionFormat.Multi)]
	public string[]? ContractId { get; set; }

	/// <summary>The contractStatus filter; omitted when null.</summary>
	[AliasAs("contractStatus")]
	[Query(CollectionFormat.Multi)]
	public string[]? ContractStatus { get; set; }

	/// <summary>The coverageStatus filter; omitted when null.</summary>
	[AliasAs("coverageStatus")]
	[Query(CollectionFormat.Multi)]
	public string[]? CoverageStatus { get; set; }

	/// <summary>The supportType filter; omitted when null.</summary>
	[AliasAs("supportType")]
	[Query(CollectionFormat.Multi)]
	public string[]? SupportType { get; set; }

	/// <summary>The supportTier filter; omitted when null.</summary>
	[AliasAs("supportTier")]
	[Query(CollectionFormat.Multi)]
	public string[]? SupportTier { get; set; }

	/// <summary>The partnerName filter; omitted when null.</summary>
	[AliasAs("partnerName")]
	[Query(CollectionFormat.Multi)]
	public string[]? PartnerName { get; set; }

	/// <summary>The telemetryStatus filter; omitted when null.</summary>
	[AliasAs("telemetryStatus")]
	[Query(CollectionFormat.Multi)]
	public string[]? TelemetryStatus { get; set; }

	/// <summary>The lastSignalType filter; omitted when null.</summary>
	[AliasAs("lastSignalType")]
	[Query(CollectionFormat.Multi)]
	public string[]? LastSignalType { get; set; }

	/// <summary>The currentHardwareMilestone filter; omitted when null.</summary>
	[AliasAs("currentHardwareMilestone")]
	[Query(CollectionFormat.Multi)]
	public string[]? CurrentHardwareMilestone { get; set; }

	/// <summary>The currentSoftwareMilestone filter; omitted when null.</summary>
	[AliasAs("currentSoftwareMilestone")]
	[Query(CollectionFormat.Multi)]
	public string[]? CurrentSoftwareMilestone { get; set; }

	/// <summary>The nextHardwareMilestone filter; omitted when null.</summary>
	[AliasAs("nextHardwareMilestone")]
	[Query(CollectionFormat.Multi)]
	public string[]? NextHardwareMilestone { get; set; }

	/// <summary>The nextSoftwareMilestone filter; omitted when null.</summary>
	[AliasAs("nextSoftwareMilestone")]
	[Query(CollectionFormat.Multi)]
	public string[]? NextSoftwareMilestone { get; set; }

	/// <summary>The lastSignalBefore filter; omitted when null.</summary>
	[AliasAs("lastSignalBefore")]
	public DateTimeOffset? LastSignalBefore { get; set; }

	/// <summary>The lastSignalAfter filter; omitted when null.</summary>
	[AliasAs("lastSignalAfter")]
	public DateTimeOffset? LastSignalAfter { get; set; }

	/// <summary>The coverageEndBefore filter; omitted when null.</summary>
	[AliasAs("coverageEndBefore")]
	public DateTimeOffset? CoverageEndBefore { get; set; }

	/// <summary>The coverageEndAfter filter; omitted when null.</summary>
	[AliasAs("coverageEndAfter")]
	public DateTimeOffset? CoverageEndAfter { get; set; }

	/// <summary>The warrantyEndBefore filter; omitted when null.</summary>
	[AliasAs("warrantyEndBefore")]
	public DateTimeOffset? WarrantyEndBefore { get; set; }

	/// <summary>The warrantyEndAfter filter; omitted when null.</summary>
	[AliasAs("warrantyEndAfter")]
	public DateTimeOffset? WarrantyEndAfter { get; set; }

	/// <summary>The shipDateBefore filter; omitted when null.</summary>
	[AliasAs("shipDateBefore")]
	public DateTimeOffset? ShipDateBefore { get; set; }

	/// <summary>The shipDateAfter filter; omitted when null.</summary>
	[AliasAs("shipDateAfter")]
	public DateTimeOffset? ShipDateAfter { get; set; }

	/// <summary>The endOfSoftwareMaintenanceBefore filter; omitted when null.</summary>
	[AliasAs("endOfSoftwareMaintenanceBefore")]
	public DateTimeOffset? EndOfSoftwareMaintenanceBefore { get; set; }

	/// <summary>The endOfSoftwareMaintenanceAfter filter; omitted when null.</summary>
	[AliasAs("endOfSoftwareMaintenanceAfter")]
	public DateTimeOffset? EndOfSoftwareMaintenanceAfter { get; set; }

	/// <summary>The nextHardwareMilestoneDateBefore filter; omitted when null.</summary>
	[AliasAs("nextHardwareMilestoneDateBefore")]
	public DateTimeOffset? NextHardwareMilestoneDateBefore { get; set; }

	/// <summary>The nextHardwareMilestoneDateAfter filter; omitted when null.</summary>
	[AliasAs("nextHardwareMilestoneDateAfter")]
	public DateTimeOffset? NextHardwareMilestoneDateAfter { get; set; }

	/// <summary>The nextSoftwareMilestoneDateBefore filter; omitted when null.</summary>
	[AliasAs("nextSoftwareMilestoneDateBefore")]
	public DateTimeOffset? NextSoftwareMilestoneDateBefore { get; set; }

	/// <summary>The nextSoftwareMilestoneDateAfter filter; omitted when null.</summary>
	[AliasAs("nextSoftwareMilestoneDateAfter")]
	public DateTimeOffset? NextSoftwareMilestoneDateAfter { get; set; }

	/// <summary>The hardwareLastDateOfSupportBefore filter; omitted when null.</summary>
	[AliasAs("hardwareLastDateOfSupportBefore")]
	public DateTimeOffset? HardwareLastDateOfSupportBefore { get; set; }

	/// <summary>The hardwareLastDateOfSupportAfter filter; omitted when null.</summary>
	[AliasAs("hardwareLastDateOfSupportAfter")]
	public DateTimeOffset? HardwareLastDateOfSupportAfter { get; set; }

	/// <summary>The softwareLastDateOfSupportBefore filter; omitted when null.</summary>
	[AliasAs("softwareLastDateOfSupportBefore")]
	public DateTimeOffset? SoftwareLastDateOfSupportBefore { get; set; }

	/// <summary>The softwareLastDateOfSupportAfter filter; omitted when null.</summary>
	[AliasAs("softwareLastDateOfSupportAfter")]
	public DateTimeOffset? SoftwareLastDateOfSupportAfter { get; set; }

	/// <summary>The hasCriticalOrHighSecurityAdvisories filter; omitted when null.</summary>
	[AliasAs("hasCriticalOrHighSecurityAdvisories")]
	public bool? HasCriticalOrHighSecurityAdvisories { get; set; }
}
