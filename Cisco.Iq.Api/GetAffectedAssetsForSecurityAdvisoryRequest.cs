using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAffectedAssetsForSecurityAdvisoryAsync.</summary>
public sealed record GetAffectedAssetsForSecurityAdvisoryRequest : IRequest<CiscoIqPage<AffectedAsset>>
{
	/// <summary>The psirtId for this request.</summary>
	public required int PsirtId { get; init; }
	/// <summary>The filter for this request.</summary>
	public CiscoIqFilter? Filter { get; init; }
}
