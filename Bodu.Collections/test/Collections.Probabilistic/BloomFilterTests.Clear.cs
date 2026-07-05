// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Clear" /> removes all previously added items, so every prior member is
    /// subsequently reported as definitely absent.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFilterHasItems_ShouldRemoveAllMembership()
    {
        var filter = new BloomFilter<int>(1000, 0.01);
        for (var i = 0; i < 500; i++)
            filter.Add(i);

        filter.Clear();

        for (var i = 0; i < 500; i++)
            Assert.IsFalse(filter.MightContain(i), $"Item {i} was still reported present after Clear.");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Clear" /> resets
    /// <see cref="BloomFilter{T}.EstimatedFalsePositiveRate" /> to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFilterHasItems_ShouldResetEstimatedFalsePositiveRateToZero()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        filter.Add(1);
        Assert.IsGreaterThan(0.0, filter.EstimatedFalsePositiveRate);

        filter.Clear();

        Assert.AreEqual(0.0, filter.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that calling <see cref="BloomFilter{T}.Clear" /> on an already-empty filter leaves it usable: items
    /// added afterwards are tracked normally.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledThenItemsReAdded_ShouldTrackNewItems()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        filter.Clear();

        filter.Add(7);

        Assert.IsTrue(filter.MightContain(7));
    }
}
