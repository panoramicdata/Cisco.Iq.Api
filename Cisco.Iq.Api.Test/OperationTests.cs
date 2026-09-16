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
		(await client.Assets.GetAssetsAsync()).Should().NotBeNull();
		(await client.Assets.GetAssetAsync("asset/one")).Should().NotBeNull();
		(await client.Assets.GetAssetLifecycleAsync("asset/one", CiscoIqMilestoneType.Software)).Should().NotBeNull();
		(await client.Assets.GetAssetRelationshipsAsync("asset/one")).Should().NotBeNull();
		(await client.Assets.GetContractsAsync()).Should().NotBeNull();
		(await client.Assets.GetContractAsync("contract/one")).Should().NotBeNull();
		(await client.Assessments.GetSecurityAdvisoriesAsync()).Should().NotBeNull();
		(await client.Assessments.GetSecurityAdvisoryAsync(123)).Should().NotBeNull();
		(await client.Assessments.GetAffectedAssetsForSecurityAdvisoryAsync(123)).Should().NotBeNull();
		(await client.Assessments.GetAffectedAssetForSecurityAdvisoryAsync(123, "asset/one")).Should().NotBeNull();
		(await client.Assessments.GetSecurityAdvisoriesForAssetAsync("asset/one")).Should().NotBeNull();
		(await client.Assessments.GetFieldNoticesAsync()).Should().NotBeNull();
		(await client.Assessments.GetFieldNoticeAsync(123)).Should().NotBeNull();
		(await client.Assessments.GetAffectedAssetsForFieldNoticeAsync(123)).Should().NotBeNull();
		(await client.Assessments.GetAffectedAssetForFieldNoticeAsync(123, "asset/one")).Should().NotBeNull();
		(await client.Assessments.GetFieldNoticesForAssetAsync("asset/one")).Should().NotBeNull();
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
			if (calls % 2 == 1) { response.Headers.Add("Link", "<?cursor=next>; rel=\"next\""); }
			else { request.RequestUri!.Query.Should().Be("?cursor=next"); }
			return Task.FromResult(response);
		}), PagingAndRetryTests.Exchange);
		await CountAsync(client.Assets.GetAssetsAllAsync());
		await CountAsync(client.Assets.GetAssetRelationshipsAllAsync("asset"));
		await CountAsync(client.Assets.GetContractsAllAsync());
		await CountAsync(client.Assessments.GetSecurityAdvisoriesAllAsync());
		await CountAsync(client.Assessments.GetAffectedAssetsForSecurityAdvisoryAllAsync(123));
		await CountAsync(client.Assessments.GetSecurityAdvisoriesForAssetAllAsync("asset"));
		await CountAsync(client.Assessments.GetFieldNoticesAllAsync());
		await CountAsync(client.Assessments.GetAffectedAssetsForFieldNoticeAllAsync(123));
		await CountAsync(client.Assessments.GetFieldNoticesForAssetAllAsync("asset"));
		calls.Should().Be(18);
	}

	private static async Task CountAsync<T>(IAsyncEnumerable<T> source)
	{
		var count = 0;
		await foreach (var item in source) { item.Should().NotBeNull(); count++; }
		count.Should().Be(2);
	}
}
