using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api.Internal;

internal sealed partial class PageReader(HttpClient http)
{
	internal static async Task<IResponse<T>> ReadAsync<T>(Task<ApiResponse<T>> task)
	{
		using var response = await task.ConfigureAwait(false);
		if (!response.IsSuccessful)
		{
			throw response.Error!;
		}
		var content = response.Content ?? throw new JsonSerializationException("Cisco IQ returned an empty response.");
		return new CiscoIqResponse<T>(content, response.StatusCode!.Value);
	}

	internal async IAsyncEnumerable<T> EnumerateAsync<T>(Func<Task<ApiResponse<CiscoIqPage<T>>>> firstPage, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var (page, next) = await ReadFirstAsync(firstPage()).ConfigureAwait(false);
		while (true)
		{
			foreach (var item in page.Items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return item;
			}
			if (next is null)
			{
				yield break;
			}
			ValidateOrigin(next);
			(page, next) = await ReadLinkedAsync<T>(next, cancellationToken).ConfigureAwait(false);
		}
	}

	private static async Task<(CiscoIqPage<T> Page, Uri? Next)> ReadFirstAsync<T>(Task<ApiResponse<CiscoIqPage<T>>> task)
	{
		using var response = await task.ConfigureAwait(false);
		if (!response.IsSuccessful)
		{
			throw response.Error!;
		}
		var page = response.Content ?? throw new JsonSerializationException("Cisco IQ returned an empty collection response.");
		return (page, Next(response.Headers, response.RequestMessage.RequestUri!));
	}

	private async Task<(CiscoIqPage<T> Page, Uri? Next)> ReadLinkedAsync<T>(Uri next, CancellationToken cancellationToken)
	{
		using var response = await http.GetAsync(next, cancellationToken).ConfigureAwait(false);
		var page = JsonConvert.DeserializeObject<CiscoIqPage<T>>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false))
			?? throw new JsonSerializationException("Cisco IQ returned an empty collection response.");
		return (page, Next(response.Headers, response.RequestMessage!.RequestUri!));
	}

	private void ValidateOrigin(Uri next)
	{
		if (next.Scheme != http.BaseAddress!.Scheme || next.Host != http.BaseAddress.Host || next.Port != http.BaseAddress.Port || next.UserInfo.Length != 0)
		{
			throw new InvalidDataException("Cisco IQ supplied a next Link outside the API origin.");
		}
	}

	internal static Uri? Next(System.Net.Http.Headers.HttpResponseHeaders headers, Uri requestUri)
	{
		if (headers.TryGetValues("Link", out var values))
		{
			foreach (var value in values)
			{
				foreach (Match link in LinkPattern().Matches(value))
				{
					foreach (Match parameter in ParameterPattern().Matches(link.Groups["parameters"].Value))
					{
						if (parameter.Groups["name"].Value.Equals("rel", StringComparison.OrdinalIgnoreCase)
							&& parameter.Groups["value"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("next", StringComparer.OrdinalIgnoreCase))
						{
							return new Uri(requestUri, link.Groups["url"].Value);
						}
					}
				}
			}
		}
		return null;
	}

	[GeneratedRegex("""<(?<url>[^>]+)>(?<parameters>(?:[^<"]|"(?:\\.|[^"\\])*")*)(?=<|$)""")]
	private static partial Regex LinkPattern();
	[GeneratedRegex(""";\s*(?<name>[^=;,\s]+)\s*=\s*(?:"(?<value>(?:\\.|[^"\\])*)"|(?<value>[^;,\s]+))""")]
	private static partial Regex ParameterPattern();
}
