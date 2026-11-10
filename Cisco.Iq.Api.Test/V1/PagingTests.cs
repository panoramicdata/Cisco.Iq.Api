using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api.Test.V1;

public class PagingTests
{
	private static async Task<int> Drain<T>(IAsyncEnumerable<T> source)
		=> (await source.ToListAsync()).Count;

	private static async Task<int> Drain<T>(ConfiguredCancelableAsyncEnumerable<T> source)
	{
		var count = 0;
		await foreach (var _ in source)
		{
			count++;
		}

		return count;
	}

	private static HttpResponseHeaders HeadersWith(params string[] links)
	{
		var headers = new HttpResponseMessage().Headers;
		foreach (var link in links)
		{
			headers.TryAddWithoutValidation("Link", link);
		}

		return headers;
	}

	[Fact]
	public void NextCursor_IsNullWithoutHeadersOrALinkHeader()
	{
		CiscoIqV1Paging.NextCursor(null).Should().BeNull();
		CiscoIqV1Paging.NextCursor(HeadersWith()).Should().BeNull();
	}

	[Fact]
	public void NextCursor_IgnoresLinksThatAreNotNext()
		=> CiscoIqV1Paging.NextCursor(HeadersWith("</api/x?after=a>; rel=\"prev\", </api/x?after=b>; rel=\"first\"", "</api/x>; title=\"none\"")).Should().BeNull();

	[Theory]
	[InlineData("</api/x?max=5&after=abc>; rel=\"next\"", "abc")]
	[InlineData("</api/x?after=abc>; rel=next", "abc")]
	[InlineData("</api/x?after=abc>; REL=\"NEXT\"", "abc")]
	[InlineData("</api/x?after=abc>; rel=\"last next\"", "abc")]
	[InlineData("</api/x?after=a>; rel=\"prev\", </api/x?max=5&after=b%3D%3D>; rel=\"next\"", "b==")]
	[InlineData("</api/x?after=a+b>; rel=\"next\"", "a b")]
	[InlineData("</api/x?flag&after=c>; rel=\"next\"", "c")]
	public void NextCursor_ReadsTheAfterParameterOfTheNextLink(string link, string expected)
		=> CiscoIqV1Paging.NextCursor(HeadersWith(link)).Should().Be(expected);

	[Theory]
	[InlineData("</api/x?max=5>; rel=\"next\"")]
	[InlineData("</api/x>; rel=\"next\"")]
	[InlineData("</api/x?after>; rel=\"next\"")]
	public void NextCursor_RejectsANextLinkWithoutACursor(string link)
	{
		Action act = () => CiscoIqV1Paging.NextCursor(HeadersWith(link));
		act.Should().Throw<InvalidDataException>();
	}

	[Fact]
	public async Task Enumerate_FollowsTheCursorAcrossPagesAndDropsCount()
	{
		var requests = new List<Uri>();
		using var client = V1Support.Client(request =>
		{
			requests.Add(request.RequestUri!);
			return request.RequestUri!.Query.Contains("after=", StringComparison.Ordinal)
				? V1Support.Json("{\"items\":[{\"id\":\"c\"}]}")
				: V1Support.Json("{\"items\":[{\"id\":\"a\"},{\"id\":\"b\"}]}").WithHeader("Link", "</api/asset/v1/physicalAssets?max=2&after=abc%3D>; rel=\"next\"");
		});

		var ids = new List<string?>();
		await foreach (var asset in client.Assets.ListAssetPhysicalAssetsAllAsync(new ListAssetPhysicalAssetsRequest { Max = 2, Count = true }))
		{
			ids.Add(asset.Id);
		}

		ids.Should().Equal("a", "b", "c");
		requests.Should().HaveCount(2);
		requests.Should().OnlyContain(uri => !uri.Query.Contains("count", StringComparison.Ordinal));
		requests[0].Query.Should().NotContain("after");
		requests[1].Query.Should().Contain("after=abc%3D");
	}

