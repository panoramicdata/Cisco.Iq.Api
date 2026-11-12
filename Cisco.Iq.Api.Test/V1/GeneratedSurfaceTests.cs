using System.Reflection;
using System.Text.RegularExpressions;
using Cisco.Iq.Api.Generator;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assessments;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Identity;
using Cisco.Iq.Api.V1.Platform;
using Cisco.Iq.Api.V1.Products;
using Newtonsoft.Json.Linq;
using Refit;

namespace Cisco.Iq.Api.Test.V1;

public partial class GeneratedSurfaceTests
{
	private sealed record Seen(HttpMethod Method, Uri Uri, string? Body, string? ContentType);

	[Theory]
	[InlineData(typeof(IAssetApi), 28)]
	[InlineData(typeof(IAssessmentApi), 16)]
	[InlineData(typeof(IProductApi), 25)]
	[InlineData(typeof(IIamApi), 100)]
	[InlineData(typeof(ISystemApi), 44)]
	public async Task EveryGeneratedOperation_RequestsItsDocumentedMethodAndPath(Type api, int expected)
	{
		var requests = new List<Seen>();
		using var client = Recording(requests);
		var instance = ApiInstance(client, api);
		var methods = api.GetMethods().Where(m => m.GetCustomAttribute<HttpMethodAttribute>() is not null).ToList();

		methods.Should().HaveCount(expected);
		foreach (var method in methods)
		{
			requests.Clear();
			var attribute = method.GetCustomAttribute<HttpMethodAttribute>()!;
			using var response = (IDisposable)await Await(method.Invoke(instance, OperationArguments(method, attribute.Path))!);

			var expectedPath = PlaceholderPattern().Replace(attribute.Path, match => "v-" + match.Groups[1].Value);
			var seen = requests.Should().ContainSingle(method.Name).Which;
			seen.Uri.AbsolutePath.Should().Be(expectedPath, method.Name);
			seen.Method.Should().Be(attribute.Method, method.Name);
			var hasBody = method.GetParameters().Any(p => p.GetCustomAttribute<BodyAttribute>() is not null);
			(seen.Body is not null).Should().Be(hasBody, method.Name);
			if (method.GetCustomAttribute<HeadersAttribute>() is not null)
			{
				seen.ContentType.Should().Be("application/merge-patch+json", method.Name);
			}
		}
	}

	[Theory]
	[InlineData(typeof(AssetApiExtensions), typeof(IAssetApi))]
	[InlineData(typeof(AssessmentApiExtensions), typeof(IAssessmentApi))]
	[InlineData(typeof(ProductApiExtensions), typeof(IProductApi))]
	[InlineData(typeof(IamApiExtensions), typeof(IIamApi))]
	[InlineData(typeof(SystemApiExtensions), typeof(ISystemApi))]
	public async Task EveryPagingHelper_EnumeratesItsOperation(Type extensions, Type api)
	{
		var requests = new List<Seen>();
		using var client = Recording(requests);
		var instance = ApiInstance(client, api);
		var helpers = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static).ToList();

		helpers.Should().NotBeEmpty();
		foreach (var helper in helpers)
		{
			requests.Clear();
			var arguments = helper.GetParameters().Select((p, index) => index == 0 ? instance : ArgumentFor(p, "id-1")).ToArray();
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
		var root = V1Support.RepositoryRoot();
		var result = ClientGenerator.Generate(Path.Combine(root, "docs", "ga-spec"), ApiDefinition.All);

		result.Files.Should().HaveCount(10);
		foreach (var file in result.Files)
		{
			var path = Path.Combine(root, "Cisco.Iq.Api", "V1", "Generated", file.Key);
			File.Exists(path).Should().BeTrue($"{file.Key} should be committed");
			File.ReadAllText(path).Replace("\r\n", "\n").Should().Be(file.Value, $"{file.Key} is out of date: run tools/Cisco.Iq.Api.Generator");
		}
	}

	[Fact]
	public void Specs_AreFullyGenerated()
	{
		var result = ClientGenerator.Generate(Path.Combine(V1Support.RepositoryRoot(), "docs", "ga-spec"), ApiDefinition.All);
		result.OperationCounts.Should().BeEquivalentTo(new Dictionary<string, int> { ["assessment"] = 16, ["asset"] = 28, ["iam"] = 100, ["product"] = 25, ["system"] = 44 });
		result.Report.Should().NotContain(line => line.Contains("skipped", StringComparison.Ordinal));
	}

	private static CiscoIqV1Client Recording(List<Seen> requests)
		=> V1Support.ClientAsync(async (request, cancellationToken) =>
		{
			var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
			requests.Add(new Seen(request.Method, request.RequestUri!, body, request.Content?.Headers.ContentType?.MediaType));
			return V1Support.Json("{\"items\":[],\"after\":null}");
		});

	private static object ApiInstance(CiscoIqV1Client client, Type api)
		=> api == typeof(IAssetApi) ? client.Assets
			: api == typeof(IAssessmentApi) ? client.Assessments
			: api == typeof(IProductApi) ? client.Products
			: api == typeof(IIamApi) ? client.Identity
			: client.Platform;

	private static object?[] OperationArguments(MethodInfo method, string template)
		=> method.GetParameters().Select(parameter =>
		{
			var name = parameter.GetCustomAttribute<AliasAsAttribute>()?.Name ?? parameter.Name;
			return parameter.ParameterType == typeof(string) && template.Contains("{" + name + "}", StringComparison.OrdinalIgnoreCase)
				? "v-" + name
				: ArgumentFor(parameter, null);
		}).ToArray();

	private static object? ArgumentFor(ParameterInfo parameter, string? text)
		=> NeedsInstance(parameter) ? NewInstance(parameter.ParameterType) : ScalarArgument(parameter, text);

	private static bool NeedsInstance(ParameterInfo parameter)
		=> parameter.GetCustomAttribute<BodyAttribute>() is not null || (!parameter.HasDefaultValue && parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string));

	private static object? NewInstance(Type type)
		=> type == typeof(JToken) ? new JObject() : Activator.CreateInstance(type);

	private static object? ScalarArgument(ParameterInfo parameter, string? text)
		=> parameter.ParameterType == typeof(string) && !parameter.HasDefaultValue ? text : parameter.HasDefaultValue ? parameter.DefaultValue : null;

	private static async Task<object> Await(object task)
	{
		await (Task)task;
		return task.GetType().GetProperty("Result")!.GetValue(task)!;
	}

	[GeneratedRegex(@"\{([^}]+)\}")]
	private static partial Regex PlaceholderPattern();
}
