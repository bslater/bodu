// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Unit tests for <see cref="BloomFilter{T}" />.
/// </summary>
[TestClass]
public sealed partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that a freshly constructed filter finds every one of 100 added items, exercising the primary
    /// add-then-query happy path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void BloomFilter_AddThenMightContain_ShouldFindAllAddedItems()
    {
        var filter = new BloomFilter<int>(expectedItems: 100, falsePositiveRate: 0.01);

        for (var i = 0; i < 100; i++)
            filter.Add(i);

        for (var i = 0; i < 100; i++)
            Assert.IsTrue(filter.MightContain(i), $"Added item {i} was not found.");
    }

    /// <summary>
    /// A reference-type equality comparer whose <see cref="object.Equals(object)" /> is reference-based, so two
    /// instances are never equal — used to drive the <see cref="BloomFilter{T}.UnionWith" /> comparer-compatibility
    /// checks.
    /// </summary>
    private sealed class ReferenceOnlyIntComparer
        : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj;
    }
}
