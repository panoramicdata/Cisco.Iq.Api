namespace Cisco.Iq.Api.Test;

public class OperationTests
{
	[Fact]
	public async Task AllOperations_UseDocumentedGetRoutes_AndDeserializeTheirResponseShape()
	{
		var routes = new Queue<(string Path, bool Collection)>([
			("/assets", true),
			("/assets/asset%2Fone", false),
			("/assets/asset%2Fone/lifecycle", false),
			("/assets/asset%2Fone/relationships", true),
			("/contracts", true),
			("/contracts/contract%2Fone", false),
			("/securityAdvisories", true),
			("/securityAdvisories/123", false),
			("/securityAdvisories/123/assets", true),
			("/securityAdvisories/123/assets/asset%2Fone", false),
			("/assets/asset%2Fone/securityAdvisories", true),
			("/fieldNotices", true),
			("/fieldNotices/123", false),
			("/fieldNotices/123/assets", true),
			("/fieldNotices/123/assets/asset%2Fone", false),
			("/assets/asset%2Fone/fieldNotices", true),
		]);
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((request, _) =>
		{
			var route = routes.Dequeue();
			request.Method.Should().Be(HttpMethod.Get);
			request.RequestUri!.AbsolutePath.Should().Be("/ciq-rest/api/v0" + route.Path);
			return Task.FromResult(TestTransport.Json(route.Collection ? "{\"items\":[{}],\"meta\":{\"count\":null}}" : "{}"));
		}), PagingAndRetryTests.Exchange);
		VerifyResponse(await client.Assets.GetAssetsAsync(new GetAssetsRequest(), CancellationToken.None));
		VerifyResponse(await client.Assets.GetAssetAsync(new GetAssetRequest {AssetId = "asset/one"}, CancellationToken.None));
		VerifyResponse(await client.Assets.GetAssetLifecycleAsync(new GetAssetLifecycleRequest {AssetId = "asset/one", MilestoneType = CiscoIqMilestoneType.Software}, CancellationToken.None));
		VerifyResponse(await client.Assets.GetAssetRelationshipsAsync(new GetAssetRelationshipsRequest {AssetId = "asset/one"}, CancellationToken.None));
		VerifyResponse(await client.Assets.GetContractsAsync(new GetContractsRequest(), CancellationToken.None));
		VerifyResponse(await client.Assets.GetContractAsync(new GetContractRequest {ContractNumber = "contract/one"}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetSecurityAdvisoriesAsync(new GetSecurityAdvisoriesRequest(), CancellationToken.None));
		VerifyResponse(await client.Assessments.GetSecurityAdvisoryAsync(new GetSecurityAdvisoryRequest {PsirtId = 123}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetAffectedAssetsForSecurityAdvisoryAsync(new GetAffectedAssetsForSecurityAdvisoryRequest {PsirtId = 123}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetAffectedAssetForSecurityAdvisoryAsync(new GetAffectedAssetForSecurityAdvisoryRequest {PsirtId = 123, AssetId = "asset/one"}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetSecurityAdvisoriesForAssetAsync(new GetSecurityAdvisoriesForAssetRequest {AssetId = "asset/one"}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetFieldNoticesAsync(new GetFieldNoticesRequest(), CancellationToken.None));
		VerifyResponse(await client.Assessments.GetFieldNoticeAsync(new GetFieldNoticeRequest {FieldNoticeId = 123}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetAffectedAssetsForFieldNoticeAsync(new GetAffectedAssetsForFieldNoticeRequest {FieldNoticeId = 123}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetAffectedAssetForFieldNoticeAsync(new GetAffectedAssetForFieldNoticeRequest {FieldNoticeId = 123, AssetId = "asset/one"}, CancellationToken.None));
		VerifyResponse(await client.Assessments.GetFieldNoticesForAssetAsync(new GetFieldNoticesForAssetRequest {AssetId = "asset/one"}, CancellationToken.None));
		routes.Should().BeEmpty();
	}

	[Fact]
	public async Task EveryCollectionCompanion_FollowsNextLink()
	{
		var calls = 0;
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((request, _) =>
		{
			calls++;
			var response = TestTransport.Json("{\"items\":[{}],\"meta\":{\"count\":null}}");
			if (calls % 2 != 0) { response.Headers.Add("Link", "<?cursor=next>; rel=\"next\""); }
			else { request.RequestUri!.Query.Should().Be("?cursor=next"); }
			return Task.FromResult(response);
		}), PagingAndRetryTests.Exchange);
		await CountAsync(client.Assets.GetAssetsAllAsync(new GetAssetsRequest(), CancellationToken.None));
		await CountAsync(client.Assets.GetAssetRelationshipsAllAsync(new GetAssetRelationshipsRequest {AssetId = "asset"}, CancellationToken.None));
		await CountAsync(client.Assets.GetContractsAllAsync(new GetContractsRequest(), CancellationToken.None));
		await CountAsync(client.Assessments.GetSecurityAdvisoriesAllAsync(new GetSecurityAdvisoriesRequest(), CancellationToken.None));
		await CountAsync(client.Assessments.GetAffectedAssetsForSecurityAdvisoryAllAsync(new GetAffectedAssetsForSecurityAdvisoryRequest {PsirtId = 123}, CancellationToken.None));
		await CountAsync(client.Assessments.GetSecurityAdvisoriesForAssetAllAsync(new GetSecurityAdvisoriesForAssetRequest {AssetId = "asset"}, CancellationToken.None));
		await CountAsync(client.Assessments.GetFieldNoticesAllAsync(new GetFieldNoticesRequest(), CancellationToken.None));
		await CountAsync(client.Assessments.GetAffectedAssetsForFieldNoticeAllAsync(new GetAffectedAssetsForFieldNoticeRequest {FieldNoticeId = 123}, CancellationToken.None));
		await CountAsync(client.Assessments.GetFieldNoticesForAssetAllAsync(new GetFieldNoticesForAssetRequest {AssetId = "asset"}, CancellationToken.None));
		calls.Should().Be(18);
	}

	private static void VerifyResponse<T>(IResponse<T> response)
	{
		response.Content.Should().NotBeNull();
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
	}

	private static async Task CountAsync<T>(IAsyncEnumerable<T> source)
	{
		var count = 0;
		await foreach (var item in source) { item.Should().NotBeNull(); count++; }
		count.Should().Be(2);
	}
}
