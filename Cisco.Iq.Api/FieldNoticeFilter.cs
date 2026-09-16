using Refit;

namespace Cisco.Iq.Api;

/// <summary>Filters for Cisco IQ FieldNotice requests.</summary>
public class FieldNoticeFilter : CiscoIqFilter
{
	/// <summary>The impact filter; omitted when null.</summary>
	[AliasAs("impact")]
	[Query(CollectionFormat.Multi)]
	public string[]? Impact { get; set; }

	/// <summary>The vulnerabilityStatus filter for notices per asset; omitted when null. Leave unset for the account-wide notice collection.</summary>
	[AliasAs("vulnerabilityStatus")]
	[Query(CollectionFormat.Multi)]
	public string[]? VulnerabilityStatus { get; set; }
}
