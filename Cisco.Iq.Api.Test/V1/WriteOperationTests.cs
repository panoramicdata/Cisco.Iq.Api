using System.Net;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Identity;
using Cisco.Iq.Api.V1.Platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cisco.Iq.Api.Test.V1;

public class WriteOperationTests
{
	private static JsonSerializerSettings RequestSettings()
	{
		var settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new CiscoIqV1ContractResolver()
		};
		settings.Converters.Add(new CiscoIqV1OptionalConverter());
		return settings;
	}

	[Fact]
	public void Optional_DistinguishesUnsetValueAndNull()
	{
		var unset = default(CiscoIqV1Optional<string>);
		CiscoIqV1Optional<string> value = "x";
		var cleared = CiscoIqV1Optional.Null<string>();

		unset.IsSet.Should().BeFalse();
		unset.IsNull.Should().BeFalse();
		unset.BoxedValue.Should().BeNull();
		value.IsSet.Should().BeTrue();
		value.IsNull.Should().BeFalse();
		value.BoxedValue.Should().Be("x");
		CiscoIqV1Optional.Of(5).BoxedValue.Should().Be(5);
		cleared.IsSet.Should().BeTrue();
		cleared.IsNull.Should().BeTrue();
		cleared.BoxedValue.Should().BeNull();
	}

	[Fact]
	public void MergePatch_SendsSetValuesAndNullsAndOmitsUnsetMembers()
	{
		var settings = RequestSettings();
		var clear = new UpdatePhysicalAssetRequest { Criticality = CiscoIqV1Optional.Null<AssetCriticality>(), Tags = new Dictionary<string, string> { ["site"] = "lon" } };
		var tagsOnly = new UpdatePhysicalAssetRequest { Tags = new Dictionary<string, string> { ["site"] = "lon" } };

		JsonConvert.SerializeObject(clear, settings).Should().Be("{\"criticality\":null,\"tags\":{\"site\":\"lon\"}}");
		JsonConvert.SerializeObject(tagsOnly, settings).Should().Be("{\"tags\":{\"site\":\"lon\"}}");
		JsonConvert.SerializeObject(new UpdatePhysicalAssetRequest(), settings).Should().Be("{}");
	}

	[Fact]
	public void ReadOnlyProperties_AreNeverWrittenBack()
	{
		var asset = new PhysicalAsset { Id = "a1", Serial = "S1", Tags = new Dictionary<string, string> { ["k"] = "v" } };
		var criticality = new AssetCriticality { Role = "computed", Importance = AssetImportance.Critical, OverriddenRole = "core" };

		JsonConvert.SerializeObject(asset, RequestSettings()).Should().Be("{\"tags\":{\"k\":\"v\"}}");
		JsonConvert.SerializeObject(criticality, RequestSettings()).Should().Be("{\"overriddenRole\":\"core\"}");
	}

	[Fact]
	public void ReadOnlyProperties_StillDeserialise()
		=> JsonConvert.DeserializeObject<PhysicalAsset>("{\"id\":\"a1\",\"serial\":\"S1\"}", RequestSettings())!.Id.Should().Be("a1");

	[Fact]
	public void OptionalConverter_IsWriteOnly()
	{
		var converter = new CiscoIqV1OptionalConverter();
		converter.CanRead.Should().BeFalse();
		converter.CanConvert(typeof(CiscoIqV1Optional<string>)).Should().BeTrue();
		converter.CanConvert(typeof(string)).Should().BeFalse();
		converter.CanConvert(typeof(List<string>)).Should().BeFalse();
		System.Action act = () => converter.ReadJson(null!, typeof(CiscoIqV1Optional<string>), null, null!);
		act.Should().Throw<NotSupportedException>();
	}
	[Fact]
	public async Task Patch_SendsMergePatchWithConditionalHeaders()
	{
		var seen = new Recorded();
		using var client = Recording(seen, () => V1Support.Json("{\"id\":\"a1\"}").WithHeader("ETag", "\"v2\""));

		using var response = await client.Assets.UpdateAssetPhysicalAssetAsync(
			"a1",
			new UpdatePhysicalAssetRequest { Criticality = CiscoIqV1Optional.Null<AssetCriticality>() },
			ifMatch: "\"v1\"");

		AssertSent(seen, HttpMethod.Patch, "application/merge-patch+json", "{\"criticality\":null}");
		seen.Request!.RequestUri!.AbsolutePath.Should().Be("/api/asset/v1/physicalAssets/a1");
		seen.Request.Headers.GetValues("If-Match").Should().Equal("\"v1\"");
		response.Content!.Id.Should().Be("a1");
		response.Headers!.GetValues("ETag").Should().Equal("\"v2\"");
	}

	[Fact]
	public async Task Put_ReplacesWithAJsonBody()
	{
		var seen = new Recorded();
		using var client = Recording(seen, () => V1Support.Json("{\"id\":\"a1\"}"));

		using var response = await client.Assets.ReplaceAssetPhysicalAssetAsync("a1", new ReplacePhysicalAssetRequest { Tags = new Dictionary<string, string> { ["a"] = "b" } });

		AssertSent(seen, HttpMethod.Put, "application/json", "{\"tags\":{\"a\":\"b\"}}");
	}

	[Fact]
	public async Task Post_CreatesAndParsesTheCreatedResource()
	{
		var seen = new Recorded();
		using var client = Recording(seen, () => V1Support.Json("{\"id\":\"k1\",\"keyId\":\"kid\",\"keyType\":\"BEARER\",\"expiresAt\":\"2027-01-01T00:00:00Z\"}", HttpStatusCode.Created));

		using var response = await client.Identity.CreateIamApiKeyAsync(new CreateApiKeyRequest { DisplayName = "ci", Owner = "/iam/v1/serviceAccounts/s1" });

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		response.Content!.KeyId.Should().Be("kid");
		response.Content.ExpiresAt.Should().Be(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
		JObject.Parse(seen.Body!).Should().BeEquivalentTo(JObject.Parse("{\"displayName\":\"ci\",\"owner\":\"/iam/v1/serviceAccounts/s1\"}"));
	}

	[Fact]
	public async Task Delete_ReturnsAnEmptyResponse()
	{
		var seen = new Recorded();
		using var client = Recording(seen, () => new HttpResponseMessage(HttpStatusCode.NoContent));

		using var response = await client.Identity.DeleteIamApiKeyAsync("k1", ifMatch: "\"v1\"");

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		seen.Request!.Method.Should().Be(HttpMethod.Delete);
		seen.Request.Headers.GetValues("If-Match").Should().Equal("\"v1\"");
	}
	[Fact]
	public async Task PreconditionFailed_IsAnApiException()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"title\":\"Precondition Failed\",\"status\":412,\"errors\":[{\"code\":\"PRECONDITION_FAILED\"}]}", HttpStatusCode.PreconditionFailed));
		Func<Task> act = () => client.Assets.ReplaceAssetPhysicalAssetAsync("a1", new ReplacePhysicalAssetRequest(), ifMatch: "\"stale\"");
		(await act.Should().ThrowAsync<CiscoIqV1ApiException>()).Which.HasErrorCode("PRECONDITION_FAILED").Should().BeTrue();
	}

	[Fact]
	public async Task Query_FollowsTheCursorInTheBody()
	{
		var bodies = new List<string>();
		using var client = V1Support.ClientAsync(async (request, cancellationToken) =>
		{
			var body = await request.Content!.ReadAsStringAsync(cancellationToken);
			bodies.Add(body);
			return body.Contains("\"after\"", StringComparison.Ordinal)
				? V1Support.Json("{\"items\":[{\"n\":2}],\"after\":null}")
				: V1Support.Json("{\"items\":[{\"n\":1}],\"after\":\"c1\"}");
		});
		var query = new QueryRequest { Filter = "x == 1" };

		var rows = await client.Platform.QueryAllAsync(query).ToListAsync();

		rows.Select(row => (int)row["n"]!).Should().Equal(1, 2);
		bodies.Should().HaveCount(2);
		bodies[1].Should().Contain("\"after\":\"c1\"").And.Contain("\"filter\":\"x == 1\"");
		query.After.Should().BeNull("the caller's request is copied, not changed");
	}

	[Fact]
	public async Task EnumerateByBody_RejectsMissingDelegatesAndARepeatedCursor()
	{
		Func<Task> noFetch = () => EnumerateByBody(null, _ => [], _ => null);
		Func<Task> noItems = () => EnumerateByBody(_ => Task.FromResult<Refit.IApiResponse<JObject>>(null!), null, _ => null);
		Func<Task> noNext = () => EnumerateByBody(_ => Task.FromResult<Refit.IApiResponse<JObject>>(null!), _ => [], null);
		await noFetch.Should().ThrowAsync<ArgumentNullException>();
		await noItems.Should().ThrowAsync<ArgumentNullException>();
		await noNext.Should().ThrowAsync<ArgumentNullException>();

		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[],\"after\":\"same\"}"));
		Func<Task> repeated = () => client.Platform.QueryAllAsync(new QueryRequest()).ToListAsync().AsTask();
		await repeated.Should().ThrowAsync<InvalidDataException>();
	}

	private sealed class Recorded
	{
		internal HttpRequestMessage? Request { get; set; }

		internal string? Body { get; set; }
	}

	private static CiscoIqV1Client Recording(Recorded seen, Func<HttpResponseMessage> respond)
		=> V1Support.Capturing((request, text) =>
		{
			seen.Request = request;
			seen.Body = text;
		}, respond);

	private static void AssertSent(Recorded seen, HttpMethod method, string mediaType, string body)
	{
		seen.Request!.Method.Should().Be(method);
		seen.Request.Content!.Headers.ContentType!.MediaType.Should().Be(mediaType);
		seen.Body.Should().Be(body);
	}

	private static Task<List<JToken>> EnumerateByBody(Func<string?, Task<Refit.IApiResponse<JObject>>>? fetch, Func<JObject, IReadOnlyList<JToken>?>? items, Func<JObject, string?>? next)
		=> CiscoIqV1Paging.EnumerateByBodyAsync<JObject, JToken>(fetch!, items!, next!, default).ToListAsync().AsTask();
}
