using Microsoft.Extensions.Configuration;

namespace Cisco.Iq.Api.Test;

public class IntegrationTests
{
	[Fact]
	[Trait("Category", "Integration")]
	public async Task Credentials_ExchangeAndReadOneAsset()
	{
		var configuration = new ConfigurationBuilder().AddUserSecrets<IntegrationTests>().AddEnvironmentVariables().Build();
		var token = configuration["CiscoIq:Token"];
		Assert.SkipWhen(string.IsNullOrWhiteSpace(token), "CiscoIq:Token is not configured.");
		Enum.TryParse<CiscoIqAccountRegion>(configuration["CiscoIq:AccountRegion"], true, out var region).Should().BeTrue();
		using var client = new CiscoIqClient(new CiscoIqClientOptions
		{
			Token = token!,
			AccountId = configuration["CiscoIq:AccountId"],
			AccountRegion = region
		});
		var page = await client.Assets.GetAssetsAsync(new AssetFilter { Max = 1 }, TestContext.Current.CancellationToken);
		page.Items.Should().HaveCountLessThanOrEqualTo(1);
		page.Meta.Max.Should().Be(1);
	}
}
