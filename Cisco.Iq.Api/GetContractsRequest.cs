using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetContractsAsync.</summary>
public sealed record GetContractsRequest : IRequest<CiscoIqPage<Contract>>
{
	/// <summary>The filter for this request.</summary>
	public ContractFilter? Filter { get; init; }
}
