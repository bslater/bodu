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

    /// <summary>
    /// Verifies that a recorded coverage window is written as a <c>[[Coverage]]</c> array of tables with native RFC 3339
    /// dates.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenWritten_ShouldProduceExpectedTomlShape()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        var fetchedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6), Duration, fetchedAt);

        var text = File.ReadAllText(PairFilePath);

        StringAssert.Contains(text, "[[Coverage]]", StringComparison.Ordinal);
        StringAssert.Contains(text, "Start = 2023-01-03", StringComparison.Ordinal);
        StringAssert.Contains(text, "End = 2023-01-06", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that rows and coverage round-trip together through a fresh cache instance, confirming a single file
    /// carries both halves of the per-pair state.
    /// </summary>
    [TestMethod]
    public void Store_AndRecordCoverage_WhenReadByNewInstance_ShouldRoundTripBoth()
    {
        var now = DateTimeOffset.UtcNow;
        TomlFileExchangeRateCache cache = CreateCache();
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6), Duration, now);

        TomlFileExchangeRateCache reopened = new(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });

        Assert.AreEqual(1, reopened.GetRates(Pair, Duration, now).Count);
        Assert.IsTrue(reopened.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6)));
    }

    /// <summary>
    /// Verifies that a legacy file written before coverage was tracked — one with only an <c>[[Entries]]</c> array and
    /// no <c>[[Coverage]]</c> section — still deserializes to its rows with empty coverage and no error.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileHasNoCoverageSection_ShouldReadEntriesWithEmptyCoverage()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        var now = DateTimeOffset.UtcNow;

        // A hand-written pre-coverage file: entries only, no [[Coverage]] array of tables.
        File.WriteAllText(
            PairFilePath,
            "[[Entries]]\nDate = 2023-01-03\nRate = \"0.5000\"\nCachedAtUtc = "
            + now.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture) + "\n");

        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(0.5000m, read[0].Rate);
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).IsEmpty);
    }

    /// <summary>
    /// Verifies that a legacy file written before the upstream fetch instant was tracked — one whose <c>[[Entries]]</c>
    /// table has no <c>ObservedAtUtc</c> key — still deserializes its rows with a <see langword="null" />
    /// <see cref="CachedExchangeRate.ObservedAtUtc" /> and no error, mirroring the missing-<c>[[Coverage]]</c> behaviour.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileHasNoObservedAtUtcKey_ShouldReadEntriesWithNullObservedAtUtc()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        var now = DateTimeOffset.UtcNow;

        // A hand-written pre-C file: an entry with Date, Rate, and CachedAtUtc but no ObservedAtUtc key.
        File.WriteAllText(
            PairFilePath,
            "[[Entries]]\nDate = 2023-01-03\nRate = \"0.5000\"\nCachedAtUtc = "
            + now.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture) + "\n");

        IReadOnlyList<CachedExchangeRate> read = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(0.5000m, read[0].Rate);
        Assert.IsNull(read[0].ObservedAtUtc);
    }

    /// <summary>
    /// Verifies that the upstream fetch instant round-trips with its offset and sub-second precision intact across a
    /// reopen, confirming the TOML file carries the <c>ObservedAtUtc</c> key losslessly.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcSet_ShouldRoundTripAcrossReopen()
    {
        var observedAt = new DateTimeOffset(2023, 1, 3, 16, 0, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);
        var now = DateTimeOffset.UtcNow;
        CreateCache().Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now, observedAt) }, Duration, now);

        TomlFileExchangeRateCache reopened = new(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });
        IReadOnlyList<CachedExchangeRate> read = reopened.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, read.Count);
        Assert.AreEqual(observedAt, read[0].ObservedAtUtc);
        Assert.AreEqual(observedAt.Offset, read[0].ObservedAtUtc!.Value.Offset);
    }

    /// <summary>
    /// Verifies that a row carrying an upstream fetch instant writes an <c>ObservedAtUtc</c> key in native RFC 3339 form,
    /// while a row without one omits the key, keeping legacy-shaped files minimal.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcSetAndUnset_ShouldWriteKeyOnlyWhenPresent()
    {
        TomlFileExchangeRateCache cache = CreateCache();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        var observedAt = new DateTimeOffset(2023, 1, 3, 16, 0, 0, TimeSpan.Zero);
        cache.Store(
            Pair,
            new[]
            {
                new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt, observedAt),
                new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, cachedAt),
            },
            Duration,
            cachedAt);

        var text = File.ReadAllText(PairFilePath);

        // A zero offset serializes to TOML's RFC 3339 'Z' form, matching the CachedAtUtc rendering.
        StringAssert.Contains(text, "ObservedAtUtc = 2023-01-03T16:00:00Z", StringComparison.Ordinal);

        // The second row has no upstream fetch instant, so exactly one ObservedAtUtc key is written.
        Assert.AreEqual(1, CountOccurrences(text, "ObservedAtUtc"));
    }

    /// <summary>
    /// Counts the non-overlapping occurrences of <paramref name="value" /> in <paramref name="text" />.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="value">The substring to count.</param>
    /// <returns>The number of non-overlapping occurrences.</returns>
    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = text.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
