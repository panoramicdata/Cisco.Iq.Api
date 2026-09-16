using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>The Cisco IQ assets operations.</summary>
public interface IAssets
{
	/// <summary>Gets one page of Assets.</summary>
	Task<IResponse<CiscoIqPage<Asset>>> GetAssetsAsync(GetAssetsRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates Assets, following each next Link until absent.</summary>
	IAsyncEnumerable<Asset> GetAssetsAllAsync(GetAssetsRequest request, CancellationToken cancellationToken);

	/// <summary>Gets Asset.</summary>
	Task<IResponse<Asset>> GetAssetAsync(GetAssetRequest request, CancellationToken cancellationToken);

	/// <summary>Gets AssetLifecycle.</summary>
	Task<IResponse<AssetLifecycle>> GetAssetLifecycleAsync(GetAssetLifecycleRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of AssetRelationships.</summary>
	Task<IResponse<CiscoIqPage<AssetRelationship>>> GetAssetRelationshipsAsync(GetAssetRelationshipsRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates AssetRelationships, following each next Link until absent.</summary>
	IAsyncEnumerable<AssetRelationship> GetAssetRelationshipsAllAsync(GetAssetRelationshipsRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of Contracts.</summary>
	Task<IResponse<CiscoIqPage<Contract>>> GetContractsAsync(GetContractsRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates Contracts, following each next Link until absent.</summary>
	IAsyncEnumerable<Contract> GetContractsAllAsync(GetContractsRequest request, CancellationToken cancellationToken);

	/// <summary>Gets Contract.</summary>
	Task<IResponse<Contract>> GetContractAsync(GetContractRequest request, CancellationToken cancellationToken);
}
