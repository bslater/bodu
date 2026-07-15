// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheRulesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the shared freshness, version-matching, merge, and batch-selection rules of
/// <see cref="NotableDateCacheRules" /> directly, independent of any backend.
/// </summary>
[TestClass]
public sealed class NotableDateCacheRulesTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The freshness duration applied by every test.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    /// <summary>
    /// Verifies that the fresh, version-matching entry for the requested year is selected.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenEntryMatchesYearAndVersion_ShouldReturnEntry()
    {
        IReadOnlyList<NotableDateCacheEntry> entries =
        [
            NotableDateCacheTestFactory.Entry("US", 2025, "v1", Now),
            NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now),
        ];

        NotableDateCacheEntry? selected = NotableDateCacheRules.SelectFresh(entries, 2026, "v1", Ttl, Now);

        Assert.IsNotNull(selected);
        Assert.AreEqual(2026, selected.Year);
    }

    /// <summary>
    /// Verifies that an entry stored under a different resource version is never served.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenVersionDiffers_ShouldReturnNull()
    {
        IReadOnlyList<NotableDateCacheEntry> entries = [NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now)];

        Assert.IsNull(NotableDateCacheRules.SelectFresh(entries, 2026, "v2", Ttl, Now));
    }

    /// <summary>
    /// Verifies that an entry exactly one time-to-live old is stale (strict less-than freshness).
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenEntryExactlyTtlOld_ShouldReturnNull()
    {
        IReadOnlyList<NotableDateCacheEntry> entries = [NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now - Ttl)];

        Assert.IsNull(NotableDateCacheRules.SelectFresh(entries, 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that an entry stamped implausibly far in the future of the evaluating clock is rejected as invalid.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenEntryStampedFarInFuture_ShouldReturnNull()
    {
        IReadOnlyList<NotableDateCacheEntry> entries = [NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now.AddHours(1))];

        Assert.IsNull(NotableDateCacheRules.SelectFresh(entries, 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that merging an incoming year replaces the stored entry for the same year and keeps other fresh years,
    /// ordered ascending by year.
    /// </summary>
    [TestMethod]
    public void Merge_WhenIncomingReplacesSameYear_ShouldKeepOtherYearsOrdered()
    {
        IReadOnlyList<NotableDateCacheEntry> existing =
        [
            NotableDateCacheTestFactory.Entry("US", 2027, "v1", Now.AddDays(-1)),
            NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now.AddDays(-1)),
        ];
        NotableDateCacheEntry incoming = NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now);

        List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(existing, incoming, Ttl, Now);

        Assert.HasCount(2, merged);
        Assert.AreEqual(2026, merged[0].Year);
        Assert.AreEqual(Now, merged[0].ComputedAtUtc, "the incoming entry must replace the stored same-year entry");
        Assert.AreEqual(2027, merged[1].Year);
    }

    /// <summary>
    /// Verifies that a store under a new resource version drops every entry from the superseded version.
    /// </summary>
    [TestMethod]
    public void Merge_WhenVersionSupersedes_ShouldDropOldVersionEntries()
    {
        IReadOnlyList<NotableDateCacheEntry> existing =
        [
            NotableDateCacheTestFactory.Entry("US", 2025, "v1", Now),
            NotableDateCacheTestFactory.Entry("US", 2027, "v1", Now),
        ];
        NotableDateCacheEntry incoming = NotableDateCacheTestFactory.Entry("US", 2026, "v2", Now);

        List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(existing, incoming, Ttl, Now);

        Assert.HasCount(1, merged);
        Assert.AreEqual("v2", merged[0].ResourceVersion);
    }

    /// <summary>
    /// Verifies that stale stored entries are pruned on merge so the store self-cleans on every write.
    /// </summary>
    [TestMethod]
    public void Merge_WhenStoredEntryIsStale_ShouldPruneIt()
    {
        IReadOnlyList<NotableDateCacheEntry> existing = [NotableDateCacheTestFactory.Entry("US", 2025, "v1", Now - Ttl)];
        NotableDateCacheEntry incoming = NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now);

        List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(existing, incoming, Ttl, Now);

        Assert.HasCount(1, merged);
        Assert.AreEqual(2026, merged[0].Year);
    }

    /// <summary>
    /// Verifies that the single-pass batch selection fills exactly the slots a per-year selection would, over a span
    /// with hits, misses, a version mismatch, and a stale year.
    /// </summary>
    [TestMethod]
    public void SelectFreshInto_WhenMixedSpan_ShouldMatchPerYearSelectFresh()
    {
        IReadOnlyList<NotableDateCacheEntry> entries =
        [
            NotableDateCacheTestFactory.Entry("US", 2025, "v1", Now),
            NotableDateCacheTestFactory.Entry("US", 2026, "v2", Now),
            NotableDateCacheTestFactory.Entry("US", 2027, "v1", Now - Ttl),
            NotableDateCacheTestFactory.Entry("US", 2028, "v1", Now),
            NotableDateCacheTestFactory.Entry("US", 2030, "v1", Now),
        ];

        var batch = new NotableDateCacheEntry?[5];
        NotableDateCacheRules.SelectFreshInto(entries, 2025, "v1", Ttl, Now, batch);

        for (int year = 2025; year <= 2029; year++)
        {
            NotableDateCacheEntry? expected = NotableDateCacheRules.SelectFresh(entries, year, "v1", Ttl, Now);
            Assert.AreEqual(expected, batch[year - 2025], $"slot for {year} must match the per-year selection");
        }
    }

    /// <summary>
    /// Verifies that when duplicate qualifying entries exist for one year, the batch selection keeps the first — the
    /// same first-match-wins rule the per-year selection applies.
    /// </summary>
    [TestMethod]
    public void SelectFreshInto_WhenDuplicateYearEntries_ShouldKeepFirstMatch()
    {
        NotableDateCacheEntry first = NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now.AddDays(-1));
        NotableDateCacheEntry second = NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now);
        IReadOnlyList<NotableDateCacheEntry> entries = [first, second];

        var batch = new NotableDateCacheEntry?[1];
        NotableDateCacheRules.SelectFreshInto(entries, 2026, "v1", Ttl, Now, batch);

        Assert.AreSame(first, batch[0]);
        Assert.AreSame(NotableDateCacheRules.SelectFresh(entries, 2026, "v1", Ttl, Now), batch[0]);
    }

    /// <summary>
    /// Verifies that entries outside the requested span leave their neighbours' slots untouched and cause no error.
    /// </summary>
    [TestMethod]
    public void SelectFreshInto_WhenEntriesOutsideSpan_ShouldLeaveSlotsNull()
    {
        IReadOnlyList<NotableDateCacheEntry> entries =
        [
            NotableDateCacheTestFactory.Entry("US", 2020, "v1", Now),
            NotableDateCacheTestFactory.Entry("US", 2030, "v1", Now),
        ];

        var batch = new NotableDateCacheEntry?[3];
        NotableDateCacheRules.SelectFreshInto(entries, 2025, "v1", Ttl, Now, batch);

        foreach (NotableDateCacheEntry? slot in batch)
            Assert.IsNull(slot);
    }
}
