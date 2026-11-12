using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
	private static void WriteProperty(StringBuilder builder, KeyValuePair<string, Node> property, string name, string type, bool readOnly, bool isPatch)
	{
		builder.Line($"\t{Summary(property.Value["description"]?.Text ?? property.Value["title"]?.Text)}");
		builder.Line($"\t[JsonProperty(\"{property.Key}\")]");
		if (readOnly)
		{
			builder.AppendLine("\t[CiscoIqV1ReadOnly]");
		}

		var propertyType = isPatch ? $"CiscoIqV1Optional<{type}>" : $"{type}?";
		builder.Line($"\tpublic {propertyType} {name} {{ get; set; }}");
		builder.AppendLine();
	}

	private static string UniqueName(HashSet<string> used, string key)
	{
		var name = key == "_type" ? "ResourceType" : Naming.Pascal(key);
		while (!used.Add(name))
		{
			name += "Value";
		}

		return name;
	}

	private static void EmitWithAfter(StringBuilder builder, string typeName)
	{
		builder.AppendLine("\t/// <summary>Returns a copy with the paging cursor replaced.</summary>");
		builder.Line($"\tpublic {typeName} WithAfter(string? after)");
		builder.AppendLine("\t{");
		builder.Line($"\t\tvar copy = ({typeName})MemberwiseClone();");
		builder.AppendLine("\t\tcopy.After = after;");
		builder.AppendLine("\t\treturn copy;");
		builder.AppendLine("\t}");
	}

	private void EmitEnum(string typeName, Node schema)
	{
		var builder = new StringBuilder();
		builder.AppendLine(Summary(schema["description"]?.Text ?? schema["title"]?.Text));
		builder.AppendLine(GeneratedCode());
		builder.Line($"[JsonConverter(typeof(CiscoIqV1StringValueConverter<{typeName}>))]");
		builder.Line($"public readonly record struct {typeName}(string Value) : ICiscoIqV1StringValue<{typeName}>");
		builder.AppendLine("{");
		var used = new HashSet<string> { typeName, "Value", "Create" };
		foreach (var value in schema["enum"]!.Items.Select(item => item.Text!))
		{
			var member = Naming.EnumMember(value);
			while (!used.Add(member))
			{
				member += "Value";
			}

			builder.Line($"\t/// <summary>The value {Escape(value)}.</summary>");
			builder.Line($"\tpublic static readonly {typeName} {member} = new(\"{value}\");");
			builder.AppendLine();
		}

		builder.Line($"\t/// <inheritdoc />");
		builder.Line($"\tpublic static {typeName} Create(string value) => new(value);");
		builder.AppendLine();
		builder.AppendLine("\t/// <inheritdoc />");
		builder.AppendLine("\tpublic override string ToString() => Value;");
		builder.AppendLine("}");
		AddType(typeName, builder.ToString());
	}
}
