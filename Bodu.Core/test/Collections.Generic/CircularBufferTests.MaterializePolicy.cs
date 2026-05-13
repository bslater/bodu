// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.MaterializePolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that the <c>(IEnumerable, capacity, allowOverwrite=true)</c> constructor retains only the newest
    /// elements when the non-array source is larger than capacity, exercising the
    /// <c>MaterializeWithOverflowPolicy</c> path that falls through to <see cref="System.Linq.Enumerable.TakeLast{T}" />.
    /// </summary>
    [TestMethod]
    public void CircularBufferMaterializeWithOverflowPolicy_WhenOverflowAllowed_ShouldKeepNewestItems()
    {
        IEnumerable<int> source = YieldMaterializeSequence(1, 8);

        var buffer = new CircularBuffer<int>(source, capacity: 3, allowOverwrite: true);

        CollectionAssert.AreEqual(new[] { 6, 7, 8 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that the <c>(IEnumerable, capacity, allowOverwrite=false)</c> constructor throws when the
    /// non-array source exceeds capacity, exercising the <c>MaterializeWithOverflowPolicy</c>
    /// length-vs-capacity guard with a non-array input.
    /// </summary>
    [TestMethod]
    public void CircularBufferMaterializeWithOverflowPolicy_WhenOverflowDisallowed_ShouldThrow()
    {
        IEnumerable<int> source = YieldMaterializeSequence(1, 5);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new CircularBuffer<int>(source, capacity: 3, allowOverwrite: false);
        });
    }

    /// <summary>
    /// Verifies that the <c>(IEnumerable, capacity, allowOverwrite=false)</c> constructor copies all elements
    /// when the non-array source fits within the supplied capacity, exercising the
    /// <c>MaterializeWithOverflowPolicy</c> happy path on a deferred enumerable.
    /// </summary>
    [TestMethod]
    public void CircularBufferMaterializeWithOverflowPolicy_WhenNonArraySourceFitsAndOverflowDisallowed_ShouldCopyAllElements()
    {
        IEnumerable<int> source = YieldMaterializeSequence(1, 3);

        var buffer = new CircularBuffer<int>(source, capacity: 5, allowOverwrite: false);

        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor preserves every element when the source length exactly equals capacity,
    /// hitting the <c>items.Length == capacity</c> boundary inside the base
    /// <see cref="RingBackedCollection{T}" /> constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExactlyEqualsCapacity_ShouldPreserveAllItems()
    {
        var buffer = new CircularBuffer<int>(new[] { 1, 2, 3 }, capacity: 3);

        Assert.AreEqual(3, buffer.Count);
        Assert.AreEqual(3, buffer.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    private static IEnumerable<int> YieldMaterializeSequence(int start, int count)
    {
        for (int i = 0; i < count; i++)
            yield return start + i;
    }
}
