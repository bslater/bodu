// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Caching.Contracts;

/// <summary>
/// Defines the backend-agnostic behavioural contract every <see cref="INotableDateCache" /> implementation must satisfy,
/// so the in-memory, file, SQLite, and distributed backends are all exercised against one authoritative specification.
/// </summary>
/// <remarks>
/// A concrete subclass supplies a fresh cache through <see cref="CreateCache" /> and is marked <c>[TestClass]</c> so the
/// inherited tests run against that backend.
/// </remarks>
public abstract class NotableDateCacheContractTests
{
    /// <summary>The fixed evaluation instant the contract tests store and read against.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The default time-to-live the contract tests use.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    /// <summary>
    /// Creates a fresh, empty cache for a single test.
    /// </summary>
    /// <returns>A new cache instance.</returns>
    protected abstract INotableDateCache CreateCache();

    /// <summary>
    /// Verifies that a stored year is served back as a fresh, version-matching hit carrying its computed instant.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoredAndFresh_ShouldReturnEntry()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "v1", Ttl, Now);

        Assert.IsNotNull(entry);
        Assert.AreEqual(2026, entry.Year);
        Assert.AreEqual("v1", entry.ResourceVersion);
        Assert.AreEqual(Now, entry.ComputedAtUtc);
        Assert.AreEqual(1, entry.Occurrences.Count);
    }

    /// <summary>
    /// Verifies that a year that was never stored is reported as a miss.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenNeverStored_ShouldReturnNull()
    {
        INotableDateCache cache = CreateCache();

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a stored year requested under a different resource version is not served.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenVersionDiffers_ShouldReturnNull()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        Assert.IsNull(cache.GetYear("US", 2026, "v2", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a stored year is not served once it is older than the time-to-live.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStaleUnderTtl_ShouldReturnNull()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now + Ttl + TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Verifies that a stored empty year is served as a computed-but-empty hit rather than a miss.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoredEmpty_ShouldReturnEmptyEntry()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.EmptyEntry("US", 2026, "v1", Now), Ttl, Now);

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "v1", Ttl, Now);

        Assert.IsNotNull(entry);
        Assert.AreEqual(0, entry.Occurrences.Count);
    }

    /// <summary>
    /// Verifies that territory keying is case-insensitive, so a differently cased query serves a stored year.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenTerritoryCaseDiffers_ShouldReturnEntry()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        Assert.IsNotNull(cache.GetYear("us", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that storing a year under a new resource version supersedes the entry from the previous version.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenVersionChanges_ShouldSupersedePreviousVersion()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v2", Now), Ttl, Now);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
        Assert.IsNotNull(cache.GetYear("US", 2026, "v2", Ttl, Now));
    }

    /// <summary>
    /// Verifies that distinct years for a territory are stored and served independently.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenMultipleYears_ShouldServeEachIndependently()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2027, "v1", Now), Ttl, Now);

        Assert.IsNotNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
        Assert.IsNotNull(cache.GetYear("US", 2027, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a subdivision and its parent country are kept as distinct keys.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenSubdivisionAndParent_ShouldKeepDistinct()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("CA", 2026, "v1", Now), Ttl, Now);

        Assert.IsNull(cache.GetYear("CA-ON", 2026, "v1", Ttl, Now));
        Assert.IsNotNull(cache.GetYear("CA", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a stored year's occurrences round-trip in the order they were supplied, which is the ordering
    /// contract the caching service relies on to assemble range results without re-sorting.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenMultipleOccurrences_ShouldPreserveStoredOrder()
    {
        INotableDateCache cache = CreateCache();

        // Deliberately not date-sorted: order preservation, not sortedness, is the contract under test.
        NotableDate july = NotableDateCacheTestFactory.ObservedHoliday(2026);
        NotableDate january = NotableDateCacheTestFactory.NewYearsDay(2026);
        cache.StoreYear(new NotableDateCacheEntry("US", 2026, "v1", new[] { july, january }, Now), Ttl, Now);

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "v1", Ttl, Now);

        Assert.IsNotNull(entry);
        Assert.AreEqual(2, entry.Occurrences.Count);
        Assert.AreEqual("independence-day", entry.Occurrences[0].NotableDateId);
        Assert.AreEqual("new-year", entry.Occurrences[1].NotableDateId);
    }

    /// <summary>
    /// Verifies that a stored entry with a lower-cased territory reads back self-describing with the normalized
    /// territory key.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenTerritoryLowerCase_ShouldNormalizeStoredEntry()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("us", 2026, "v1", Now), Ttl, Now);

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "v1", Ttl, Now);

        Assert.IsNotNull(entry);
        Assert.AreEqual("US", entry.Territory);
    }

    /// <summary>
    /// Verifies that clearing the cache removes every stored year.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesStored_ShouldRemoveAll()
    {
        INotableDateCache cache = CreateCache();
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        cache.Clear();

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateCache.GetYear" /> rejects a null territory.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenTerritoryNull_ShouldThrowArgumentNullException()
    {
        INotableDateCache cache = CreateCache();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.GetYear(null!, 2026, "v1", Ttl, Now);
        });
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateCache.StoreYear" /> rejects a null entry.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenEntryNull_ShouldThrowArgumentNullException()
    {
        INotableDateCache cache = CreateCache();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.StoreYear(null!, Ttl, Now);
        });
    }
}
