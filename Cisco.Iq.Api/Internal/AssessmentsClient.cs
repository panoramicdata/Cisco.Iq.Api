using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api.Internal;

internal sealed class AssessmentsClient(IAssessmentsApi api, PageReader reader) : IAssessments
{
	public Task<CiscoIqPage<SecurityAdvisory>> GetSecurityAdvisoriesAsync(SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoriesAsync(filter, cancellationToken));

	public IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesAllAsync(SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetSecurityAdvisoriesAsync(filter, cancellationToken), cancellationToken);

	public Task<SecurityAdvisory> GetSecurityAdvisoryAsync(int psirtId, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoryAsync(psirtId, cancellationToken));

	public Task<CiscoIqPage<AffectedAsset>> GetAffectedAssetsForSecurityAdvisoryAsync(int psirtId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAffectedAssetsForSecurityAdvisoryAsync(psirtId, filter, cancellationToken));

	public IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForSecurityAdvisoryAllAsync(int psirtId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetAffectedAssetsForSecurityAdvisoryAsync(psirtId, filter, cancellationToken), cancellationToken);

	public Task<AffectedAsset> GetAffectedAssetForSecurityAdvisoryAsync(int psirtId, string assetId, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAffectedAssetForSecurityAdvisoryAsync(psirtId, assetId, cancellationToken));

	public Task<CiscoIqPage<SecurityAdvisory>> GetSecurityAdvisoriesForAssetAsync(string assetId, SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetSecurityAdvisoriesForAssetAsync(assetId, filter, cancellationToken));

	public IAsyncEnumerable<SecurityAdvisory> GetSecurityAdvisoriesForAssetAllAsync(string assetId, SecurityAdvisoryFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetSecurityAdvisoriesForAssetAsync(assetId, filter, cancellationToken), cancellationToken);

	public Task<CiscoIqPage<FieldNotice>> GetFieldNoticesAsync(FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetFieldNoticesAsync(filter, cancellationToken));

	public IAsyncEnumerable<FieldNotice> GetFieldNoticesAllAsync(FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetFieldNoticesAsync(filter, cancellationToken), cancellationToken);

	public Task<FieldNotice> GetFieldNoticeAsync(int fieldNoticeId, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetFieldNoticeAsync(fieldNoticeId, cancellationToken));

	public Task<CiscoIqPage<AffectedAsset>> GetAffectedAssetsForFieldNoticeAsync(int fieldNoticeId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAffectedAssetsForFieldNoticeAsync(fieldNoticeId, filter, cancellationToken));

	public IAsyncEnumerable<AffectedAsset> GetAffectedAssetsForFieldNoticeAllAsync(int fieldNoticeId, CiscoIqFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetAffectedAssetsForFieldNoticeAsync(fieldNoticeId, filter, cancellationToken), cancellationToken);

	public Task<AffectedAsset> GetAffectedAssetForFieldNoticeAsync(int fieldNoticeId, string assetId, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetAffectedAssetForFieldNoticeAsync(fieldNoticeId, assetId, cancellationToken));

	public Task<CiscoIqPage<FieldNotice>> GetFieldNoticesForAssetAsync(string assetId, FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default)
		=> PageReader.ReadAsync(api.GetFieldNoticesForAssetAsync(assetId, filter, cancellationToken));

	public IAsyncEnumerable<FieldNotice> GetFieldNoticesForAssetAllAsync(string assetId, FieldNoticeFilter? filter = null, CancellationToken cancellationToken = default)
		=> reader.EnumerateAsync(() => api.GetFieldNoticesForAssetAsync(assetId, filter, cancellationToken), cancellationToken);
}
