// Optional parameters preserve the published API and conventional cancellation-token usage.
#pragma warning disable S2360

using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>The Cisco IQ assets operations.</summary>
public interface IAssets
{
	/// <summary>Gets one page of Assets.</summary>
	Task<CiscoIqPage<Asset>> GetAssetsAsync(AssetFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates Assets, following each next Link until absent.</summary>
	IAsyncEnumerable<Asset> GetAssetsAllAsync(AssetFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets Asset.</summary>
	Task<Asset> GetAssetAsync(string assetId, CancellationToken cancellationToken = default);

	/// <summary>Gets AssetLifecycle.</summary>
	Task<AssetLifecycle> GetAssetLifecycleAsync(string assetId, CiscoIqMilestoneType milestoneType = CiscoIqMilestoneType.Hardware, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of AssetRelationships.</summary>
	Task<CiscoIqPage<AssetRelationship>> GetAssetRelationshipsAsync(string assetId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates AssetRelationships, following each next Link until absent.</summary>
	IAsyncEnumerable<AssetRelationship> GetAssetRelationshipsAllAsync(string assetId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of Contracts.</summary>
	Task<CiscoIqPage<Contract>> GetContractsAsync(ContractFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates Contracts, following each next Link until absent.</summary>
	IAsyncEnumerable<Contract> GetContractsAllAsync(ContractFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets Contract.</summary>
	Task<Contract> GetContractAsync(string contractNumber, CancellationToken cancellationToken = default);
}
