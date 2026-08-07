// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the real IMF report source: the component that turns a report month into a query URL, downloads the
/// tab-separated report, caches the bytes and parses them.
/// </summary>
/// <remarks>
/// The provider tests substitute a fixture-backed source, which covers parsing but bypasses request construction and
/// caching entirely - so the URL the provider actually requests was unasserted. The IMF report is selected by a
/// <c>SelectDate</c> query parameter carrying the <em>last</em> day of the month rather than the first, which is
/// exactly the kind of detail a fixture-backed test cannot catch and a wrong value would silently return the previous
/// month's rates.
/// </remarks>
[TestClass]
public class ImfReportTableSourceTests
{
    /// <summary>
    /// An in-memory report cache that records what it was asked to store.
    /// </summary>
    private sealed class RecordingCache
        : IImfReportCache
    {
        /// <summary>The stored entries, keyed by report month.</summary>
        private readonly Dictionary<ImfReportMonth, byte[]> _entries = [];

        /// <summary>
        /// Gets the number of times <see cref="Store" /> was called.
        /// </summary>
        /// <value>The store count.</value>
        public int StoreCount { get; private set; }

        /// <summary>
        /// Seeds the cache without counting it as a store.
        /// </summary>
        /// <param name="month">The report month.</param>
        /// <param name="bytes">The report bytes.</param>
        public void Seed(ImfReportMonth month, byte[] bytes) =>
            _entries[month] = bytes;

        /// <inheritdoc />
        public bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, out byte[] bytes) =>
            _entries.TryGetValue(month, out bytes!);

        /// <inheritdoc />
        public void Store(ImfReportMonth month, byte[] bytes)
        {
            StoreCount++;
            _entries[month] = bytes;
        }
    }

    /// <summary>The report month matching the embedded fixture.</summary>
    private static ImfReportMonth Month => new(2026, 4);

    /// <summary>
    /// Builds a source over a stub handler serving the sample report.
    /// </summary>
    /// <param name="handler">Receives the stub handler.</param>
    /// <param name="cache">Receives the recording cache.</param>
    /// <param name="options">The provider options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The source under test.</returns>
    private static ImfReportTableSource CreateSource(
        out StubHttpMessageHandler handler,
        out RecordingCache cache,
        ImfRateProviderOptions? options = null)
    {
        handler = new StubHttpMessageHandler(ImfFixtures.ReadBytes(ImfFixtures.Rep202604));
        cache = new RecordingCache();

        return new ImfReportTableSource(new HttpClient(handler), options ?? new ImfRateProviderOptions(), cache);
    }

    /// <summary>
    /// Verifies that a report is downloaded and parsed into a rate table.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenReportIsNotCached_ShouldDownloadAndParseIt()
    {
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out _);

        ImfRateTable table = await source.GetTableAsync(Month);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that the request carries the report path and the month's selection date, and asks for the
    /// tab-separated form. The selection date is the last day of the month, not the first.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_ShouldRequestTheReportForThatMonth()
    {
        var options = new ImfRateProviderOptions();
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out _, options);

        _ = await source.GetTableAsync(Month);

        string requested = handler.LastRequestUri!.ToString();
        Assert.Contains(options.ReportPath, requested);
        Assert.Contains("SelectDate=2026-04-30", requested);
        Assert.Contains("tsvflag=Y", requested);
    }

    /// <summary>
    /// Verifies that downloaded bytes are cached, so a later run is served without another request.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenReportIsDownloaded_ShouldCacheTheBytes()
    {
        ImfReportTableSource source = CreateSource(out _, out RecordingCache cache);

        _ = await source.GetTableAsync(Month);

        Assert.AreEqual(1, cache.StoreCount);
    }

    /// <summary>
    /// Verifies that a cached report is parsed without issuing a request at all - the cache exists to spare the
    /// IMF's servers, not merely to speed up parsing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenReportIsCached_ShouldNotIssueARequest()
    {
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache);
        cache.Seed(Month, ImfFixtures.ReadBytes(ImfFixtures.Rep202604));

        ImfRateTable table = await source.GetTableAsync(Month);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, handler.RequestCount);
        Assert.AreEqual(0, cache.StoreCount);
    }

    /// <summary>
    /// Verifies that a second request for the same month is served from the cache the first one populated.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenCalledTwice_ShouldDownloadOnce()
    {
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out _);

        _ = await source.GetTableAsync(Month);
        _ = await source.GetTableAsync(Month);

        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> month is rejected before any request is issued.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenMonthIsNull_ShouldThrowArgumentNullException()
    {
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out _);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await source.GetTableAsync(null!);
        });

        Assert.AreEqual(0, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a report month identifies itself by year and month, so two instances for the same period are
    /// the same cache key.
    /// </summary>
    [TestMethod]
    public void ImfReportMonth_ShouldCompareByYearAndMonth()
    {
        var first = new ImfReportMonth(2026, 4);
        var same = new ImfReportMonth(2026, 4);
        var other = new ImfReportMonth(2026, 5);

        Assert.AreEqual(first, same);
        Assert.AreEqual(first.GetHashCode(), same.GetHashCode());
        Assert.AreNotEqual(first, other);
        Assert.IsFalse(first.Equals("2026-04"));
    }

    /// <summary>
    /// Verifies the derived identifiers a report month exposes: its cache key, its cache file name, and the
    /// selection date the IMF query expects - the last day of the month rather than the first.
    /// </summary>
    [TestMethod]
    public void ImfReportMonth_ShouldDeriveKeyFileNameAndSelectDate()
    {
        var month = new ImfReportMonth(2026, 4);

        Assert.AreEqual("2026-04", month.Key);
        Assert.AreEqual("imf_rep_2026-04.tsv", month.FileName);
        Assert.AreEqual(new DateOnly(2026, 4, 30), month.SelectDate);
        Assert.AreEqual("2026-04", month.ToString());

        // February in a leap year, where a naive "day 30" or "day 31" would be wrong.
        Assert.AreEqual(new DateOnly(2024, 2, 29), new ImfReportMonth(2024, 2).SelectDate);
    }

    /// <summary>
    /// Verifies that the null cache never reports a hit and accepts a store without retaining it, so a provider
    /// configured without caching still works and simply re-downloads.
    /// </summary>
    [TestMethod]
    public void NullImfReportCache_ShouldNeverHit()
    {
        IImfReportCache cache = NullImfReportCache.Instance;

        cache.Store(Month, [1, 2, 3]);

        Assert.IsFalse(cache.TryGet(Month, TimeSpan.FromDays(1), out byte[]? bytes));
        Assert.IsNull(bytes);
        Assert.AreSame(NullImfReportCache.Instance, NullImfReportCache.Instance);
    }

    /// <summary>
    /// Verifies that the constructor rejects each missing dependency, so a misconfigured registration fails where it
    /// is composed rather than on the first download.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenADependencyIsNull_ShouldThrowArgumentNullException()
    {
        using var client = new HttpClient(new StubHttpMessageHandler([]));
        var options = new ImfRateProviderOptions();
        var cache = new RecordingCache();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new ImfReportTableSource(null!, options, cache));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new ImfReportTableSource(client, null!, cache));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new ImfReportTableSource(client, options, null!));
    }
}
