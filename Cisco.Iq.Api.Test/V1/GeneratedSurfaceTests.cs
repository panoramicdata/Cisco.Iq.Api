using System.Reflection;
using System.Text.RegularExpressions;
using Cisco.Iq.Api.Generator;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assessments;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Products;
using Refit;

namespace Cisco.Iq.Api.Test.V1;

public partial class GeneratedSurfaceTests
{
	private static readonly string[] DefaultApis = ["asset", "assessment", "product"];

	[Theory]
	[InlineData(typeof(IAssetApi), 24)]
	[InlineData(typeof(IAssessmentApi), 16)]
	[InlineData(typeof(IProductApi), 25)]
	public async Task EveryGeneratedOperation_RequestsItsDocumentedPath(Type api, int expected)
	{
		var requests = new List<Uri>();
		using var client = V1Support.Client(request =>
		{
			requests.Add(request.RequestUri!);
			return V1Support.Json("{\"items\":[]}");
		});
		var instance = ApiInstance(client, api);
		var methods = api.GetMethods().Where(m => m.GetCustomAttribute<GetAttribute>() is not null).ToList();

		methods.Should().HaveCount(expected);
		foreach (var method in methods)
		{
			requests.Clear();
			var template = method.GetCustomAttribute<GetAttribute>()!.Path;
			using var response = (IDisposable)await Await(method.Invoke(instance, OperationArguments(method, template))!);

			var expectedPath = PlaceholderPattern().Replace(template, match => "v-" + match.Groups[1].Value);
			requests.Should().ContainSingle(method.Name).Which.AbsolutePath.Should().Be(expectedPath, method.Name);
		}
	}

	[Theory]
	[InlineData(typeof(AssetApiExtensions), typeof(IAssetApi))]
	[InlineData(typeof(AssessmentApiExtensions), typeof(IAssessmentApi))]
	[InlineData(typeof(ProductApiExtensions), typeof(IProductApi))]
	public async Task EveryPagingHelper_EnumeratesItsOperation(Type extensions, Type api)
	{
		var requests = new List<Uri>();
		using var client = V1Support.Client(request =>
		{
			requests.Add(request.RequestUri!);
			return V1Support.Json("{\"items\":[]}");
		});
		var instance = ApiInstance(client, api);
		var helpers = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static).ToList();

		helpers.Should().NotBeEmpty();
		foreach (var helper in helpers)
		{
			requests.Clear();
			var arguments = helper.GetParameters().Select((p, index) => index == 0 ? instance : p.ParameterType == typeof(string) ? "id-1" : p.HasDefaultValue ? p.DefaultValue : null).ToArray();
			var sequence = helper.Invoke(null, arguments)!;
			var element = helper.ReturnType.GetGenericArguments()[0];
			var enumerable = typeof(IAsyncEnumerable<>).MakeGenericType(element);
			var enumerator = enumerable.GetMethod("GetAsyncEnumerator")!.Invoke(sequence, [CancellationToken.None])!;
			var more = await (ValueTask<bool>)typeof(IAsyncEnumerator<>).MakeGenericType(element).GetMethod("MoveNextAsync")!.Invoke(enumerator, null)!;
			more.Should().BeFalse(helper.Name);
			requests.Should().ContainSingle(helper.Name);
			await ((IAsyncDisposable)enumerator).DisposeAsync();
		}
	}

	[Fact]
	public void GeneratedCode_IsUpToDateWithTheSpecs()
	{
		var root = RepositoryRoot();
		var result = ClientGenerator.Generate(Path.Combine(root, "docs", "ga-spec"), ApiDefinition.All.Where(api => DefaultApis.Contains(api.Key)));

		result.Files.Should().HaveCount(6);
		foreach (var file in result.Files)
		{
			var path = Path.Combine(root, "Cisco.Iq.Api", "V1", "Generated", file.Key);
			File.Exists(path).Should().BeTrue($"{file.Key} should be committed");
			File.ReadAllText(path).Replace("\r\n", "\n").Should().Be(file.Value, $"{file.Key} is out of date: run tools/Cisco.Iq.Api.Generator");
		}
	}

	[Fact]
	public void Specs_HaveTheOperationCountsCiscoPublished()
	{
		var result = ClientGenerator.Generate(Path.Combine(RepositoryRoot(), "docs", "ga-spec"), ApiDefinition.All.Where(api => DefaultApis.Contains(api.Key)));
		result.OperationCounts.Should().BeEquivalentTo(new Dictionary<string, int> { ["assessment"] = 16, ["asset"] = 28, ["product"] = 25 });
		result.Report.Count(line => line.Contains("skipped", StringComparison.Ordinal)).Should().Be(4, "only the four asset write operations are not generated yet");
	}

	private static object ApiInstance(CiscoIqV1Client client, Type api)
		=> api == typeof(IAssetApi) ? client.Assets : api == typeof(IAssessmentApi) ? client.Assessments : client.Products;

	private static object?[] OperationArguments(MethodInfo method, string template)
		=> method.GetParameters().Select(parameter =>
		{
			var name = parameter.GetCustomAttribute<AliasAsAttribute>()?.Name ?? parameter.Name;
			return parameter.ParameterType == typeof(string) && template.Contains("{" + name + "}", StringComparison.OrdinalIgnoreCase)
				? "v-" + name
				: parameter.HasDefaultValue ? parameter.DefaultValue : null;
		}).ToArray();

	private static async Task<object> Await(object task)
	{
		await (Task)task;
		return task.GetType().GetProperty("Result")!.GetValue(task)!;
	}

	private static string RepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cisco.Iq.Api.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");
	}

	[GeneratedRegex(@"\{([^}]+)\}")]
	private static partial Regex PlaceholderPattern();
}
