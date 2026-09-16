using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api.Internal;

internal sealed class AssetsClient(IAssetsApi api, PageReader reader) : IAssets
{
	public Task<IResponse<CiscoIqPage<Asset>>> GetAssetsAsync(GetAssetsRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAssetsAsync(request.Filter, cancellationToken));

	public IAsyncEnumerable<Asset> GetAssetsAllAsync(GetAssetsRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetAssetsAsync(request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<Asset>> GetAssetAsync(GetAssetRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAssetAsync(request.AssetId, cancellationToken));

	public Task<IResponse<AssetLifecycle>> GetAssetLifecycleAsync(GetAssetLifecycleRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAssetLifecycleAsync(request.AssetId, request.MilestoneType, cancellationToken));

	public Task<IResponse<CiscoIqPage<AssetRelationship>>> GetAssetRelationshipsAsync(GetAssetRelationshipsRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAssetRelationshipsAsync(request.AssetId, request.Filter, cancellationToken));

	public IAsyncEnumerable<AssetRelationship> GetAssetRelationshipsAllAsync(GetAssetRelationshipsRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetAssetRelationshipsAsync(request.AssetId, request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<CiscoIqPage<Contract>>> GetContractsAsync(GetContractsRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetContractsAsync(request.Filter, cancellationToken));

	public IAsyncEnumerable<Contract> GetContractsAllAsync(GetContractsRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetContractsAsync(request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<Contract>> GetContractAsync(GetContractRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetContractAsync(request.ContractNumber, cancellationToken));
}
