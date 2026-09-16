using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetContractAsync.</summary>
public sealed record GetContractRequest : IRequest<Contract>
{
	/// <summary>The contractNumber for this request.</summary>
	public required string ContractNumber { get; init; }
}
