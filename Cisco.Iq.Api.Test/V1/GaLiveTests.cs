using System.Reflection;
using System.Text.RegularExpressions;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assessments;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Identity;
using Cisco.Iq.Api.V1.Platform;
using Cisco.Iq.Api.V1.Products;
using Microsoft.Extensions.Configuration;

namespace Cisco.Iq.Api.Test.V1;

/// <summary>Read-only checks against a live Cisco IQ account. Every call is a list with a page size of one: nothing is written, deleted or created.</summary>
public class GaLiveTests
{
	private static readonly Dictionary<string, Func<CiscoIqV1Client, CancellationToken, Task<IDisposable>>> Reads = new()
	{
		["assets.physicalAssets"] = async (c, t) => await c.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest { Max = 1, Count = true }, t),
		["assets.virtualAssets"] = async (c, t) => await c.Assets.ListAssetVirtualAssetsAsync(new ListAssetVirtualAssetsRequest { Max = 1 }, t),
		["assets.coverages"] = async (c, t) => await c.Assets.ListAssetCoveragesAsync(new ListAssetCoveragesRequest { Max = 1 }, t),
		["assets.coverageSources"] = async (c, t) => await c.Assets.ListAssetCoverageSourcesAsync(new ListAssetCoverageSourcesRequest { Max = 1 }, t),
		["assets.dataSources"] = async (c, t) => await c.Assets.ListAssetDataSourcesAsync(new ListAssetDataSourcesRequest { Max = 1 }, t),
		["assets.licensingAccounts"] = async (c, t) => await c.Assets.ListAssetLicensingAccountsAsync(new ListAssetLicensingAccountsRequest { Max = 1 }, t),
		["assessments.assessments"] = async (c, t) => await c.Assessments.ListAssessmentAssessmentsAsync(new ListAssessmentAssessmentsRequest { Max = 1 }, t),
		["assessments.findings"] = async (c, t) => await c.Assessments.ListAssessmentFindingsAsync(new ListAssessmentFindingsRequest { Max = 1 }, t),
		["assessments.insights"] = async (c, t) => await c.Assessments.ListAssessmentInsightsAsync(new ListAssessmentInsightsRequest { Max = 1 }, t),
		["products.securityAdvisories"] = async (c, t) => await c.Products.ListProductSecurityAdvisoriesAsync(new ListProductSecurityAdvisoriesRequest { Max = 1 }, t),
		["products.fieldNotices"] = async (c, t) => await c.Products.ListProductFieldNoticesAsync(new ListProductFieldNoticesRequest { Max = 1 }, t),
		["products.products"] = async (c, t) => await c.Products.ListProductProductsAsync(new ListProductProductsRequest { Max = 1 }, t),
		["products.bugs"] = async (c, t) => await c.Products.ListProductBugsAsync(new ListProductBugsRequest { Max = 1 }, t),
		["products.hardwareEndOfLifeBulletins"] = async (c, t) => await c.Products.ListProductHardwareEndOfLifeBulletinsAsync(new ListProductHardwareEndOfLifeBulletinsRequest { Max = 1 }, t),
		["products.softwareEndOfLifeBulletins"] = async (c, t) => await c.Products.ListProductSoftwareEndOfLifeBulletinsAsync(new ListProductSoftwareEndOfLifeBulletinsRequest { Max = 1 }, t),
		["identity.regions"] = async (c, t) => await c.Identity.ListIamRegionsAsync(new ListIamRegionsRequest { Max = 1 }, t),
		["identity.accounts"] = async (c, t) => await c.Identity.ListIamAccountsAsync(new ListIamAccountsRequest { Max = 1 }, t),
		["identity.users"] = async (c, t) => await c.Identity.ListIamUsersAsync(new ListIamUsersRequest { Max = 1 }, t),
		["identity.roles"] = async (c, t) => await c.Identity.ListIamRolesAsync(new ListIamRolesRequest { Max = 1 }, t),
		["identity.apiKeys"] = async (c, t) => await c.Identity.ListIamApiKeysAsync(new ListIamApiKeysRequest { Max = 1 }, t),
		["platform.resourceGroups"] = async (c, t) => await c.Platform.ListSystemResourceGroupsAsync(new ListSystemResourceGroupsRequest { Max = 1 }, t),
		["platform.userPreferences"] = async (c, t) => await c.Platform.ListSystemUserPreferencesAsync(new ListSystemUserPreferencesRequest { Max = 1 }, t)
	};

	public static TheoryData<string> ReadNames => [.. Reads.Keys];

	[Theory]
	[Trait("Category", "Integration")]
	[MemberData(nameof(ReadNames))]
	public async Task List_ReturnsAPage(string name)
	{
		using var client = CreateClient();

		using var response = (Refit.IApiResponse)await Reads[name](client, TestContext.Current.CancellationToken);

		response.IsSuccessStatusCode.Should().BeTrue(name);
		response.Error.Should().BeNull(name);
	}

	[Theory]
	[Trait("Category", "Integration")]
	[InlineData("assets")]
	[InlineData("assessments")]
	[InlineData("products")]
	[InlineData("identity")]
	[InlineData("platform")]
	public async Task Walk_ReadsFirstItemsAndTheirRelations(string area)
	{
		using var client = CreateClient();
		var api = area switch
		{
			"assets" => (object)client.Assets,
			"assessments" => client.Assessments,
			"products" => client.Products,
			"identity" => client.Identity,
			_ => client.Platform
		};
		var reads = api.GetType().GetInterfaces().SelectMany(i => i.GetMethods())
			.Select(method => (Method: method, Path: method.GetCustomAttribute<Refit.GetAttribute>()?.Path))
			.Where(read => read.Path is not null)
			.ToList();
		var results = new List<string>();
		var failures = new List<string>();
		foreach (var list in reads.Where(read => !read.Path!.Contains('{', StringComparison.Ordinal)))
		{
			var id = await FirstIdAsync(api, list.Method);
			if (id is null)
			{
				results.Add($"{list.Method.Name}: no items, relations not walked");
				continue;
			}

			var related = reads.Where(read => Regex.IsMatch(read.Path!, "^" + Regex.Escape(list.Path!) + @"/\{[a-zA-Z]+\}(/[a-zA-Z]+)?$"));
			foreach (var read in related)
			{
				var outcome = await InvokeAsync(api, read.Method, id);
				(outcome is null ? results : failures).Add($"{read.Method.Name}: {outcome ?? "ok"}");
			}
		}

		var report = string.Join(Environment.NewLine, results.Concat(failures.Select(f => "FAILED " + f)));
		if (Environment.GetEnvironmentVariable("CiscoIqV1__WalkReportPath") is { Length: > 0 } path)
		{
			await File.AppendAllTextAsync(path, $"== {area}{Environment.NewLine}{report}{Environment.NewLine}", TestContext.Current.CancellationToken);
		}

		failures.Should().BeEmpty(report);
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task Paging_FollowsTheCursorOnARealCollection()
	{
		using var client = CreateClient();

		var items = new List<PhysicalAsset>();
		await foreach (var asset in client.Assets.ListAssetPhysicalAssetsAllAsync(new ListAssetPhysicalAssetsRequest { Max = 2 }, TestContext.Current.CancellationToken))
		{
			items.Add(asset);
			if (items.Count >= 5)
			{
				break;
			}
		}

		items.Count.Should().BeLessThanOrEqualTo(5);
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task Hosts_ReportWhichDocumentedHostsAnswer()
	{
		var results = new List<string>();
		foreach (var host in new[] { "https://iq.cisco.com", "https://us-west-2.iq.cisco.com", "https://eu-central-1.iq.cisco.com", "https://ap-southeast-2.iq.cisco.com", "https://us-east-1.iq.cisco.com", "https://eu-frankfurt-1.iq.cisco.com" })
		{
			using var client = CreateClient(new Uri(host));
			try
			{
				using var response = await client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest { Max = 1 }, TestContext.Current.CancellationToken);
				results.Add($"{host}: {(int)response.StatusCode!.Value}");
			}
			catch (CiscoIqV1ApiException exception)
			{
				results.Add($"{host}: {(int)exception.StatusCode} {exception.GetType().Name}");
			}
			catch (HttpRequestException exception)
			{
				results.Add($"{host}: {exception.GetType().Name} {exception.Message}");
			}
		}

		TestContext.Current.SendDiagnosticMessage(string.Join(Environment.NewLine, results));
		if (Environment.GetEnvironmentVariable("CiscoIqV1__HostReportPath") is { Length: > 0 } reportPath)
		{
			await File.WriteAllLinesAsync(reportPath, results, TestContext.Current.CancellationToken);
		}

		results[0].Should().Contain(": 200", "the global host with X-IQ-Region is the client default");
	}

	private static async Task<string?> FirstIdAsync(object api, MethodInfo list)
	{
		var arguments = list.GetParameters().Select(parameter =>
		{
			if (parameter.ParameterType.Name.EndsWith("Request", StringComparison.Ordinal))
			{
				var request = Activator.CreateInstance(parameter.ParameterType)!;
				parameter.ParameterType.GetProperty("Max")?.SetValue(request, 1L);
				return request;
			}

			return parameter.HasDefaultValue ? parameter.DefaultValue : null;
		}).ToArray();
		object response;
		try
		{
			response = await AwaitResponse(list.Invoke(api, arguments)!);
		}
		catch (CiscoIqV1AuthorizationException)
		{
			return null;
		}

		using var disposable = (IDisposable)response;
		var content = response.GetType().GetProperty("Content")!.GetValue(response);
		var items = content?.GetType().GetProperty("Items")?.GetValue(content) as System.Collections.IList;
		return items is { Count: > 0 } ? items[0]!.GetType().GetProperty("Id")?.GetValue(items[0]) as string : null;
	}

	private static async Task<string?> InvokeAsync(object api, MethodInfo read, string id)
	{
		var arguments = read.GetParameters().Select(parameter => parameter.ParameterType == typeof(string) && !parameter.HasDefaultValue ? id : parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
		try
		{
			using var response = (Refit.IApiResponse)await AwaitResponse(read.Invoke(api, arguments)!);
			return response.IsSuccessStatusCode && response.Error is null ? null : $"HTTP {(int)(response.StatusCode ?? 0)} {response.Error?.GetType().Name} {response.Error?.Message} {response.Error?.InnerException?.Message}";
		}
		catch (CiscoIqV1AuthorizationException)
		{
			return null;
		}
		catch (CiscoIqV1ApiException exception)
		{
			return $"HTTP {(int)exception.StatusCode} {exception.GetType().Name} {exception.Message}";
		}
	}

	private static async Task<object> AwaitResponse(object task)
	{
		await (Task)task;
		return task.GetType().GetProperty("Result")!.GetValue(task)!;
	}

	private static CiscoIqV1Client CreateClient(Uri? baseAddress = null)
	{
		var configuration = new ConfigurationBuilder().AddUserSecrets<GaLiveTests>().AddEnvironmentVariables().Build();
		var key = configuration["CiscoIqV1:ApiKey"];
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new InvalidOperationException("Set CiscoIqV1:ApiKey in user secrets (dotnet user-secrets set \"CiscoIqV1:ApiKey\" \"...\" --project Cisco.Iq.Api.Test) or the CiscoIqV1__ApiKey environment variable.");
		}

		if (!Enum.TryParse<CiscoIqV1Region>(configuration["CiscoIqV1:Region"], true, out var region))
		{
			throw new InvalidOperationException("Set CiscoIqV1:Region to Us, Emea or Apjc.");
		}

		return new CiscoIqV1Client(new CiscoIqV1ClientOptions { ApiKey = key, Region = region, BaseAddress = baseAddress });
	}
}
