using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ FieldNotice response.</summary>
public class FieldNotice
{
	/// <summary>The fieldNoticeId value, when included in the response.</summary>
	[JsonProperty("fieldNoticeId")]
	public int? FieldNoticeId { get; set; }

	/// <summary>The title value, when included in the response.</summary>
	[JsonProperty("title")]
	public string? Title { get; set; }

	/// <summary>The impact value, when included in the response.</summary>
	[JsonProperty("impact")]
	public string? Impact { get; set; }

	/// <summary>The firstPublished value, when included in the response.</summary>
	[JsonProperty("firstPublished")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? FirstPublished { get; set; }

	/// <summary>The fieldNoticeLastUpdateDate value, when included in the response.</summary>
	[JsonProperty("fieldNoticeLastUpdateDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? FieldNoticeLastUpdateDate { get; set; }

	/// <summary>The potentiallyAffectedAssetsCount value, when included in the response.</summary>
	[JsonProperty("potentiallyAffectedAssetsCount")]
	public int? PotentiallyAffectedAssetsCount { get; set; }

	/// <summary>The url value, when included in the response.</summary>
	[JsonProperty("url")]
	public string? Url { get; set; }

	/// <summary>The status value, when included in the response.</summary>
	[JsonProperty("status")]
	public string? Status { get; set; }

	/// <summary>The problemDescription value, when included in the response.</summary>
	[JsonProperty("problemDescription")]
	public string? ProblemDescription { get; set; }

	/// <summary>The description value, when included in the response.</summary>
	[JsonProperty("description")]
	public string? Description { get; set; }

	/// <summary>The additionalNotes value, when included in the response.</summary>
	[JsonProperty("additionalNotes")]
	public string? AdditionalNotes { get; set; }

	/// <summary>The workaround value, when included in the response.</summary>
	[JsonProperty("workaround")]
	public string? Workaround { get; set; }

	/// <summary>The affectedAssetsCount value, when included in the response.</summary>
	[JsonProperty("affectedAssetsCount")]
	public int? AffectedAssetsCount { get; set; }

	/// <summary>The lastPublished value, when included in the response.</summary>
	[JsonProperty("lastPublished")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? LastPublished { get; set; }

	/// <summary>The ciscoBugIds value, when included in the response.</summary>
	[JsonProperty("ciscoBugIds")]
	public List<string>? CiscoBugIds { get; set; }

	/// <summary>The vulnerabilityStatus value, when included in the response.</summary>
	[JsonProperty("vulnerabilityStatus")]
	public string? VulnerabilityStatus { get; set; }

	/// <summary>The vulnerabilityReasons value, when included in the response.</summary>
	[JsonProperty("vulnerabilityReasons")]
	public List<string>? VulnerabilityReasons { get; set; }
}
