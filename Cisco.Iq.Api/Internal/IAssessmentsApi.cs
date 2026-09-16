using Cisco.Iq.Api.Data;
using Refit;

namespace Cisco.Iq.Api.Internal;

internal interface IAssessmentsApi
{
	[Get("/securityAdvisories")]
	Task<ApiResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesAsync([Query] SecurityAdvisoryFilter? filter, CancellationToken cancellationToken);

	[Get("/securityAdvisories/{psirtId}")]
	Task<ApiResponse<SecurityAdvisory>> GetSecurityAdvisoryAsync(int psirtId, CancellationToken cancellationToken);

	[Get("/securityAdvisories/{psirtId}/assets")]
	Task<ApiResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForSecurityAdvisoryAsync(int psirtId, [Query] CiscoIqFilter? filter, CancellationToken cancellationToken);

	[Get("/securityAdvisories/{psirtId}/assets/{assetId}")]
	Task<ApiResponse<AffectedAsset>> GetAffectedAssetForSecurityAdvisoryAsync(int psirtId, string assetId, CancellationToken cancellationToken);

	[Get("/assets/{assetId}/securityAdvisories")]
	Task<ApiResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesForAssetAsync(string assetId, [Query] SecurityAdvisoryFilter? filter, CancellationToken cancellationToken);

	[Get("/fieldNotices")]
	Task<ApiResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesAsync([Query] FieldNoticeFilter? filter, CancellationToken cancellationToken);

	[Get("/fieldNotices/{fieldNoticeId}")]
	Task<ApiResponse<FieldNotice>> GetFieldNoticeAsync(int fieldNoticeId, CancellationToken cancellationToken);

	[Get("/fieldNotices/{fieldNoticeId}/assets")]
	Task<ApiResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForFieldNoticeAsync(int fieldNoticeId, [Query] CiscoIqFilter? filter, CancellationToken cancellationToken);

	[Get("/fieldNotices/{fieldNoticeId}/assets/{assetId}")]
	Task<ApiResponse<AffectedAsset>> GetAffectedAssetForFieldNoticeAsync(int fieldNoticeId, string assetId, CancellationToken cancellationToken);

	[Get("/assets/{assetId}/fieldNotices")]
	Task<ApiResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesForAssetAsync(string assetId, [Query] FieldNoticeFilter? filter, CancellationToken cancellationToken);
}
