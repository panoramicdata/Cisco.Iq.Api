using Refit;

namespace Cisco.Iq.Api;

/// <summary>Filters for Cisco IQ collections requests.</summary>
public class CiscoIqFilter
{
	/// <summary>The max filter; omitted when null.</summary>
	[AliasAs("max")]
	public int? Max { get; set; }

	/// <summary>The offset filter; omitted when null.</summary>
	[AliasAs("offset")]
	public int? Offset { get; set; }

	/// <summary>The sort filter; omitted when null.</summary>
	[AliasAs("sort")]
	public string? Sort { get; set; }

	/// <summary>The order filter; omitted when null.</summary>
	[AliasAs("order")]
	public CiscoIqSortOrder? Order { get; set; }

	/// <summary>The fields filter; omitted when null.</summary>
	[AliasAs("fields")]
	public string? Fields { get; set; }
}
