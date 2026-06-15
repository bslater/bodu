// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// The path of the cache file the tests' provider and pair resolve to: a per-provider subdirectory holding one file
    /// per pair.
    /// </summary>
    private string PairFilePath => Path.Combine(_directory, Provider, "AUDUSD.toml");

    /// <summary>
    /// Verifies that a high-precision decimal rate round-trips exactly, confirming decimals are not coerced to binary
    /// floating point on disk.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateIsHighPrecision_ShouldRoundTripExactly()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        const decimal precise = 0.123456789012345678m;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), precise, now) }, Duration, now);
        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Pair, Duration, now);

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
        TomlFileExchangeRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        var text = File.ReadAllText(PairFilePath);

        StringAssert.Contains(text, "[[Entries]]", StringComparison.Ordinal);
        StringAssert.Contains(text, "Rate = \"0.5000\"", StringComparison.Ordinal);
        StringAssert.Contains(text, "Date = 2023-01-03", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the cache file is laid out under a per-provider subdirectory and named from the pair codes.
    /// </summary>
    [TestMethod]
    public void Store_WhenWritten_ShouldLayOutFileUnderProviderDirectory()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        Assert.IsTrue(File.Exists(PairFilePath));
        Assert.AreEqual(PairFilePath, cache.ResolveFilePath(Pair));
    }

    /// <summary>
    /// Verifies that a corrupt TOML file is treated as an empty cache rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileIsCorrupt_ShouldReturnEmpty()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        File.WriteAllText(PairFilePath, "this is = not [valid toml");

        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, read.Count);
    }

    /// <summary>
    /// Verifies that the persisted file is re-readable by a second cache instance pointed at the same directory and
    /// provider, confirming cross-instance (and therefore cross-process) reuse.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenReadByNewInstance_ShouldServePersistedRows()
    {
        var now = DateTimeOffset.UtcNow;
        CreateCache().Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        TomlFileExchangeRateCache reopened = new(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });
        IReadOnlyList<CachedExchangeRate> read = reopened.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(0.5m, read[0].Rate);
    }
}
