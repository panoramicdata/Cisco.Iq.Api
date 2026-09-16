using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ Contract response.</summary>
public class Contract
{
	/// <summary>The customerId value, when included in the response.</summary>
	[JsonProperty("customerId")]
	public string? CustomerId { get; set; }

	/// <summary>The contractNumber value, when included in the response.</summary>
	[JsonProperty("contractNumber")]
	public string? ContractNumber { get; set; }

	/// <summary>The contractStatus value, when included in the response.</summary>
	[JsonProperty("contractStatus")]
	public string? ContractStatus { get; set; }

	/// <summary>The serviceLevel value, when included in the response.</summary>
	[JsonProperty("serviceLevel")]
	public string? ServiceLevel { get; set; }

	/// <summary>The supportTier value, when included in the response.</summary>
	[JsonProperty("supportTier")]
	public string? SupportTier { get; set; }

	/// <summary>The supportType value, when included in the response.</summary>
	[JsonProperty("supportType")]
	public string? SupportType { get; set; }

	/// <summary>The serviceLevelAgreementDescription value, when included in the response.</summary>
	[JsonProperty("serviceLevelAgreementDescription")]
	public string? ServiceLevelAgreementDescription { get; set; }

	/// <summary>The partnerName value, when included in the response.</summary>
	[JsonProperty("partnerName")]
	public string? PartnerName { get; set; }

	/// <summary>The contractStartDate value, when included in the response.</summary>
	[JsonProperty("contractStartDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? ContractStartDate { get; set; }

	/// <summary>The contractEndDate value, when included in the response.</summary>
	[JsonProperty("contractEndDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? ContractEndDate { get; set; }

	/// <summary>The coveredAssetCount value, when included in the response.</summary>
	[JsonProperty("coveredAssetCount")]
	public int? CoveredAssetCount { get; set; }
}
