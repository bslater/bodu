// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonFileRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="JsonFileRateCache" /> construction and the round-trip of cached rates through JSON files,
/// including the self-describing header and partitioned layouts.
/// </summary>
[TestClass]
public sealed partial class JsonFileRateCacheTests
{
    /// <summary>The provider identifier the cache is bound to.</summary>
    private const string Provider = "Yahoo";

    /// <summary>The currency pair used by the tests.</summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>The duration used by the tests.</summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>The isolated temporary directory for the current test.</summary>
    private string _directory = null!;

    /// <summary>The path of the cache file the tests' provider and pair resolve to under the single-file layout.</summary>
    private string PairFilePath => Path.Combine(_directory, Provider, "AUDUSD.json");

    /// <summary>
    /// Creates an isolated temporary directory for the current test.
    /// </summary>
    [TestInitialize]
    public void Initialize() =>
        _directory = Path.Combine(Path.GetTempPath(), "bodu-exchange-rates-tests", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Removes the temporary directory created for the current test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }

    /// <summary>
    /// Creates a cache bound to <see cref="Provider" /> rooted at the test's temporary directory.
    /// </summary>
    /// <returns>A new cache instance.</returns>
    private JsonFileRateCache CreateCache() =>
        new(new FileRateCacheOptions { Provider = Provider, CacheDirectory = _directory });

    /// <summary>
    /// Verifies that a stored rate round-trips through the file system preserving its date, rate, and caching instant.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Store_WhenRoundTripped_ShouldPreserveAllFields()
    {
        JsonFileRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        CachedRate stored = new(new DateOnly(2023, 1, 3), 0.5000m, cachedAt);

        cache.Store(Pair, new[] { stored }, Duration, cachedAt);
        IReadOnlyList<CachedRate> read = cache.GetRates(Pair, Duration, cachedAt);

        Assert.HasCount(1, read);
        Assert.AreEqual(stored.Date, read[0].Date);
        Assert.AreEqual(stored.Rate, read[0].Rate);
        Assert.AreEqual(stored.CachedAtUtc, read[0].CachedAtUtc);
    }

    /// <summary>
    /// Verifies that the file uses the <c>.json</c> extension and is laid out under a per-provider subdirectory.
    /// </summary>
    [TestMethod]
    public void Store_WhenWritten_ShouldLayOutJsonFileUnderProviderDirectory()
    {
        JsonFileRateCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        Assert.IsTrue(File.Exists(PairFilePath));
        Assert.AreEqual(PairFilePath, cache.ResolveFilePath(Pair));
    }

    /// <summary>
    /// Verifies that the JSON body records the bound provider and the pair's currency codes, making the file
    /// self-describing.
    /// </summary>
    [TestMethod]
    public void Store_WhenWritten_ShouldRecordProviderAndPairInBody()
    {
        JsonFileRateCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        string text = File.ReadAllText(PairFilePath);

        StringAssert.Contains(text, "\"Provider\": \"Yahoo\"", StringComparison.Ordinal);
        StringAssert.Contains(text, "\"From\": \"AUD\"", StringComparison.Ordinal);
        StringAssert.Contains(text, "\"To\": \"USD\"", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a high-precision decimal rate round-trips exactly through JSON numbers.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateIsHighPrecision_ShouldRoundTripExactly()
    {
        JsonFileRateCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        const decimal precise = 0.123456789012345678m;

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), precise, now) }, Duration, now);
        IReadOnlyList<CachedRate> read = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, read);
        Assert.AreEqual(precise, read[0].Rate);
    }

    /// <summary>
    /// Verifies that a corrupt JSON file is treated as an empty cache rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileIsCorrupt_ShouldReturnEmpty()
    {
        JsonFileRateCache cache = CreateCache();
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        File.WriteAllText(PairFilePath, "{ this is not valid json");

        IReadOnlyList<CachedRate> read = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(read);
    }

    /// <summary>
    /// Verifies that a monthly layout splits a pair across one JSON file per month, confirming the partitioning provided
    /// by the shared base applies to the JSON format too.
    /// </summary>
    [TestMethod]
    public void Store_WhenLayoutIsMonthly_ShouldWriteOneJsonFilePerMonth()
    {
        var cache = new JsonFileRateCache(new FileRateCacheOptions
        {
            Provider = Provider,
            CacheDirectory = _directory,
            Layout = RateCacheFileLayout.Monthly,
        });
        DateTimeOffset now = new(2023, 3, 1, 0, 0, 0, TimeSpan.Zero);

        cache.Store(
            Pair,
            new[]
            {
                new CachedRate(new DateOnly(2023, 1, 3), 0.50m, now),
                new CachedRate(new DateOnly(2023, 2, 6), 0.51m, now),
            },
            Duration,
            now);

        string pairDirectory = Path.Combine(_directory, Provider, "AUDUSD");
        Assert.IsTrue(File.Exists(Path.Combine(pairDirectory, "2023-01.json")));
        Assert.IsTrue(File.Exists(Path.Combine(pairDirectory, "2023-02.json")));
    }
}
