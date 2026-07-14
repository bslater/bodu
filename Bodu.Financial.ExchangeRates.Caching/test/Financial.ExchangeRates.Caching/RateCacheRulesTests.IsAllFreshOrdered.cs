// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesTests.IsAllFreshOrdered.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheRulesTests
{
    /// <summary>
    /// Verifies that a date-ordered list of valid, fresh rows qualifies for the zero-copy serve.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenAllRowsFreshAndOrdered_ShouldReturnTrue()
    {
        IReadOnlyList<CachedRate> rows = [Row(1), Row(2), Row(3)];

        Assert.IsTrue(RateCacheRules.IsAllFreshOrdered(rows, Duration, Now));
    }

    /// <summary>
    /// Verifies that an empty list trivially qualifies.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(RateCacheRules.IsAllFreshOrdered(Array.Empty<CachedRate>(), Duration, Now));
    }

    /// <summary>
    /// Verifies that one stale row disqualifies the list, forcing the filtered-copy path.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenOneRowStale_ShouldReturnFalse()
    {
        IReadOnlyList<CachedRate> rows = [Row(1), Row(2, ageHours: 48), Row(3)];

        Assert.IsFalse(RateCacheRules.IsAllFreshOrdered(rows, Duration, Now));
    }

    /// <summary>
    /// Verifies that one invalid row (non-positive rate) disqualifies the list.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenOneRowInvalid_ShouldReturnFalse()
    {
        IReadOnlyList<CachedRate> rows = [Row(1), Row(2, rate: 0m), Row(3)];

        Assert.IsFalse(RateCacheRules.IsAllFreshOrdered(rows, Duration, Now));
    }

    /// <summary>
    /// Verifies that out-of-order dates disqualify the list even when every row is fresh and valid.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenRowsUnordered_ShouldReturnFalse()
    {
        IReadOnlyList<CachedRate> rows = [Row(2), Row(1)];

        Assert.IsFalse(RateCacheRules.IsAllFreshOrdered(rows, Duration, Now));
    }

    /// <summary>
    /// Verifies that a qualifying list is exactly what <see cref="RateCacheRules.SelectFresh" /> would produce, so the
    /// zero-copy serve is behaviourally equivalent.
    /// </summary>
    [TestMethod]
    public void IsAllFreshOrdered_WhenTrue_ShouldMatchSelectFreshOutput()
    {
        List<CachedRate> rows = [Row(1), Row(2), Row(3)];

        Assert.IsTrue(RateCacheRules.IsAllFreshOrdered(rows, Duration, Now));
        CollectionAssert.AreEqual(rows, RateCacheRules.SelectFresh(rows, Duration, Now));
    }
}
