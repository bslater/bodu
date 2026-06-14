// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileSystemExchangeRateCacheTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class TomlFileSystemExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a high-precision decimal rate round-trips exactly, confirming decimals are not coerced to binary
    /// floating point on disk.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateIsHighPrecision_ShouldRoundTripExactly()
    {
        TomlFileSystemExchangeRateCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        const decimal precise = 0.123456789012345678m;

        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), precise, now) }, Duration, now);
        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Provider, Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(precise, read[0].Rate);
    }

    /// <summary>
    /// Verifies that the on-disk file is written as a TOML array of tables with the decimal rate quoted as a string and
    /// the dates in RFC 3339 form.
    /// </summary>
    [TestMethod]
    public void Store_WhenWritten_ShouldProduceExpectedTomlShape()
    {
        TomlFileSystemExchangeRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        var text = File.ReadAllText(Path.Combine(_directory, "Yahoo_AUDUSD.toml"));

        StringAssert.Contains(text, "[[Entries]]", StringComparison.Ordinal);
        StringAssert.Contains(text, "Rate = \"0.5000\"", StringComparison.Ordinal);
        StringAssert.Contains(text, "Date = 2023-01-03", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the cache file name is composed from the provider and pair codes.
    /// </summary>
    [TestMethod]
    public void Store_WhenWritten_ShouldNameFileFromProviderAndPair()
    {
        TomlFileSystemExchangeRateCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "Yahoo_AUDUSD.toml")));
    }

    /// <summary>
    /// Verifies that a corrupt TOML file is treated as an empty cache rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileIsCorrupt_ShouldReturnEmpty()
    {
        TomlFileSystemExchangeRateCache cache = CreateCache();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Yahoo_AUDUSD.toml"), "this is = not [valid toml");

        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Provider, Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, read.Count);
    }

    /// <summary>
    /// Verifies that the persisted file is re-readable by a second cache instance pointed at the same directory,
    /// confirming cross-instance (and therefore cross-process) reuse.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenReadByNewInstance_ShouldServePersistedRows()
    {
        var now = DateTimeOffset.UtcNow;
        CreateCache().Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        TomlFileSystemExchangeRateCache reopened = new(new FileSystemExchangeRateCacheOptions { CacheDirectory = _directory });
        IReadOnlyList<CachedExchangeRate> read = reopened.GetRates(Provider, Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(0.5m, read[0].Rate);
    }
}
