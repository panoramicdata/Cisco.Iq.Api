using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ AssetLifecycle response.</summary>
public class AssetLifecycle
{
	/// <summary>The assetId value, when included in the response.</summary>
	[JsonProperty("assetId")]
	public string? AssetId { get; set; }

	/// <summary>The serialNumber value, when included in the response.</summary>
	[JsonProperty("serialNumber")]
	public string? SerialNumber { get; set; }

	/// <summary>The productId value, when included in the response.</summary>
	[JsonProperty("productId")]
	public string? ProductId { get; set; }

	/// <summary>The milestoneType value, when included in the response.</summary>
	[JsonProperty("milestoneType")]
	public string? MilestoneType { get; set; }

	/// <summary>The currentMilestone value, when included in the response.</summary>
	[JsonProperty("currentMilestone")]
	public string? CurrentMilestone { get; set; }

	/// <summary>The nextMilestone value, when included in the response.</summary>
	[JsonProperty("nextMilestone")]
	public string? NextMilestone { get; set; }

	/// <summary>The currentMilestoneDate value, when included in the response.</summary>
	[JsonProperty("currentMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? CurrentMilestoneDate { get; set; }

	/// <summary>The nextMilestoneDate value, when included in the response.</summary>
	[JsonProperty("nextMilestoneDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? NextMilestoneDate { get; set; }

	/// <summary>The lastDateOfSupport value, when included in the response.</summary>
	[JsonProperty("lastDateOfSupport")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? LastDateOfSupport { get; set; }

	/// <summary>The bulletinReference value, when included in the response.</summary>
	[JsonProperty("bulletinReference")]
	public string? BulletinReference { get; set; }

	/// <summary>The bulletinTitle value, when included in the response.</summary>
	[JsonProperty("bulletinTitle")]
	public string? BulletinTitle { get; set; }

	/// <summary>The bulletinUrl value, when included in the response.</summary>
	[JsonProperty("bulletinUrl")]
	public string? BulletinUrl { get; set; }

	/// <summary>The endOfLifeAnnouncementDate value, when included in the response.</summary>
	[JsonProperty("endOfLifeAnnouncementDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfLifeAnnouncementDate { get; set; }

	/// <summary>The endOfSaleDate value, when included in the response.</summary>
	[JsonProperty("endOfSaleDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfSaleDate { get; set; }

	/// <summary>The lastShipDate value, when included in the response.</summary>
	[JsonProperty("lastShipDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? LastShipDate { get; set; }

	/// <summary>The endOfRoutineFailureAnalysisDate value, when included in the response.</summary>
	[JsonProperty("endOfRoutineFailureAnalysisDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfRoutineFailureAnalysisDate { get; set; }

	/// <summary>The endOfNewServiceAttachmentDate value, when included in the response.</summary>
	[JsonProperty("endOfNewServiceAttachmentDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfNewServiceAttachmentDate { get; set; }

	/// <summary>The endOfServiceContractRenewalDate value, when included in the response.</summary>
	[JsonProperty("endOfServiceContractRenewalDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfServiceContractRenewalDate { get; set; }

	/// <summary>The endOfSoftwareMaintenance value, when included in the response.</summary>
	[JsonProperty("endOfSoftwareMaintenance")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfSoftwareMaintenance { get; set; }

	/// <summary>The endOfVulnerabilitySecuritySupport value, when included in the response.</summary>
	[JsonProperty("endOfVulnerabilitySecuritySupport")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? EndOfVulnerabilitySecuritySupport { get; set; }
}
