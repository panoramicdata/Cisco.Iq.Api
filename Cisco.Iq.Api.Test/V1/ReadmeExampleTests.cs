using System.Globalization;
using System.Text;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Products;

namespace Cisco.Iq.Api.Test.V1;

public class ReadmeExampleTests
{
	[Fact]
	public async Task UsageExample_RunsAsDocumented()
	{
		var requests = new List<Uri>();
		var output = new StringBuilder();
		CancellationToken cancellationToken = default;
		using var client = V1Support.Client(request =>
		{
			requests.Add(request.RequestUri!);
			return request.RequestUri!.AbsolutePath.EndsWith("securityAdvisories", StringComparison.Ordinal)
				? V1Support.Json("{\"items\":[{\"advisoryId\":\"cisco-sa-x\",\"title\":\"A title\"}]}")
				: V1Support.Json("{\"count\":3,\"items\":[{\"serial\":\"S1\",\"model\":\"C9300\"}]}");
		});

		using var page = await client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest
		{
			Filter = "isManaged == true",
			OrderBy = "model",
			Max = 100,
			Count = true
		}, cancellationToken: cancellationToken);

		output.AppendLine(CultureInfo.InvariantCulture, $"{page.Content!.Count} managed assets");
		foreach (var asset in page.Content.Items!)
		{
			output.AppendLine(CultureInfo.InvariantCulture, $"{asset.Serial} {asset.Model}");
		}

		await foreach (var advisory in client.Products.ListProductSecurityAdvisoriesAllAsync(
			new ListProductSecurityAdvisoriesRequest { Filter = "severity == \"CRITICAL\"" }, cancellationToken))
		{
			output.AppendLine(CultureInfo.InvariantCulture, $"{advisory.AdvisoryId} {advisory.Title}");
		}

		output.ToString().Should().Contain("3 managed assets").And.Contain("S1 C9300").And.Contain("cisco-sa-x A title");
		Uri.UnescapeDataString(requests[0].Query).Should().Contain("filter=isManaged == true").And.Contain("orderby=model").And.Contain("count=true");
		Uri.UnescapeDataString(requests[1].Query).Should().Contain("filter=severity == \"CRITICAL\"");
	}
}
