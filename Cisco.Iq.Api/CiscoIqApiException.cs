using System.Net;
using Cisco.Iq.Api.Data;
using Newtonsoft.Json;

namespace Cisco.Iq.Api;

/// <summary>A Cisco IQ API error, including tracking identifiers for support.</summary>
public class CiscoIqApiException : Exception
{
	/// <summary>Creates an API error.</summary>
	public CiscoIqApiException(HttpStatusCode statusCode, string? message, string? bodyTrackingId = null, string? headerTrackingId = null)
		: base(message ?? $"Cisco IQ returned HTTP {(int)statusCode}.")
	{
		StatusCode = statusCode;
		BodyTrackingId = bodyTrackingId;
		HeaderTrackingId = headerTrackingId;
	}

	/// <summary>The response status code.</summary>
	public HttpStatusCode StatusCode { get; }
	/// <summary>The body tracking identifier, falling back to the response header.</summary>
	public string? TrackingId => BodyTrackingId ?? HeaderTrackingId;
	/// <summary>The trackingId supplied in the response body.</summary>
	public string? BodyTrackingId { get; }
	/// <summary>The TrackingID supplied in the response header.</summary>
	public string? HeaderTrackingId { get; }

	internal static async Task<CiscoIqApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		ErrorBody? error = null;
		try
		{
			error = JsonConvert.DeserializeObject<ErrorBody>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
		}
		catch (JsonException)
		{
			// Gateways may return HTML. Do not include arbitrary response text in exceptions.
		}
		var header = response.Headers.TryGetValues("TrackingID", out var values) ? values.FirstOrDefault() : null;
		return response.StatusCode switch
		{
			HttpStatusCode.Unauthorized => new CiscoIqAuthenticationException(error?.Message, error?.TrackingId, header),
			HttpStatusCode.Forbidden => new CiscoIqAuthorizationException(error?.Message, error?.TrackingId, header),
			HttpStatusCode.NotFound => new CiscoIqNotFoundException(error?.Message, error?.TrackingId, header),
			HttpStatusCode.TooManyRequests => new CiscoIqRateLimitException(error?.Message, CiscoIqRateLimitStatus.Read(response)?.ResetSeconds, error?.TrackingId, header),
			_ => new CiscoIqApiException(response.StatusCode, error?.Message, error?.TrackingId, header)
		};
	}
}

/// <summary>The identity could not be authenticated.</summary>
public sealed class CiscoIqAuthenticationException(string? message, string? bodyTrackingId = null, string? headerTrackingId = null)
	: CiscoIqApiException(HttpStatusCode.Unauthorized, message, bodyTrackingId, headerTrackingId);

/// <summary>The identity does not have access to the resource.</summary>
public sealed class CiscoIqAuthorizationException(string? message, string? bodyTrackingId = null, string? headerTrackingId = null)
	: CiscoIqApiException(HttpStatusCode.Forbidden, message, bodyTrackingId, headerTrackingId);

/// <summary>The requested resource was not found.</summary>
public sealed class CiscoIqNotFoundException(string? message, string? bodyTrackingId = null, string? headerTrackingId = null)
	: CiscoIqApiException(HttpStatusCode.NotFound, message, bodyTrackingId, headerTrackingId);

/// <summary>A rate-limit window has been exhausted.</summary>
public sealed class CiscoIqRateLimitException : CiscoIqApiException
{
	/// <summary>Creates a rate-limit error.</summary>
	public CiscoIqRateLimitException(string? message, long? resetSeconds = null, string? bodyTrackingId = null, string? headerTrackingId = null)
		: base(HttpStatusCode.TooManyRequests, message, bodyTrackingId, headerTrackingId)
	{
		ResetSeconds = resetSeconds;
	}
	/// <summary>The maximum advertised reset time in seconds, or null if unknown.</summary>
	public long? ResetSeconds { get; }
}
