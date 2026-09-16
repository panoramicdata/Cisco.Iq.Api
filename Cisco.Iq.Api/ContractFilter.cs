using Refit;

namespace Cisco.Iq.Api;

/// <summary>Filters for Cisco IQ Contract requests.</summary>
public class ContractFilter : CiscoIqFilter
{
	/// <summary>The contractNumber filter; omitted when null.</summary>
	[AliasAs("contractNumber")]
	[Query(CollectionFormat.Multi)]
	public string[]? ContractNumber { get; set; }

	/// <summary>The contractStatus filter; omitted when null.</summary>
	[AliasAs("contractStatus")]
	[Query(CollectionFormat.Multi)]
	public string[]? ContractStatus { get; set; }

	/// <summary>The serviceLevel filter; omitted when null.</summary>
	[AliasAs("serviceLevel")]
	[Query(CollectionFormat.Multi)]
	public string[]? ServiceLevel { get; set; }

	/// <summary>The supportTier filter; omitted when null.</summary>
	[AliasAs("supportTier")]
	[Query(CollectionFormat.Multi)]
	public string[]? SupportTier { get; set; }

	/// <summary>The partnerName filter; omitted when null.</summary>
	[AliasAs("partnerName")]
	[Query(CollectionFormat.Multi)]
	public string[]? PartnerName { get; set; }

	/// <summary>The contractEndBefore filter; omitted when null.</summary>
	[AliasAs("contractEndBefore")]
	public DateTimeOffset? ContractEndBefore { get; set; }

	/// <summary>The contractEndAfter filter; omitted when null.</summary>
	[AliasAs("contractEndAfter")]
	public DateTimeOffset? ContractEndAfter { get; set; }
}
