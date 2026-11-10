using Cisco.Iq.Api.Generator;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.Test.V1;

public class SpecOverrideTests
{
	private static readonly System.Text.Json.JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public void SupportTiers_AcceptsResourcesAndReferencesInItems()
	{
		var page = JsonConvert.DeserializeObject<Cisco.Iq.Api.V1.Assessments.EntitlementTierListResponse>("{\"items\":[{\"id\":\"a\",\"name\":\"Premium\"},\"/iam/v1/entitlementTiers/b\"]}")!;

		page.Items.Should().HaveCount(2);
		page.Items![0]["name"]!.ToString().Should().Be("Premium");
		page.Items[1].ToString().Should().Be("/iam/v1/entitlementTiers/b");
	}

	[Fact]
	public void EveryOverrideInTheFile_IsAppliedByTheGenerator()
	{
		var root = FindRoot();
		var overrides = System.Text.Json.JsonSerializer.Deserialize<List<SpecOverride>>(File.ReadAllText(Path.Combine(root, "docs", "ga-spec", "overrides.json")), Options)!;
		var result = ClientGenerator.Generate(Path.Combine(root, "docs", "ga-spec"), ApiDefinition.All);

		overrides.Should().NotBeEmpty();
		foreach (var item in overrides)
		{
			item.Reason.Should().NotBeNullOrWhiteSpace();
			result.Report.Should().Contain(line => line.Contains($"{item.Type}.{item.Property}: overridden", StringComparison.Ordinal), $"{item.Type}.{item.Property} should be used by the specification");
		}
	}

	private static string FindRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cisco.Iq.Api.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");
	}
}
