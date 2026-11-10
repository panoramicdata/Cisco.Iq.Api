using System.Net;
using Cisco.Iq.Api.V1;

namespace Cisco.Iq.Api.Test.V1;

internal static class V1Support
{
	internal const string ApiKey = "test-api-key-0123456789";

	internal static CiscoIqV1ClientOptions Options(Action<CiscoIqV1ClientOptions>? change = null)
	{
		var options = new CiscoIqV1ClientOptions { ApiKey = ApiKey, Region = CiscoIqV1Region.Emea };
		change?.Invoke(options);
		return options;
	}

	internal static CiscoIqV1Client Client(
		Func<HttpRequestMessage, HttpResponseMessage> respond,
		Action<CiscoIqV1ClientOptions>? change = null,
		List<TimeSpan>? delays = null)
		=> ClientAsync((request, _) => Task.FromResult(respond(request)), change, delays);

	internal static CiscoIqV1Client ClientAsync(
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
		Action<CiscoIqV1ClientOptions>? change = null,
		List<TimeSpan>? delays = null)
		=> new(
			Options(change),
			new TestTransport(respond),
			(delay, _) =>
			{
				delays?.Add(delay);
				return Task.CompletedTask;
			},
			null);

	internal static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
		=> TestTransport.Json(body, status);

	internal static HttpResponseMessage WithHeader(this HttpResponseMessage response, string name, string value)
	{
		response.Headers.TryAddWithoutValidation(name, value);
		return response;
	}
}
