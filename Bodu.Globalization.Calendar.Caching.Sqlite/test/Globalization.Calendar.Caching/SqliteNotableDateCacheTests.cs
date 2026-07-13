// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the SQLite-specific persistence and option behaviour of <see cref="SqliteNotableDateCache" />.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a year stored through one cache instance is read back through a fresh instance over the same file.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void StoreYear_WhenReadThroughFreshInstance_ShouldPersistAcrossInstances()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            var entry = new NotableDateCacheEntry("US", 2026, "v1", new[] { NotableDateCacheTestFactory.ObservedHoliday(2026) }, Now);
            using (var writer = new SqliteNotableDateCache(path))
                Assert.AreEqual(NotableDateCacheWriteStatus.Stored, writer.StoreYear(entry, TimeSpan.FromDays(30), Now));

            using var reader = new SqliteNotableDateCache(path);
            NotableDateCacheEntry? read = reader.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);

            Assert.IsNotNull(read);
            Assert.AreEqual(1, read.Occurrences.Count);
            Assert.AreEqual("independence-day", read.Occurrences[0].NotableDateId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that options with neither a database file path nor a connection string are rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNoDatabaseLocation_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            new SqliteNotableDateCacheOptions().Validate();
        });
    }
}
