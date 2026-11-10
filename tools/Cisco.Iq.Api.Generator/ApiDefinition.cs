namespace Cisco.Iq.Api.Generator;

/// <summary>One of the five Cisco IQ GA APIs and where its generated code goes.</summary>
/// <param name="Key">The short key, such as asset.</param>
/// <param name="FileName">The OpenAPI file name.</param>
/// <param name="Namespace">The C# namespace segment under Cisco.Iq.Api.V1.</param>
/// <param name="InterfaceName">The Refit interface name.</param>
public sealed record ApiDefinition(string Key, string FileName, string Namespace, string InterfaceName)
{
	/// <summary>All five GA APIs.</summary>
	public static IReadOnlyList<ApiDefinition> All { get; } =
	[
		new("asset", "iq-asset.openapi-en.yaml", "Assets", "IAssetApi"),
		new("assessment", "iq-assessment.openapi-en.yaml", "Assessments", "IAssessmentApi"),
		new("product", "iq-product.openapi-en.yaml", "Products", "IProductApi"),
		new("iam", "iq-iam.openapi-en.yaml", "Identity", "IIamApi"),
		new("system", "iq-system.openapi-en.yaml", "Platform", "ISystemApi")
	];
}

/// <summary>The generated sources and a human-readable report of what was generated and what was skipped.</summary>
/// <param name="Files">Generated source text keyed by file name.</param>
/// <param name="Report">One line per notable decision or omission.</param>
/// <param name="OperationCounts">The number of operations in each spec, counted by method and path.</param>
public sealed record GenerationResult(IReadOnlyDictionary<string, string> Files, IReadOnlyList<string> Report, IReadOnlyDictionary<string, int> OperationCounts);

/// <summary>A documented place where Cisco's specification is wrong and the live service is followed instead.</summary>
/// <param name="Api">The API key, such as assessment.</param>
/// <param name="Type">The generated type name.</param>
/// <param name="Property">The JSON property name.</param>
/// <param name="CSharp">The C# type to use instead.</param>
/// <param name="Reason">Why, with the evidence.</param>
public sealed record SpecOverride(string Api, string Type, string Property, string CSharp, string Reason);
