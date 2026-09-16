using System.Net;

namespace Cisco.Iq.Api;

/// <summary>A successful Cisco IQ response.</summary>
public interface IResponse<out T>
{
	/// <summary>The deserialized response payload.</summary>
	T Content { get; }
	/// <summary>The HTTP response status.</summary>
	HttpStatusCode StatusCode { get; }
}

internal sealed record CiscoIqResponse<T>(T Content, HttpStatusCode StatusCode) : IResponse<T>;
