// Optional parameters preserve the published API and conventional cancellation-token usage.
#pragma warning disable S2360

using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>The Cisco IQ assessments operations.</summary>
public interface IAssessments
{
	/// <summary>Gets one page of SecurityAdvisories.</summary>
	Task<CiscoIqPage<SecurityAdvisory>> GetSecurityAdvisoriesAsync(SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates SecurityAdvisories, following each next Link until absent.</summary>
	IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesAllAsync(SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets SecurityAdvisory.</summary>
	Task<SecurityAdvisory> GetSecurityAdvisoryAsync(int psirtId, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of AffectedAssetsForSecurityAdvisory.</summary>
	Task<CiscoIqPage<AffectedAsset>> GetAffectedAssetsForSecurityAdvisoryAsync(int psirtId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates AffectedAssetsForSecurityAdvisory, following each next Link until absent.</summary>
	IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForSecurityAdvisoryAllAsync(int psirtId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets AffectedAssetForSecurityAdvisory.</summary>
	Task<AffectedAsset> GetAffectedAssetForSecurityAdvisoryAsync(int psirtId, string assetId, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of SecurityAdvisoriesForAsset.</summary>
	Task<CiscoIqPage<SecurityAdvisory>> GetSecurityAdvisoriesForAssetAsync(string assetId, SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates SecurityAdvisoriesForAsset, following each next Link until absent.</summary>
	IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesForAssetAllAsync(string assetId, SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of FieldNotices.</summary>
	Task<CiscoIqPage<FieldNotice>> GetFieldNoticesAsync(FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates FieldNotices, following each next Link until absent.</summary>
	IAsyncEnumerable<FieldNotice> GetFieldNoticesAllAsync(FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets FieldNotice.</summary>
	Task<FieldNotice> GetFieldNoticeAsync(int fieldNoticeId, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of AffectedAssetsForFieldNotice.</summary>
	Task<CiscoIqPage<AffectedAsset>> GetAffectedAssetsForFieldNoticeAsync(int fieldNoticeId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates AffectedAssetsForFieldNotice, following each next Link until absent.</summary>
	IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForFieldNoticeAllAsync(int fieldNoticeId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Gets AffectedAssetForFieldNotice.</summary>
	Task<AffectedAsset> GetAffectedAssetForFieldNoticeAsync(int fieldNoticeId, string assetId, CancellationToken cancellationToken = default);

	/// <summary>Gets one page of FieldNoticesForAsset.</summary>
	Task<CiscoIqPage<FieldNotice>> GetFieldNoticesForAssetAsync(string assetId, FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);

	/// <summary>Enumerates FieldNoticesForAsset, following each next Link until absent.</summary>
	IAsyncEnumerable<FieldNotice> GetFieldNoticesForAssetAllAsync(string assetId, FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);
}
