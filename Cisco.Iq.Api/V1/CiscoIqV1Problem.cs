using Newtonsoft.Json;

namespace Cisco.Iq.Api.V1;

/// <summary>An RFC 9457 problem document returned by Cisco IQ.</summary>
public class CiscoIqV1Problem
{
	/// <summary>The problem type URI.</summary>
	[JsonProperty("type")]
	public string? Type { get; set; }

	/// <summary>A short summary of the problem.</summary>
	[JsonProperty("title")]
	public string? Title { get; set; }

	/// <summary>The HTTP status.</summary>
	[JsonProperty("status")]
	public int? Status { get; set; }

	/// <summary>An explanation specific to this occurrence.</summary>
	[JsonProperty("detail")]
	public string? Detail { get; set; }

	/// <summary>The request path that produced the problem.</summary>
	[JsonProperty("instance")]
	public string? Instance { get; set; }

	/// <summary>The individual errors. Branch on their codes, not on the status.</summary>
	[JsonProperty("errors")]
	public List<CiscoIqV1ProblemError> Errors { get; set; } = [];
}

/// <summary>One error within a problem document. The set of codes is open.</summary>
public class CiscoIqV1ProblemError
{
	/// <summary>The machine-readable code, such as VALIDATION_FAILED.</summary>
	[JsonProperty("code")]
	public string? Code { get; set; }

	/// <summary>The error message.</summary>
	[JsonProperty("message")]
	public string? Message { get; set; }

	/// <summary>A longer description.</summary>
	[JsonProperty("description")]
	public string? Description { get; set; }

	/// <summary>The dotted path of the offending field, or null.</summary>
	[JsonProperty("field")]
	public string? Field { get; set; }
}
