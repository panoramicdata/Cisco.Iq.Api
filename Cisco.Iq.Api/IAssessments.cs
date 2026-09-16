using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>The Cisco IQ assessments operations.</summary>
public interface IAssessments
{
	/// <summary>Gets one page of SecurityAdvisories.</summary>
	Task<IResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesAsync(GetSecurityAdvisoriesRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates SecurityAdvisories, following each next Link until absent.</summary>
	IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesAllAsync(GetSecurityAdvisoriesRequest request, CancellationToken cancellationToken);

	/// <summary>Gets SecurityAdvisory.</summary>
	Task<IResponse<SecurityAdvisory>> GetSecurityAdvisoryAsync(GetSecurityAdvisoryRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of AffectedAssetsForSecurityAdvisory.</summary>
	Task<IResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForSecurityAdvisoryAsync(GetAffectedAssetsForSecurityAdvisoryRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates AffectedAssetsForSecurityAdvisory, following each next Link until absent.</summary>
	IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForSecurityAdvisoryAllAsync(GetAffectedAssetsForSecurityAdvisoryRequest request, CancellationToken cancellationToken);

	/// <summary>Gets AffectedAssetForSecurityAdvisory.</summary>
	Task<IResponse<AffectedAsset>> GetAffectedAssetForSecurityAdvisoryAsync(GetAffectedAssetForSecurityAdvisoryRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of SecurityAdvisoriesForAsset.</summary>
	Task<IResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesForAssetAsync(GetSecurityAdvisoriesForAssetRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates SecurityAdvisoriesForAsset, following each next Link until absent.</summary>
	IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesForAssetAllAsync(GetSecurityAdvisoriesForAssetRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of FieldNotices.</summary>
	Task<IResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesAsync(GetFieldNoticesRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates FieldNotices, following each next Link until absent.</summary>
	IAsyncEnumerable<FieldNotice> GetFieldNoticesAllAsync(GetFieldNoticesRequest request, CancellationToken cancellationToken);

	/// <summary>Gets FieldNotice.</summary>
	Task<IResponse<FieldNotice>> GetFieldNoticeAsync(GetFieldNoticeRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of AffectedAssetsForFieldNotice.</summary>
	Task<IResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForFieldNoticeAsync(GetAffectedAssetsForFieldNoticeRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates AffectedAssetsForFieldNotice, following each next Link until absent.</summary>
	IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForFieldNoticeAllAsync(GetAffectedAssetsForFieldNoticeRequest request, CancellationToken cancellationToken);

	/// <summary>Gets AffectedAssetForFieldNotice.</summary>
	Task<IResponse<AffectedAsset>> GetAffectedAssetForFieldNoticeAsync(GetAffectedAssetForFieldNoticeRequest request, CancellationToken cancellationToken);

	/// <summary>Gets one page of FieldNoticesForAsset.</summary>
	Task<IResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesForAssetAsync(GetFieldNoticesForAssetRequest request, CancellationToken cancellationToken);

	/// <summary>Enumerates FieldNoticesForAsset, following each next Link until absent.</summary>
	IAsyncEnumerable<FieldNotice> GetFieldNoticesForAssetAllAsync(GetFieldNoticesForAssetRequest request, CancellationToken cancellationToken);
}
