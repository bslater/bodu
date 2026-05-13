// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.GrowthBoundary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    /// <summary>
    /// Verifies that adding items past the 75% load factor on a freshly-constructed storage triggers the hash
    /// table to grow and keeps Contains working correctly after each rehash, exercising the
    /// <c>RoundUpToPowerOfTwo</c> and <c>EnsureHashCapacity</c> bucket-growth branches.
    /// </summary>
    [TestMethod]
    public void Add_WhenLoadFactorExceeded_ShouldRehashAndPreserveContains()
    {
        var sut = new OrderedSetStorage<int>(capacity: 0, comparer: null);

        // Force several growth cycles by adding well beyond the default 4-bucket capacity.
        for (int i = 0; i < 50; i++)
            sut.Add(i);

        Assert.AreEqual(50, sut.Count);

        // All inserted items must remain findable after every rehash that occurred during the run.
        for (int i = 0; i < 50; i++)
            Assert.IsTrue(sut.Contains(i), $"Storage lost item {i} during rehash.");
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.EnsureCapacity(int)" /> expands the item array to at least
    /// the requested capacity, exercising the <c>GrowCapacity</c> <c>capacity &lt; minimum</c> branch where the
    /// requested minimum exceeds the doubled current size.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequiredExceedsDouble_ShouldReturnRequiredCapacity()
    {
        var sut = new OrderedSetStorage<int>(capacity: 4, comparer: null);

        int reported = sut.EnsureCapacity(1024);

        Assert.IsTrue(reported >= 1024);
        Assert.IsTrue(sut.Capacity >= 1024);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.EnsureCapacity(int)" /> growing from a zero-capacity
    /// storage produces at least the default item capacity floor, exercising the
    /// <c>_items.Length == 0</c> branch in <c>GrowCapacity</c>.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenStorageIsEmpty_ShouldReturnAtLeastDefaultCapacity()
    {
        var sut = new OrderedSetStorage<int>(capacity: 0, comparer: null);

        int reported = sut.EnsureCapacity(1);

        Assert.IsTrue(reported >= 4, $"Expected at least DefaultCapacity (4), got {reported}.");
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.AddRange(System.Collections.Generic.IEnumerable{T})" />
    /// with an empty source does not allocate or grow the hash table, exercising the <c>itemCount == 0</c>
    /// early return in <c>EnsureHashCapacity</c>.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceIsEmpty_ShouldNotGrowStorage()
    {
        var sut = new OrderedSetStorage<int>(capacity: 0, comparer: null);
        int capacityBefore = sut.Capacity;

        int added = sut.AddRange(System.Array.Empty<int>());

        Assert.AreEqual(0, added);
        Assert.AreEqual(capacityBefore, sut.Capacity);
        Assert.AreEqual(0, sut.Count);
    }
}
