// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileSystemExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="TomlFileSystemExchangeRateCache" /> construction, directory selection, and the round-trip of
/// cached rates through the file system.
/// </summary>
[TestClass]
public sealed partial class TomlFileSystemExchangeRateCacheTests
{
    /// <summary>
    /// The provider identifier used by the tests.
    /// </summary>
    private const string Provider = "Yahoo";

    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly ExchangeRatePair Pair = new("AUD", "USD");

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
    /// Creates a cache rooted at the test's temporary directory.
    /// </summary>
    /// <returns>A new cache instance.</returns>
    private TomlFileSystemExchangeRateCache CreateCache() =>
        new(new FileSystemExchangeRateCacheOptions { CacheDirectory = _directory });

    /// <summary>
    /// Verifies that a stored rate round-trips through the file system preserving its date, rate, and caching instant.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Store_WhenRoundTripped_ShouldPreserveAllFields()
    {
        TomlFileSystemExchangeRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        CachedExchangeRate stored = new(new DateOnly(2023, 1, 3), 0.5000m, cachedAt);

        cache.Store(Provider, Pair, new[] { stored }, Duration, cachedAt);
        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Provider, Pair, Duration, cachedAt);

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
    public void Directory_WhenNoDirectoryConfigured_ShouldDefaultToTempSubfolder()
    {
        TomlFileSystemExchangeRateCache cache = new(new FileSystemExchangeRateCacheOptions());

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-exchange-rates"), cache.Directory);
    }
}
