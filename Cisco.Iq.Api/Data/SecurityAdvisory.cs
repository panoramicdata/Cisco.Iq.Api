using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>A Cisco IQ SecurityAdvisory response.</summary>
public class SecurityAdvisory
{
	/// <summary>The psirtId value, when included in the response.</summary>
	[JsonProperty("psirtId")]
	public int? PsirtId { get; set; }

	/// <summary>The advisoryId value, when included in the response.</summary>
	[JsonProperty("advisoryId")]
	public string? AdvisoryId { get; set; }

	/// <summary>The title value, when included in the response.</summary>
	[JsonProperty("title")]
	public string? Title { get; set; }

	/// <summary>The description value, when included in the response.</summary>
	[JsonProperty("description")]
	public string? Description { get; set; }

	/// <summary>The additionalNotes value, when included in the response.</summary>
	[JsonProperty("additionalNotes")]
	public string? AdditionalNotes { get; set; }

	/// <summary>The url value, when included in the response.</summary>
	[JsonProperty("url")]
	public string? Url { get; set; }

	/// <summary>The impact value, when included in the response.</summary>
	[JsonProperty("impact")]
	public string? Impact { get; set; }

	/// <summary>The cvssScore value, when included in the response.</summary>
	[JsonProperty("cvssScore")]
	public decimal? CvssScore { get; set; }

	/// <summary>The cvssTemporalScore value, when included in the response.</summary>
	[JsonProperty("cvssTemporalScore")]
	public decimal? CvssTemporalScore { get; set; }

	/// <summary>The cveIds value, when included in the response.</summary>
	[JsonProperty("cveIds")]
	public List<string>? CveIds { get; set; }

	/// <summary>The ciscoBugIds value, when included in the response.</summary>
	[JsonProperty("ciscoBugIds")]
	public List<string>? CiscoBugIds { get; set; }

	/// <summary>The alertStatusCd value, when included in the response.</summary>
	[JsonProperty("alertStatusCd")]
	public string? AlertStatusCd { get; set; }

	/// <summary>The version value, when included in the response.</summary>
	[JsonProperty("version")]
	public string? Version { get; set; }

	/// <summary>The createdAt value, when included in the response.</summary>
	[JsonProperty("createdAt")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? CreatedAt { get; set; }

	/// <summary>The firstPublished value, when included in the response.</summary>
	[JsonProperty("firstPublished")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? FirstPublished { get; set; }

	/// <summary>The lastPublished value, when included in the response.</summary>
	[JsonProperty("lastPublished")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? LastPublished { get; set; }

	/// <summary>The psirtLastUpdateDate value, when included in the response.</summary>
	[JsonProperty("psirtLastUpdateDate")]
	[JsonConverter(typeof(UnixMillisecondsConverter))]
	public DateTimeOffset? PsirtLastUpdateDate { get; set; }

	/// <summary>The affectedAssetsCount value, when included in the response.</summary>
	[JsonProperty("affectedAssetsCount")]
	public string? AffectedAssetsCount { get; set; }

	/// <summary>The potentiallyAffectedAssetsCount value, when included in the response.</summary>
	[JsonProperty("potentiallyAffectedAssetsCount")]
	public string? PotentiallyAffectedAssetsCount { get; set; }

	/// <summary>The vulnerabilityStatus value, when included in the response.</summary>
	[JsonProperty("vulnerabilityStatus")]
	public string? VulnerabilityStatus { get; set; }

	/// <summary>The vulnerabilityReasons value, when included in the response.</summary>
	[JsonProperty("vulnerabilityReasons")]
	public List<string>? VulnerabilityReasons { get; set; }
}
