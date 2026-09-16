using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api.Internal;

internal sealed class AssessmentsClient(IAssessmentsApi api, PageReader reader) : IAssessments
{
	public Task<IResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesAsync(GetSecurityAdvisoriesRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoriesAsync(request.Filter, cancellationToken));

	public IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesAllAsync(GetSecurityAdvisoriesRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetSecurityAdvisoriesAsync(request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<SecurityAdvisory>> GetSecurityAdvisoryAsync(GetSecurityAdvisoryRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoryAsync(request.PsirtId, cancellationToken));

	public Task<IResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForSecurityAdvisoryAsync(GetAffectedAssetsForSecurityAdvisoryRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAffectedAssetsForSecurityAdvisoryAsync(request.PsirtId, request.Filter, cancellationToken));

	public IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForSecurityAdvisoryAllAsync(GetAffectedAssetsForSecurityAdvisoryRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetAffectedAssetsForSecurityAdvisoryAsync(request.PsirtId, request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<AffectedAsset>> GetAffectedAssetForSecurityAdvisoryAsync(GetAffectedAssetForSecurityAdvisoryRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAffectedAssetForSecurityAdvisoryAsync(request.PsirtId, request.AssetId, cancellationToken));

	public Task<IResponse<CiscoIqPage<SecurityAdvisory>>> GetSecurityAdvisoriesForAssetAsync(GetSecurityAdvisoriesForAssetRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoriesForAssetAsync(request.AssetId, request.Filter, cancellationToken));

	public IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesForAssetAllAsync(GetSecurityAdvisoriesForAssetRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetSecurityAdvisoriesForAssetAsync(request.AssetId, request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesAsync(GetFieldNoticesRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetFieldNoticesAsync(request.Filter, cancellationToken));

	public IAsyncEnumerable<FieldNotice> GetFieldNoticesAllAsync(GetFieldNoticesRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetFieldNoticesAsync(request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<FieldNotice>> GetFieldNoticeAsync(GetFieldNoticeRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetFieldNoticeAsync(request.FieldNoticeId, cancellationToken));

	public Task<IResponse<CiscoIqPage<AffectedAsset>>> GetAffectedAssetsForFieldNoticeAsync(GetAffectedAssetsForFieldNoticeRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAffectedAssetsForFieldNoticeAsync(request.FieldNoticeId, request.Filter, cancellationToken));

	public IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForFieldNoticeAllAsync(GetAffectedAssetsForFieldNoticeRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetAffectedAssetsForFieldNoticeAsync(request.FieldNoticeId, request.Filter, cancellationToken), cancellationToken);

	public Task<IResponse<AffectedAsset>> GetAffectedAssetForFieldNoticeAsync(GetAffectedAssetForFieldNoticeRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetAffectedAssetForFieldNoticeAsync(request.FieldNoticeId, request.AssetId, cancellationToken));

	public Task<IResponse<CiscoIqPage<FieldNotice>>> GetFieldNoticesForAssetAsync(GetFieldNoticesForAssetRequest request, CancellationToken cancellationToken)
		=> PageReader.ReadAsync(api.GetFieldNoticesForAssetAsync(request.AssetId, request.Filter, cancellationToken));

	public IAsyncEnumerable<FieldNotice> GetFieldNoticesForAssetAllAsync(GetFieldNoticesForAssetRequest request, CancellationToken cancellationToken)
		=> reader.EnumerateAsync(() => api.GetFieldNoticesForAssetAsync(request.AssetId, request.Filter, cancellationToken), cancellationToken);
}
