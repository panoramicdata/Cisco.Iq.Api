using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal static partial class Naming
{
	private static readonly HashSet<string> Keywords =
	[
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal",
		"default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
		"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
		"operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
		"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
		"unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
	];

	/// <summary>Converts a JSON or spec name to PascalCase, splitting on punctuation and keeping inner capitals.</summary>
	internal static string Pascal(string name)
	{
		var builder = new StringBuilder();
		foreach (var part in NonAlphanumeric().Split(name).Where(p => p.Length > 0))
		{
			builder.Append(char.ToUpperInvariant(part[0])).Append(part.AsSpan(1));
		}

		var result = builder.ToString();
		return result.Length == 0 ? "Value" : char.IsDigit(result[0]) ? "V" + result : result;
	}

	internal static string Camel(string name)
	{
		var pascal = Pascal(name);
		var camel = char.ToLowerInvariant(pascal[0]) + pascal[1..];
		return Keywords.Contains(camel) ? "@" + camel : camel;
	}

	/// <summary>Maps a schema name such as asset.PhysicalAsset to a type name, dropping the namespace prefix.</summary>
	internal static string TypeName(string schemaName)
	{
		var index = schemaName.IndexOf('.');
		return Pascal(index < 0 ? schemaName : schemaName[(index + 1)..]);
	}

	/// <summary>Maps an enum wire value such as CLOUD_NATIVE to a member name.</summary>
	internal static string EnumMember(string value) => Pascal(value.Any(char.IsLower) ? value : value.ToLowerInvariant());

	[GeneratedRegex("[^A-Za-z0-9]+")]
	private static partial Regex NonAlphanumeric();
}
