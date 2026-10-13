// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaXlsRateTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaXlsRateTableSource" /> downloads, parses, and caches era files.
/// </summary>
[TestClass]
public class RbaXlsRateTableSourceTests
{
    private static readonly RbaEraWorkbook s_immutableEra = new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));

    /// <summary>
    /// Verifies that the source downloads an era file and parses it into a table.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_ShouldDownloadAndParse()
    {
        RbaRateProviderOptions options = new();
        StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(s_immutableEra.FileName));
        using HttpClient client = new(handler);
        RbaXlsRateTableSource source = new(client, options, NullByteCache<RbaEraWorkbook>.Instance);

        RbaRateTable table = await source.GetTableAsync(s_immutableEra);

        Assert.Contains(s => s.CurrencyCode == "USD", table.Series);
        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a cached immutable era is served from disk on the second request without re-downloading.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_WhenImmutableEraCached_ShouldNotRefetch()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-rba-src-" + Guid.NewGuid().ToString("N"));
        try
        {
            RbaRateProviderOptions options = new();
            StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(s_immutableEra.FileName));
            using HttpClient client = new(handler);
            RbaXlsRateTableSource source = new(client, options, new FileSystemRbaWorkbookCache(directory));

            _ = await source.GetTableAsync(s_immutableEra);
            _ = await source.GetTableAsync(s_immutableEra);

            Assert.AreEqual(1, handler.RequestCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that an era not cached is downloaded from its file under the base address, cached once, and looked up
    /// with the current-era refresh interval.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenNotCached_ShouldDownloadTheEraFileAndCacheIt()
    {
        RbaRateProviderOptions options = new() { CurrentEraRefreshInterval = TimeSpan.FromHours(5) };
        StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(s_immutableEra.FileName));
        using HttpClient client = new(handler);
        RecordingCache cache = new();
        RbaXlsRateTableSource source = new(client, options, cache);

        _ = await source.GetTableAsync(s_immutableEra);

        Assert.AreEqual(new Uri(options.BaseUrl, s_immutableEra.FileName), handler.LastRequestUri);
        Assert.AreEqual(1, cache.StoreCount);
        Assert.AreEqual(TimeSpan.FromHours(5), cache.LastRefreshInterval);
    }

    /// <summary>
    /// Verifies that a failed download propagates and leaves nothing in the cache.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenDownloadFails_ShouldCacheNothing()
    {
        using HttpClient client = new(new StubHttpMessageHandler([], System.Net.HttpStatusCode.ServiceUnavailable));
        RecordingCache cache = new();
        RbaXlsRateTableSource source = new(client, new RbaRateProviderOptions(), cache);

        _ = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            _ = await source.GetTableAsync(s_immutableEra);
        });

        Assert.AreEqual(0, cache.StoreCount);
    }

    /// <summary>
    /// An in-memory workbook cache that records how it is used.
    /// </summary>
    private sealed class RecordingCache
        : IByteCache<RbaEraWorkbook>
    {
        /// <summary>The stored entries, keyed by era label.</summary>
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the number of stores.
        /// </summary>
        /// <value>The store count.</value>
        public int StoreCount { get; private set; }

        /// <summary>
        /// Gets the refresh interval of the last lookup.
        /// </summary>
        /// <value>The interval, or <see langword="null" /> before any lookup.</value>
        public TimeSpan? LastRefreshInterval { get; private set; }

        /// <inheritdoc />
        public bool TryGet(RbaEraWorkbook key, TimeSpan refreshInterval, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out byte[] bytes)
        {
            LastRefreshInterval = refreshInterval;
            return _entries.TryGetValue(key.Label, out bytes);
        }

        /// <inheritdoc />
        public void Store(RbaEraWorkbook key, byte[] bytes)
        {
            StoreCount++;
            _entries[key.Label] = bytes;
        }
    }
}
