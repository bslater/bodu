// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="TomlFileExchangeRateCache" /> construction, directory selection, and the round-trip of cached
/// rates through the file system.
/// </summary>
[TestClass]
public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// The provider identifier the cache is bound to.
    /// </summary>
    private const string Provider = "Yahoo";

    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly ExchangeRatePair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// The duration used by the tests.
    /// </summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// The isolated temporary directory for the current test.
    /// </summary>
    private string _directory = null!;

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
    private TomlFileExchangeRateCache CreateCache() =>
        new(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });

    /// <summary>
    /// Verifies that a stored rate round-trips through the file system preserving its date, rate, and caching instant.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Store_WhenRoundTripped_ShouldPreserveAllFields()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        CachedExchangeRate stored = new(new DateOnly(2023, 1, 3), 0.5000m, cachedAt);

        cache.Store(Pair, new[] { stored }, Duration, cachedAt);
        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Pair, Duration, cachedAt);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(stored.Date, read[0].Date);
        Assert.AreEqual(stored.Rate, read[0].Rate);
        Assert.AreEqual(stored.CachedAtUtc, read[0].CachedAtUtc);
    }

    /// <summary>
    /// Verifies that the cache directory defaults to a <c>bodu-exchange-rates</c> folder under the temporary path when no
    /// directory is configured.
    /// </summary>
    [TestMethod]
    public void CacheDirectory_WhenNoDirectoryConfigured_ShouldDefaultToTempSubfolder()
    {
        TomlFileExchangeRateCache cache = new(new FileExchangeRateCacheOptions { Provider = Provider });

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-exchange-rates"), cache.CacheDirectory);
    }

    /// <summary>
    /// Verifies that the constructor rejects options with a blank provider.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = "  ", CacheDirectory = _directory });
        });

        Assert.AreEqual("Provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write whose underlying file write fails reports
    /// <see cref="ExchangeRateCacheWriteStatus.Failed" /> rather than falsely reporting the rows and coverage as stored.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenFileWriteFails_ShouldReturnFailed()
    {
        // Occupy the configured cache directory path with a regular file so resolving the per-pair file forces the
        // backend to create a directory beneath an existing file, which fails the write with an IOException that the
        // best-effort backend swallows.
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        TomlFileExchangeRateCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3),
            Duration,
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Failed, status);
    }
}
