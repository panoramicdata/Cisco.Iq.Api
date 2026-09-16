using Newtonsoft.Json;

namespace Cisco.Iq.Api.Test;

public class ClientOptionsTests
{
	[Theory]
	[InlineData(CiscoIqAccountRegion.Us, "US")]
	[InlineData(CiscoIqAccountRegion.Emea, "EMEA")]
	[InlineData(CiscoIqAccountRegion.Apjc, "APJC")]
	public void Region_UsesUppercaseWireValue(CiscoIqAccountRegion region, string wireValue)
	{
		JsonConvert.SerializeObject(region).Should().Be($"\"{wireValue}\"");
		JsonConvert.DeserializeObject<CiscoIqAccountRegion>($"\"{wireValue}\"").Should().Be(region);
	}

	[Fact]
	public void Validate_AcceptsServiceAccountWithoutAccountId()
	{
		var options = new CiscoIqClientOptions { Token = "test-token", AccountRegion = CiscoIqAccountRegion.Emea };
		options.Validate();
	}

	[Fact]
	public void Validate_RejectsUndefinedRegion()
	{
		var options = new CiscoIqClientOptions { Token = "test-token", AccountRegion = (CiscoIqAccountRegion)99 };
		Action act = options.Validate;
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("test-token\r\nInjected: value")]
	public void Validate_RejectsInvalidTokenWithoutDisclosingIt(string token)
	{
		var options = new CiscoIqClientOptions { Token = token, AccountRegion = CiscoIqAccountRegion.Emea };
		Action act = options.Validate;
		act.Should().Throw<ArgumentException>().Which.Message.Should().NotContain("Injected");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Validate_RejectsNonpositiveAttemptCount(int attemptCount)
	{
		var options = new CiscoIqClientOptions { Token = "test-token", AccountRegion = CiscoIqAccountRegion.Emea, MaxAttemptCount = attemptCount };
		Action act = options.Validate;
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_RejectsUserAgentHeaderInjection()
	{
		var options = new CiscoIqClientOptions
		{
			Token = "test-token",
			AccountRegion = CiscoIqAccountRegion.Emea,
			UserAgent = "Example/1.0\r\nInjected: value"
		};
		Action act = options.Validate;
		act.Should().Throw<ArgumentException>();
	}
}
