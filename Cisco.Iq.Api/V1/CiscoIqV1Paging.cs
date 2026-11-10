using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api.V1;

/// <summary>Follows the opaque cursor in the Link header, or in the page body, across the pages of a collection.</summary>
public static partial class CiscoIqV1Paging
{
	/// <summary>Enumerates every item, requesting each page with the cursor from the previous page's next link.</summary>
	/// <param name="fetch">Fetches one page for a cursor, or the first page when the cursor is null.</param>
	/// <param name="items">Selects the items from a page.</param>
	/// <param name="cancellationToken">Cancels the enumeration.</param>
	public static IAsyncEnumerable<TItem> EnumerateAsync<TPage, TItem>(
		Func<string?, Task<IApiResponse<TPage>>> fetch,
		Func<TPage, IReadOnlyList<TItem>?> items,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(fetch);
		ArgumentNullException.ThrowIfNull(items);
		return EnumerateCoreAsync(fetch, items, (response, _) => NextCursor(response.Headers), cancellationToken);
	}

	/// <summary>Enumerates every item of an endpoint that pages through its bodies: the next cursor is a field of the page, and goes into the next request.</summary>
	/// <param name="fetch">Fetches one page for a cursor, or the first page when the cursor is null.</param>
	/// <param name="items">Selects the items from a page.</param>
	/// <param name="nextCursor">Selects the cursor for the next page, or null on the last page.</param>
	/// <param name="cancellationToken">Cancels the enumeration.</param>
	public static IAsyncEnumerable<TItem> EnumerateByBodyAsync<TPage, TItem>(
		Func<string?, Task<IApiResponse<TPage>>> fetch,
		Func<TPage, IReadOnlyList<TItem>?> items,
		Func<TPage, string?> nextCursor,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(fetch);
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(nextCursor);
		return EnumerateCoreAsync(fetch, items, (_, page) => nextCursor(page), cancellationToken);
	}

	private static async IAsyncEnumerable<TItem> EnumerateCoreAsync<TPage, TItem>(
		Func<string?, Task<IApiResponse<TPage>>> fetch,
		Func<TPage, IReadOnlyList<TItem>?> items,
		Func<IApiResponse<TPage>, TPage, string?> nextOf,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		string? cursor = null;
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var (pageItems, next) = await FetchPageAsync(fetch, items, nextOf, cursor).ConfigureAwait(false);
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
		Func<IApiResponse<TPage>, TPage, string?> nextOf,
		string? cursor)
	{
		var response = await fetch(cursor).ConfigureAwait(false);
		try
		{
			var page = response.Content ?? throw new JsonSerializationException("Cisco IQ returned an empty collection response.");
			return (items(page) ?? [], nextOf(response, page));
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
