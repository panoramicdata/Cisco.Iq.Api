using System.Globalization;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.Test.V1;

public class OptionsAndConverterTests
{
	[Theory]
	[InlineData(CiscoIqV1Region.Us, "US")]
	[InlineData(CiscoIqV1Region.Emea, "EMEA")]
	[InlineData(CiscoIqV1Region.Apjc, "APJC")]
	public void Region_UsesUppercaseWireValue(CiscoIqV1Region region, string wire)
		=> region.ToWireValue().Should().Be(wire);

	[Fact]
	public void Region_RejectsUndefinedValue()
	{
		Action act = () => ((CiscoIqV1Region)99).ToWireValue();
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_AcceptsDefaults()
		=> V1Support.Options().Validate();

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("key with space")]
	[InlineData("key\r\nInjected: value")]
	public void Validate_RejectsBadKeyWithoutDisclosingIt(string key)
	{
		Action act = V1Support.Options(o => o.ApiKey = key).Validate;
		act.Should().Throw<ArgumentException>().Which.Message.Should().NotContain("Injected").And.NotContain("key with space");
	}

	[Fact]
	public void Validate_RejectsUndefinedRegion()
	{
		Action act = V1Support.Options(o => o.Region = (CiscoIqV1Region)42).Validate;
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("/relative", UriKind.Relative)]
	[InlineData("ftp://iq.cisco.com", UriKind.Absolute)]
	public void Validate_RejectsUnusableBaseAddress(string address, UriKind kind)
	{
		Action act = V1Support.Options(o => o.BaseAddress = new Uri(address, kind)).Validate;
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("http://localhost:5000")]
	[InlineData("https://us-west-2.iq.cisco.com")]
	public void Validate_AcceptsHttpAndHttpsBaseAddresses(string address)
		=> V1Support.Options(o => o.BaseAddress = new Uri(address)).Validate();

