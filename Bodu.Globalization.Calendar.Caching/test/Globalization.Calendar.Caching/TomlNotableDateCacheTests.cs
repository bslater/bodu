// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNotableDateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="TomlNotableDateCache" /> persists and reloads computed years through TOML files without
/// loss.
/// </summary>
[TestClass]
public sealed class TomlNotableDateCacheTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a stored year read back through a fresh cache instance over the same directory reconstructs every
    /// occurrence field.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenReadThroughFreshInstance_ShouldRoundTripAcrossFiles()
    {
        string directory = CreateTempDirectory();
        try
        {
            var options = new FileNotableDateCacheOptions { CacheDirectory = directory };
            var entry = new NotableDateCacheEntry(
                "US",
                2026,
                "v1",
                new[] { NotableDateCacheTestFactory.NewYearsDay(2026), NotableDateCacheTestFactory.ObservedHoliday(2026) },
                Now);

            var writer = new TomlNotableDateCache(options);
            Assert.AreEqual(NotableDateCacheWriteStatus.Stored, writer.StoreYear(entry, TimeSpan.FromDays(30), Now));

            var reader = new TomlNotableDateCache(options);
            NotableDateCacheEntry? read = reader.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);

            Assert.IsNotNull(read);
            Assert.AreEqual(entry.ComputedAtUtc, read.ComputedAtUtc);
            Assert.AreEqual(2, read.Occurrences.Count);
            NotableDateCacheFileConverterTests.AssertOccurrenceEqual(
                NotableDateCacheTestFactory.NewYearsDay(2026),
                read.Occurrences.Single(o => o.NotableDateId == "new-year"));
            NotableDateCacheFileConverterTests.AssertOccurrenceEqual(
                NotableDateCacheTestFactory.ObservedHoliday(2026),
                read.Occurrences.Single(o => o.NotableDateId == "independence-day"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that a stored empty year round-trips as a computed-but-empty year rather than a miss.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenYearEmpty_ShouldRoundTripAsComputedEmptyYear()
    {
        string directory = CreateTempDirectory();
        try
        {
            var options = new FileNotableDateCacheOptions { CacheDirectory = directory };
            var cache = new TomlNotableDateCache(options);
            cache.StoreYear(NotableDateCacheTestFactory.EmptyEntry("US", 2026, "v1", Now), TimeSpan.FromDays(30), Now);

            NotableDateCacheEntry? read = new TomlNotableDateCache(options).GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);

            Assert.IsNotNull(read);
            Assert.AreEqual(0, read.Occurrences.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Creates a fresh temporary directory for a test's cache files.
    /// </summary>
    /// <returns>The created directory path.</returns>
    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
