using Cisco.Iq.Api.Data;
using Refit;

namespace Cisco.Iq.Api.Internal;

internal interface IAssetsApi
{
	[Get("/assets")]
	Task<ApiResponse<CiscoIqPage<Asset>>> GetAssetsAsync([Query] AssetFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/assets/{assetId}")]
	Task<ApiResponse<Asset>> GetAssetAsync(string assetId, CancellationToken cancellationToken = default);

	[Get("/assets/{assetId}/lifecycle")]
	Task<ApiResponse<AssetLifecycle>> GetAssetLifecycleAsync(string assetId, [AliasAs("milestoneType")] CiscoIqMilestoneType milestoneType = CiscoIqMilestoneType.Hardware, CancellationToken cancellationToken = default);

	[Get("/assets/{assetId}/relationships")]
	Task<ApiResponse<CiscoIqPage<AssetRelationship>>> GetAssetRelationshipsAsync(string assetId, [Query] CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/contracts")]
	Task<ApiResponse<CiscoIqPage<Contract>>> GetContractsAsync([Query] ContractFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/contracts/{contractNumber}")]
	Task<ApiResponse<Contract>> GetContractAsync(string contractNumber, CancellationToken cancellationToken = default);
}
