// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbXmlRateTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the real ECB feed source: the component that turns a feed into a URL, downloads it, caches the bytes and
/// parses them.
/// </summary>
/// <remarks>
/// The provider tests substitute a fixture-backed source, which covers parsing but bypasses request construction and
/// caching entirely - so the URL the provider actually builds, and the decision of whether to issue a request at all,
/// were unasserted. Driving the real source over a stub handler closes that: the handler records what it was asked
/// for, and the request count distinguishes a response served from cache from one that was fetched.
/// </remarks>
[TestClass]
public class EcbXmlRateTableSourceTests
{
    /// <summary>
    /// An in-memory byte cache that records what it was asked to store.
    /// </summary>
    private sealed class RecordingCache
        : IByteCache<EcbRateFeed>
    {
        /// <summary>The stored entries, keyed by feed.</summary>
        private readonly Dictionary<EcbRateFeed, byte[]> _entries = [];

        /// <summary>
        /// Gets the number of times <see cref="Store" /> was called.
        /// </summary>
        /// <value>The store count.</value>
        public int StoreCount { get; private set; }

        /// <summary>
        /// Seeds the cache with a feed's bytes without counting it as a store.
        /// </summary>
        /// <param name="feed">The feed.</param>
        /// <param name="bytes">The bytes.</param>
        public void Seed(EcbRateFeed feed, byte[] bytes) =>
            _entries[feed] = bytes;

        /// <inheritdoc />
        public bool TryGet(EcbRateFeed key, TimeSpan refreshInterval, out byte[] bytes) =>
            _entries.TryGetValue(key, out bytes!);

        /// <inheritdoc />
        public void Store(EcbRateFeed key, byte[] bytes)
        {
            StoreCount++;
            _entries[key] = bytes;
        }
    }

    /// <summary>
    /// Builds a source over a stub handler serving the sample feed.
    /// </summary>
    /// <param name="handler">Receives the stub handler.</param>
    /// <param name="cache">Receives the recording cache.</param>
    /// <param name="options">The provider options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The source under test.</returns>
    private static EcbXmlRateTableSource CreateSource(
        out StubHttpMessageHandler handler,
        out RecordingCache cache,
        EcbRateProviderOptions? options = null)
    {
        handler = new StubHttpMessageHandler(EcbFixtures.ReadBytes(EcbFixtures.Sample));
        cache = new RecordingCache();

        return new EcbXmlRateTableSource(new HttpClient(handler), options ?? new EcbRateProviderOptions(), cache);
    }

    /// <summary>
    /// Verifies that a feed is downloaded from the URL the endpoint options resolve, so a configured base URL is
    /// actually honored rather than only validated.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenFeedIsNotCached_ShouldDownloadFromTheResolvedUrl()
    {
        var options = new EcbRateProviderOptions();
        EcbXmlRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out _, options);
        EcbRateFeed feed = EcbRateFeed.Full;

        EcbRateTable table = await source.GetTableAsync(feed);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, handler.RequestCount);
        Assert.AreEqual(options.Endpoint.ResolveFeedUrl(feed), handler.LastRequestUri);
    }

    /// <summary>
    /// Verifies that downloaded bytes are stored in the cache, so a second process or a later run can be served
    /// without another request.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenFeedIsDownloaded_ShouldCacheTheBytes()
    {
        EcbXmlRateTableSource source = CreateSource(out _, out RecordingCache cache);

        _ = await source.GetTableAsync(EcbRateFeed.Full);

        Assert.AreEqual(1, cache.StoreCount);
    }

    /// <summary>
    /// Verifies that a cached feed is parsed without issuing a request at all - the point of the cache being to
    /// spare the ECB's servers, not merely to speed up parsing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenFeedIsCached_ShouldNotIssueARequest()
    {
        EcbXmlRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache);
        cache.Seed(EcbRateFeed.Full, EcbFixtures.ReadBytes(EcbFixtures.Sample));

        EcbRateTable table = await source.GetTableAsync(EcbRateFeed.Full);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, handler.RequestCount);
        Assert.AreEqual(0, cache.StoreCount);
    }

    /// <summary>
    /// Verifies that a second request for the same feed is served from the cache the first one populated, so a
    /// repeated lookup does not repeat the download.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenCalledTwice_ShouldDownloadOnce()
    {
        EcbXmlRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out _);

        _ = await source.GetTableAsync(EcbRateFeed.Full);
        _ = await source.GetTableAsync(EcbRateFeed.Full);

        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> feed is rejected before any request is issued.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenFeedIsNull_ShouldThrowArgumentNullException()
    {
        EcbXmlRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out _);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await source.GetTableAsync(null!);
        });

        Assert.AreEqual(0, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that the constructor rejects each missing dependency, so a misconfigured registration fails where it
    /// is composed rather than on the first download.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenADependencyIsNull_ShouldThrowArgumentNullException()
    {
        using var client = new HttpClient(new StubHttpMessageHandler([]));
        var options = new EcbRateProviderOptions();
        var cache = new RecordingCache();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new EcbXmlRateTableSource(null!, options, cache));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new EcbXmlRateTableSource(client, null!, cache));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new EcbXmlRateTableSource(client, options, null!));
    }
}
