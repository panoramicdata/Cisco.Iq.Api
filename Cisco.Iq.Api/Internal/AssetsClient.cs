using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api.Internal;

internal sealed class AssetsClient(IAssetsApi api, PageReader reader) : IAssets
{
	public Task<CiscoIqPage<Asset>> GetAssetsAsync(AssetFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAssetsAsync(filter, cancellationToken));

	public IAsyncEnumerable<Asset> GetAssetsAllAsync(AssetFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetAssetsAsync(filter, cancellationToken), cancellationToken);

	public Task<Asset> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAssetAsync(assetId, cancellationToken));

	public Task<AssetLifecycle> GetAssetLifecycleAsync(string assetId, CiscoIqMilestoneType milestoneType = CiscoIqMilestoneType.Hardware, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAssetLifecycleAsync(assetId, milestoneType, cancellationToken));

	public Task<CiscoIqPage<AssetRelationship>> GetAssetRelationshipsAsync(string assetId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAssetRelationshipsAsync(assetId, filter, cancellationToken));

	public IAsyncEnumerable<AssetRelationship> GetAssetRelationshipsAllAsync(string assetId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetAssetRelationshipsAsync(assetId, filter, cancellationToken), cancellationToken);

	public Task<CiscoIqPage<Contract>> GetContractsAsync(ContractFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetContractsAsync(filter, cancellationToken));

	public IAsyncEnumerable<Contract> GetContractsAllAsync(ContractFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetContractsAsync(filter, cancellationToken), cancellationToken);

	public Task<Contract> GetContractAsync(string contractNumber, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetContractAsync(contractNumber, cancellationToken));
}
