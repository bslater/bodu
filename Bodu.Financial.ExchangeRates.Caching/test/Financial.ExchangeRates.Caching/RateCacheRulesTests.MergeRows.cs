// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesTests.MergeRows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheRulesTests
{
    /// <summary>
    /// Verifies that a single incoming row for a new date is inserted at the correct position — front, middle, or back
    /// — of a date-sorted existing list.
    /// </summary>
    /// <param name="day">The day-of-month of the incoming row.</param>
    /// <param name="expectedIndex">The index the incoming row must land at.</param>
    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(10, 1)]
    [DataRow(20, 2)]
    public void MergeRows_WhenSingleIncomingDateIsNew_ShouldInsertInDateOrder(int day, int expectedIndex)
    {
        List<CachedRate> existing = [Row(5), Row(15)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(day)], Duration, Now);

        Assert.HasCount(3, merged);
        Assert.AreEqual(new DateOnly(2026, 6, day), merged[expectedIndex].Date);
        for (int i = 1; i < merged.Count; i++)
            Assert.IsTrue(merged[i - 1].Date < merged[i].Date, "the merged rows must remain date-ordered");
    }

    /// <summary>
    /// Verifies that a single incoming row at least as recently cached as the stored same-date row replaces it.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingIsNewerForSameDate_ShouldReplaceStoredRow()
    {
        List<CachedRate> existing = [Row(5), Row(10, rate: 2.0m, ageHours: 5), Row(15)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10, rate: 3.0m, ageHours: 1)], Duration, Now);

        Assert.HasCount(3, merged);
        Assert.AreEqual(3.0m, merged[1].Rate, "the more recently cached incoming row must win the date");
    }

    /// <summary>
    /// Verifies that a single incoming row cached earlier than the stored same-date row loses to it.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingIsOlderForSameDate_ShouldKeepStoredRow()
    {
        List<CachedRate> existing = [Row(5), Row(10, rate: 2.0m, ageHours: 1), Row(15)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10, rate: 3.0m, ageHours: 5)], Duration, Now);

        Assert.HasCount(3, merged);
        Assert.AreEqual(2.0m, merged[1].Rate, "the more recently cached stored row must win the date");
    }

    /// <summary>
    /// Verifies that the single-row merge still prunes stored rows that are no longer fresh, matching the general
    /// merge's self-cleaning pass.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingAndStoredRowIsStale_ShouldPruneStaleRow()
    {
        List<CachedRate> existing = [Row(5, ageHours: 48), Row(15)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10)], Duration, Now);

        Assert.HasCount(2, merged);
        Assert.AreEqual(new DateOnly(2026, 6, 10), merged[0].Date);
        Assert.AreEqual(new DateOnly(2026, 6, 15), merged[1].Date);
    }

    /// <summary>
    /// Verifies that a single incoming row that is invalid is skipped and cannot overwrite the valid stored row for
    /// its date.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingIsInvalid_ShouldKeepStoredRow()
    {
        List<CachedRate> existing = [Row(10, rate: 2.0m, ageHours: 5)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10, rate: 0m, ageHours: 1)], Duration, Now);

        Assert.HasCount(1, merged);
        Assert.AreEqual(2.0m, merged[0].Rate, "an invalid incoming row must not overwrite a valid stored row");
    }

    /// <summary>
    /// Verifies that an existing list that is not date-sorted falls back to the general merge and still produces the
    /// pruned, date-ordered result.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingAndExistingIsUnsorted_ShouldStillMergeAndOrder()
    {
        List<CachedRate> existing = [Row(15), Row(5)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10)], Duration, Now);

        Assert.HasCount(3, merged);
        Assert.AreEqual(new DateOnly(2026, 6, 5), merged[0].Date);
        Assert.AreEqual(new DateOnly(2026, 6, 10), merged[1].Date);
        Assert.AreEqual(new DateOnly(2026, 6, 15), merged[2].Date);
    }

    /// <summary>
    /// Verifies that an existing list carrying duplicate dates falls back to the general merge, which keeps the last
    /// occurrence per date.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingAndExistingHasDuplicateDates_ShouldKeepLastOccurrence()
    {
        List<CachedRate> existing = [Row(5, rate: 2.0m), Row(5, rate: 4.0m)];

        List<CachedRate> merged = RateCacheRules.MergeRows(existing, [Row(10)], Duration, Now);

        Assert.HasCount(2, merged);
        Assert.AreEqual(4.0m, merged[0].Rate, "the general merge keeps the last existing occurrence per date");
    }

    /// <summary>
    /// Verifies that a single incoming row merged into an empty store yields just that row.
    /// </summary>
    [TestMethod]
    public void MergeRows_WhenSingleIncomingAndExistingIsEmpty_ShouldReturnIncomingRow()
    {
        List<CachedRate> merged = RateCacheRules.MergeRows([], [Row(10)], Duration, Now);

        Assert.HasCount(1, merged);
        Assert.AreEqual(new DateOnly(2026, 6, 10), merged[0].Date);
    }
}
