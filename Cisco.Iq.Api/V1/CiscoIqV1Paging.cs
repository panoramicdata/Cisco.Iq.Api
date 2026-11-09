using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api.V1;

/// <summary>Follows the opaque cursor in the Link header across the pages of a collection.</summary>
public static partial class CiscoIqV1Paging
{
	/// <summary>Enumerates every item, requesting each page with the cursor from the previous page's next link.</summary>
	/// <param name="fetch">Fetches one page for a cursor, or the first page when the cursor is null.</param>
	/// <param name="items">Selects the items from a page.</param>
	/// <param name="cancellationToken">Cancels the enumeration.</param>
	public static async IAsyncEnumerable<TItem> EnumerateAsync<TPage, TItem>(
		Func<string?, Task<IApiResponse<TPage>>> fetch,
		Func<TPage, IReadOnlyList<TItem>?> items,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(fetch);
		ArgumentNullException.ThrowIfNull(items);
		string? cursor = null;
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var (pageItems, next) = await FetchPageAsync(fetch, items, cursor).ConfigureAwait(false);
			for (var index = 0; index < pageItems.Count; index++)
			{
				yield return pageItems[index];
			}

			if (next is null)
			{
				yield break;
			}

			if (string.Equals(next, cursor, StringComparison.Ordinal))
			{
				throw new InvalidDataException("Cisco IQ returned the same paging cursor twice.");
			}

			cursor = next;
		}
	}

	private static async Task<(IReadOnlyList<TItem> Items, string? Next)> FetchPageAsync<TPage, TItem>(
		Func<string?, Task<IApiResponse<TPage>>> fetch,
		Func<TPage, IReadOnlyList<TItem>?> items,
		string? cursor)
	{
		var response = await fetch(cursor).ConfigureAwait(false);
		try
		{
			var page = response.Content ?? throw new JsonSerializationException("Cisco IQ returned an empty collection response.");
			return (items(page) ?? [], NextCursor(response.Headers));
		}
		finally
		{
			response.Dispose();
		}
	}

	/// <summary>Reads the cursor from a rel="next" link, or returns null when there is no next page.</summary>
	public static string? NextCursor(HttpResponseHeaders? headers)
	{
		if (headers is null || !headers.TryGetValues("Link", out var values))
		{
			return null;
		}

		foreach (var link in values.SelectMany(value => LinkPattern().Matches(value).Cast<Match>()))
		{
			if (!RelNextPattern().IsMatch(link.Groups["parameters"].Value))
			{
				continue;
			}

			var query = link.Groups["url"].Value.Split('?', 2) is { Length: 2 } parts ? parts[1] : string.Empty;
			var cursor = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
				.Select(pair => pair.Split('=', 2))
				.FirstOrDefault(pair => pair[0] == "after" && pair.Length == 2);
			return cursor is null ? throw new InvalidDataException("Cisco IQ supplied a next link without an after cursor.") : Uri.UnescapeDataString(cursor[1].Replace('+', ' '));
		}

		return null;
	}

	[GeneratedRegex("<(?<url>[^>]*)>(?<parameters>[^,<]*)")]
	private static partial Regex LinkPattern();

	[GeneratedRegex("""rel\s*=\s*"?([^";]*\s)?next(\s[^";]*)?"?""", RegexOptions.IgnoreCase)]
	private static partial Regex RelNextPattern();
}