	[Fact]
	public void Validate_RejectsNonpositiveTimeout()
	{
		Action act = V1Support.Options(o => o.HttpClientTimeoutSeconds = 0).Validate;
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_RejectsNonpositiveAttemptCount()
	{
		Action act = V1Support.Options(o => o.MaxAttemptCount = 0).Validate;
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_RejectsInvalidUserAgent()
	{
		Action act = V1Support.Options(o => o.UserAgent = "bad(product").Validate;
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void EffectiveValues_UseDefaultsWhenUnset()
	{
		var options = V1Support.Options();
		options.EffectiveBaseAddress.Should().Be(new Uri("https://iq.cisco.com"));
		options.EffectiveUserAgent.Should().StartWith("Cisco.Iq.Api/");
	}

	[Fact]
	public void EffectiveValues_UseOverridesAndTreatBlankUserAgentAsUnset()
	{
		var options = V1Support.Options(o =>
		{
			o.BaseAddress = new Uri("https://eu-central-1.iq.cisco.com");
			o.UserAgent = "Contoso/1.0";
		});
		options.EffectiveBaseAddress.Host.Should().Be("eu-central-1.iq.cisco.com");
		options.EffectiveUserAgent.Should().Be("Contoso/1.0");
		V1Support.Options(o => o.UserAgent = "  ").EffectiveUserAgent.Should().StartWith("Cisco.Iq.Api/");
	}

	[Fact]
	public void StringValue_RoundTripsAndKeepsUnknownValues()
	{
		JsonConvert.SerializeObject(AssetOrigin.Cisco).Should().Be("\"CISCO\"");
		JsonConvert.DeserializeObject<AssetOrigin>("\"CISCO\"").Should().Be(AssetOrigin.Cisco);
		var unknown = JsonConvert.DeserializeObject<AssetOrigin>("\"SOMETHING_NEW\"");
		unknown.Value.Should().Be("SOMETHING_NEW");
		unknown.ToString().Should().Be("SOMETHING_NEW");
		AssetOrigin.CloudNative.Value.Should().Be("CLOUD_NATIVE");
		AssetOrigin.Create("X").Value.Should().Be("X");
	}

	[Fact]
	public void StringValue_RejectsNonStringTokens()
	{
		Action act = () => JsonConvert.DeserializeObject<AssetOrigin>("12");
		act.Should().Throw<JsonSerializationException>();
	}

	[Fact]
	public void StringValue_IsNullableOnModels()
	{
		JsonConvert.DeserializeObject<PhysicalAsset>("{\"assetOrigin\":\"CISCO\"}")!.AssetOrigin.Should().Be(AssetOrigin.Cisco);
		JsonConvert.DeserializeObject<PhysicalAsset>("{\"assetOrigin\":null}")!.AssetOrigin.Should().BeNull();
		JsonConvert.SerializeObject(new PhysicalAsset { AssetOrigin = AssetOrigin.Cisco }).Should().Contain("\"assetOrigin\":\"CISCO\"");
	}

	[Fact]
	public void StringValue_ReadsNullAsDefaultForANonNullableMember()
	{
		var holder = JsonConvert.DeserializeObject<OriginHolder>("{\"Origin\":null}")!;
		holder.Origin.Value.Should().BeNull();
	}

	[Fact]
	public void StringValue_ConvertsOnlyItsOwnTypeAndItsNullableForm()
	{
		var converter = new CiscoIqV1StringValueConverter<AssetOrigin>();
		converter.CanConvert(typeof(AssetOrigin)).Should().BeTrue();
		converter.CanConvert(typeof(AssetOrigin?)).Should().BeTrue();
		converter.CanConvert(typeof(string)).Should().BeFalse();
	}

	[Fact]
	public void DateOnly_HandlesNullForNullableAndNonNullableMembers()
	{
		var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None, Converters = { new CiscoIqV1DateOnlyConverter() } };
		var holder = JsonConvert.DeserializeObject<DateHolder>("{\"Day\":null,\"Maybe\":null}", settings)!;
		holder.Day.Should().Be(default);
		holder.Maybe.Should().BeNull();
		JsonConvert.DeserializeObject<DateHolder>("{\"Day\":\"2024-02-29\",\"Maybe\":\"2024-03-01\"}", settings)!.Maybe.Should().Be(new DateOnly(2024, 3, 1));
		var converter = new CiscoIqV1DateOnlyConverter();
		converter.CanConvert(typeof(DateOnly)).Should().BeTrue();
		converter.CanConvert(typeof(DateOnly?)).Should().BeTrue();
		converter.CanConvert(typeof(DateTime)).Should().BeFalse();
	}

	private sealed class OriginHolder
	{
		public AssetOrigin Origin { get; set; }
	}

	private sealed class DateHolder
	{
		public DateOnly Day { get; set; }

		public DateOnly? Maybe { get; set; }
	}

	[Fact]
	public void DateOnly_ReadsStringsAndDateTimeTokensAndWritesIsoDates()
	{
		var strings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None, Converters = { new CiscoIqV1DateOnlyConverter() } };
		var dates = new JsonSerializerSettings { DateParseHandling = DateParseHandling.DateTime, Converters = { new CiscoIqV1DateOnlyConverter() } };
		JsonConvert.DeserializeObject<DateOnly>("\"2024-01-15\"", strings).Should().Be(new DateOnly(2024, 1, 15));
		JsonConvert.DeserializeObject<DateOnly>("\"2024-01-15T00:00:00\"", dates).Should().Be(new DateOnly(2024, 1, 15));
		JsonConvert.SerializeObject(new DateOnly(2024, 1, 15), strings).Should().Be("\"2024-01-15\"");
	}

	[Fact]
	public void DateOnly_RejectsOtherTokens()
	{
		var settings = new JsonSerializerSettings { Converters = { new CiscoIqV1DateOnlyConverter() } };
		Action act = () => JsonConvert.DeserializeObject<DateOnly>("12", settings);
		act.Should().Throw<JsonSerializationException>();
	}

	[Fact]
	public void Problem_DeserializesErrorsWithOpenCodes()
	{
		const string Body = """
			{"type":"https://iq.cisco.com/api/problems/validation-failed","title":"Validation Failed","status":422,"detail":"Bad filter","instance":"/api/asset/v1/physicalAssets",
			 "errors":[{"code":"VALIDATION_FAILED","message":"m","description":"d","field":"filter"},{"code":"A_NEW_CODE"}]}
			""";
		var problem = JsonConvert.DeserializeObject<CiscoIqV1Problem>(Body)!;
		problem.Status.Should().Be(422);
		problem.Type.Should().EndWith("validation-failed");
		problem.Instance.Should().Be("/api/asset/v1/physicalAssets");
		problem.Errors.Should().HaveCount(2);
		problem.Errors[0].Field.Should().Be("filter");
		problem.Errors[0].Description.Should().Be("d");
		problem.Errors[0].Message.Should().Be("m");
		problem.Errors[1].Code.Should().Be("A_NEW_CODE");
	}

	[Fact]
	public void UrlFormatter_UsesWireConventions()
	{
		var formatter = new CiscoIqV1UrlParameterFormatter();
		string? Format(object? value) => formatter.Format(value, typeof(CiscoIqV1UrlParameterFormatter), value?.GetType() ?? typeof(object));
		Format(null).Should().BeNull();
		Format(true).Should().Be("true");
		Format(false).Should().Be("false");
		Format(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2))).Should().Be("2024-01-15T08:30:00Z");
		Format(new DateOnly(2024, 1, 15)).Should().Be("2024-01-15");
		Format(1234.5).Should().Be(1234.5.ToString(CultureInfo.InvariantCulture));
		Format("text").Should().Be("text");
		Format(AssetOrigin.Cisco).Should().Be("CISCO");
		Action act = () => Format(CiscoIqV1Region.Us);
		act.Should().Throw<ArgumentException>();
	}
}
