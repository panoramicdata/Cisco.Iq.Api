using Cisco.Iq.Api.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cisco.Iq.Api.Test;

public class SerializationTests
{
	[Theory]
	[InlineData("Asset")]
	[InlineData("AssetLifecycle")]
	[InlineData("AssetRelationship")]
	public void SanitizedLivePayloads_MatchTheModels(string model)
	{
		var type = typeof(Asset).Assembly.GetType("Cisco.Iq.Api.Data." + model)!;
		var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Captured", model + ".json"));
		var expected = JObject.Parse(json);
		var value = JsonConvert.DeserializeObject(json, type)!;
		JToken.DeepEquals(expected, JObject.FromObject(value)).Should().BeTrue();
	}

	[Theory]
	[InlineData("Asset")]
	[InlineData("AffectedAsset")]
	[InlineData("AssetLifecycle")]
	[InlineData("AssetRelationship")]
	[InlineData("Contract")]
	[InlineData("SecurityAdvisory")]
	[InlineData("FieldNotice")]
	[InlineData("CollectionMeta")]
	[InlineData("ErrorBody")]
	public void Models_RoundTripEveryDeclaredField_AndAllowSparseNullResponses(string model)
	{
		var type = typeof(Asset).Assembly.GetType("Cisco.Iq.Api.Data." + model)!;
		var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", model + ".json"));
		var expected = JObject.Parse(json);
		var value = JsonConvert.DeserializeObject(json, type)!;
		JToken.DeepEquals(expected, JObject.FromObject(value)).Should().BeTrue();
		foreach (var property in expected.Properties()) { property.Value = JValue.CreateNull(); }
		var nullable = JsonConvert.DeserializeObject(expected.ToString(), type)!;
		JToken.DeepEquals(expected, JObject.FromObject(nullable)).Should().BeTrue();
		var sparse = JsonConvert.DeserializeObject("{}", type)!;
		JObject.FromObject(sparse).Properties().Should().OnlyContain(p => p.Value.Type == JTokenType.Null);
	}

	[Theory]
	[InlineData(0L)]
	[InlineData(-123L)]
	[InlineData(1700000000123L)]
	public void UnixMilliseconds_RoundTripExactly(long timestamp)
	{
		var asset = JsonConvert.DeserializeObject<Asset>($"{{\"shipDate\":{timestamp}}}")!;
		asset.ShipDate.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(timestamp));
		JObject.FromObject(asset)["shipDate"]!.Value<long>().Should().Be(timestamp);
	}

	[Fact]
	public void UnixMilliseconds_RejectsIsoDateText()
	{
		Action act = () => JsonConvert.DeserializeObject<Asset>("{\"shipDate\":\"2026-09-16T00:00:00Z\"}");
		act.Should().Throw<JsonSerializationException>();
	}

	[Fact]
	public void Count_NullRemainsUnknown_AndSparseAdvisoryCountsRemainStrings()
	{
		var page = JsonConvert.DeserializeObject<CiscoIqPage<SecurityAdvisory>>("{\"items\":[{\"affectedAssetsCount\":\"unknown\"}],\"meta\":{\"count\":null}}")!;
		page.Meta.Count.Should().BeNull();
		page.Items.Should().ContainSingle().Which.AffectedAssetsCount.Should().Be("unknown");
	}
}
