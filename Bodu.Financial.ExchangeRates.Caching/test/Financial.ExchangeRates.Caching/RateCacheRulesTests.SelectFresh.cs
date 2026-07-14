// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesTests.SelectFresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheRulesTests
{
    /// <summary>
    /// Verifies that an already date-ordered source passes through with its order preserved.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenSourceIsSorted_ShouldPreserveOrder()
    {
        List<CachedRate> rows = [Row(1), Row(2), Row(3)];

        List<CachedRate> fresh = RateCacheRules.SelectFresh(rows, Duration, Now);

        Assert.HasCount(3, fresh);
        for (int i = 0; i < fresh.Count; i++)
            Assert.AreEqual(rows[i].Date, fresh[i].Date);
    }

    /// <summary>
    /// Verifies that an unsorted source is still returned ordered ascending by date.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenSourceIsUnsorted_ShouldReturnDateOrdered()
    {
        List<CachedRate> rows = [Row(3), Row(1), Row(2)];

        List<CachedRate> fresh = RateCacheRules.SelectFresh(rows, Duration, Now);

        Assert.HasCount(3, fresh);
        for (int i = 1; i < fresh.Count; i++)
            Assert.IsTrue(fresh[i - 1].Date < fresh[i].Date, "the selected rows must be date-ordered");
    }

    /// <summary>
    /// Verifies that a source that becomes sorted only after stale and invalid rows are filtered out does not pay the
    /// redundant sort, and the surviving rows remain correctly ordered.
    /// </summary>
    [TestMethod]
    public void SelectFresh_WhenDisorderIsOnlyInFilteredRows_ShouldReturnSurvivorsOrdered()
    {
        // The out-of-order rows are exactly the ones filtered (stale and invalid), so the survivors arrive sorted.
        List<CachedRate> rows = [Row(2), Row(1, ageHours: 48), Row(4), Row(3, rate: 0m)];

        List<CachedRate> fresh = RateCacheRules.SelectFresh(rows, Duration, Now);

        Assert.HasCount(2, fresh);
        Assert.AreEqual(new DateOnly(2026, 6, 2), fresh[0].Date);
        Assert.AreEqual(new DateOnly(2026, 6, 4), fresh[1].Date);
    }
}
