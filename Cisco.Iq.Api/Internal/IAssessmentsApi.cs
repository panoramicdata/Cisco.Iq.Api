using Cisco.Iq.Api.Data;
using Refit;

namespace Cisco.Iq.Api.Internal;

internal interface IAssessmentsApi
{
	[Get("/securityAdvisories")]
	Task<ApiResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesAsync([Query] SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/securityAdvisories/{psirtId}")]
	Task<ApiResponse<SecurityAdvisory>> GetSecurityAdvisoryAsync(int psirtId, CancellationToken cancellationToken = default);

	[Get("/securityAdvisories/{psirtId}/assets")]
	Task<ApiResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForSecurityAdvisoryAsync(int psirtId, [Query] CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/securityAdvisories/{psirtId}/assets/{assetId}")]
	Task<ApiResponse<AffectedAsset>> GetAffectedAssetForSecurityAdvisoryAsync(int psirtId, string assetId, CancellationToken cancellationToken = default);

	[Get("/assets/{assetId}/securityAdvisories")]
	Task<ApiResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesForAssetAsync(string assetId, [Query] SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/fieldNotices")]
	Task<ApiResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesAsync([Query] FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/fieldNotices/{fieldNoticeId}")]
	Task<ApiResponse<FieldNotice>> GetFieldNoticeAsync(int fieldNoticeId, CancellationToken cancellationToken = default);

	[Get("/fieldNotices/{fieldNoticeId}/assets")]
	Task<ApiResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForFieldNoticeAsync(int fieldNoticeId, [Query] CiscoIqFilter? filter = null, CancellationToken cancellationToken = default);

	[Get("/fieldNotices/{fieldNoticeId}/assets/{assetId}")]
	Task<ApiResponse<AffectedAsset>> GetAffectedAssetForFieldNoticeAsync(int fieldNoticeId, string assetId, CancellationToken cancellationToken = default);

	[Get("/assets/{assetId}/fieldNotices")]
	Task<ApiResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesForAssetAsync(string assetId, [Query] FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default);
}