	[Fact]
	public async Task Enumerate_LinksTwoDifferentTokensAndCompletesOrFails()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[{\"id\":\"a\"}]}"));
		using var sequenceSource = new CancellationTokenSource();
		using var enumeratorSource = new CancellationTokenSource();
		var items = new List<Cisco.Iq.Api.V1.Assets.PhysicalAsset>();
		await foreach (var asset in client.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: sequenceSource.Token).WithCancellation(enumeratorSource.Token))
		{
			items.Add(asset);
		}

		items.Should().ContainSingle();

		using var failing = V1Support.Client(_ => V1Support.Json("null"));
		Func<Task> act = async () =>
		{
			await Drain(failing.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: sequenceSource.Token).WithCancellation(enumeratorSource.Token));
		};
		await act.Should().ThrowAsync<JsonSerializationException>();

		await enumeratorSource.CancelAsync();
		Func<Task> cancelled = async () =>
		{
			await Drain(client.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: sequenceSource.Token).WithCancellation(enumeratorSource.Token));
		};
		await cancelled.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Enumerate_CanBeDisposedBeforeItStartsOrAfterItFinishes()
	{
		var requests = 0;
		using var client = V1Support.Client(_ =>
		{
			requests++;
			return V1Support.Json("{\"items\":[]}");
		});

		await using (var unstarted = client.Assets.ListAssetPhysicalAssetsAllAsync().GetAsyncEnumerator())
		{
			requests.Should().Be(0);
		}

		await using (var finished = client.Assets.ListAssetPhysicalAssetsAllAsync().GetAsyncEnumerator())
		{
			(await finished.MoveNextAsync()).Should().BeFalse();
		}

		requests.Should().Be(1);
	}

	[Fact]
	public async Task Enumerate_AcceptsATokenOnTheSequenceTheEnumeratorOrBoth()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[{\"id\":\"a\"}]}"));
		using var source = new CancellationTokenSource();

		(await client.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: source.Token).ToListAsync()).Should().ContainSingle();
		var enumeratorOnly = new List<Cisco.Iq.Api.V1.Assets.PhysicalAsset>();
		await foreach (var asset in client.Assets.ListAssetPhysicalAssetsAllAsync().WithCancellation(source.Token))
		{
			enumeratorOnly.Add(asset);
		}

		enumeratorOnly.Should().ContainSingle();
		var both = client.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: source.Token);
		var items = new List<Cisco.Iq.Api.V1.Assets.PhysicalAsset>();
		await foreach (var asset in both.WithCancellation(source.Token))
		{
			items.Add(asset);
		}

		items.Should().ContainSingle();
	}

	[Fact]
	public async Task Enumerate_WorksWhenPagesArriveAsynchronously()
	{
		using var client = new CiscoIqV1Client(V1Support.Options(), new TestTransport(async (_, cancellationToken) =>
		{
			await Task.Delay(5, cancellationToken);
			return V1Support.Json("{\"items\":[{\"id\":\"a\"}]}");
		}), null, null);

		var items = await client.Assets.ListAssetPhysicalAssetsAllAsync().ToListAsync();

		items.Should().ContainSingle();
	}

	[Fact]
	public async Task Enumerate_CanBeEnumeratedMoreThanOnce()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[{\"id\":\"a\"}]}"));
		var sequence = client.Assets.ListAssetPhysicalAssetsAllAsync();

		var first = await sequence.ToListAsync();
		var second = await sequence.ToListAsync();

		first.Should().ContainSingle();
		second.Should().ContainSingle();
	}

	[Fact]
	public async Task Enumerate_CanBeStoppedEarlyWithoutFetchingMore()
	{
		var requests = 0;
		using var client = V1Support.Client(_ =>
		{
			requests++;
			return V1Support.Json("{\"items\":[{\"id\":\"a\"},{\"id\":\"b\"}]}").WithHeader("Link", "</api/asset/v1/physicalAssets?after=next-1>; rel=\"next\"");
		});

		await foreach (var asset in client.Assets.ListAssetPhysicalAssetsAllAsync())
		{
			asset.Id.Should().Be("a");
			break;
		}

		requests.Should().Be(1);
	}

	[Fact]
	public async Task Enumerate_PassesPathParametersToTheOperation()
	{
		HttpRequestMessage? seen = null;
		using var client = V1Support.Client(request =>
		{
			seen = request;
			return V1Support.Json("{\"items\":[]}");
		});

		await Drain(client.Assets.ListAssetCoveragesForPhysicalAssetAllAsync("asset-9"));

		seen!.RequestUri!.AbsolutePath.Should().Be("/api/asset/v1/physicalAssets/asset-9/coverages");
	}

	[Fact]
	public async Task Enumerate_ToleratesAPageWithoutItems()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{}"));
		var count = 0;
		await foreach (var _ in client.Assets.ListAssetPhysicalAssetsAllAsync())
		{
			count++;
		}

		count.Should().Be(0);
	}

	[Fact]
	public async Task Enumerate_RejectsAnEmptyBody()
	{
		using var client = V1Support.Client(_ => V1Support.Json("null"));
		Func<Task> act = async () =>
		{
			await Drain(client.Assets.ListAssetPhysicalAssetsAllAsync());
		};
		await act.Should().ThrowAsync<JsonSerializationException>();
	}

	[Fact]
	public async Task Enumerate_RejectsACursorThatRepeats()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[]}").WithHeader("Link", "</api/asset/v1/physicalAssets?after=same>; rel=\"next\""));
		Func<Task> act = async () =>
		{
			await Drain(client.Assets.ListAssetPhysicalAssetsAllAsync());
		};
		await act.Should().ThrowAsync<InvalidDataException>();
	}

	[Fact]
	public async Task Enumerate_HonoursCancellation()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"items\":[]}"));
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();
		Func<Task> act = async () =>
		{
			await Drain(client.Assets.ListAssetPhysicalAssetsAllAsync(cancellationToken: cancelled.Token));
		};
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Enumerate_RequiresItsDelegates()
	{
		Func<Task> noFetch = async () =>
		{
			await Drain(CiscoIqV1Paging.EnumerateAsync<object, object>(null!, _ => Array.Empty<object>(), default));
		};
		Func<Task> noItems = async () =>
		{
			await Drain(CiscoIqV1Paging.EnumerateAsync<object, object>(_ => Task.FromResult<IApiResponse<object>>(null!), null!, default));
		};
		await noFetch.Should().ThrowAsync<ArgumentNullException>();
		await noItems.Should().ThrowAsync<ArgumentNullException>();
	}
}
