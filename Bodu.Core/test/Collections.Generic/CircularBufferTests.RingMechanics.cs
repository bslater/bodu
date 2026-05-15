// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.RingMechanics.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.Contains(T)" /> returns <see langword="false" /> when
    /// the buffer is full and wrapped to <c>_head == _tail == 0</c> and the item is absent. Exercises the
    /// short-circuit branch where the second-segment lookup is skipped because the tail offset is zero.
    /// </summary>
    [TestMethod]
    public void Contains_WhenBufferFullAndWrappedToZeroAndItemAbsent_ShouldReturnFalse()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        Assert.IsFalse(buffer.Contains(99));
    }
    /// <summary>
    /// Verifies that the strongly-typed <see cref="RingBackedCollection{T}.GetEnumerator()" /> method returns an
    /// enumerator struct that walks the live region in head-to-tail order. Exercises the public typed overload
    /// that <see langword="foreach" /> over an interface-typed reference does not reach.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCalledOnTypedReference_ShouldEnumerateInHeadToTailOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        var actual = new System.Collections.Generic.List<int>();
        using (RingBackedCollection<int>.Enumerator enumerator = buffer.GetEnumerator())
        {
            while (enumerator.MoveNext())
                actual.Add(enumerator.Current);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.TrimExcess" /> on an empty buffer that was previously at
    /// a larger capacity exercises the empty-buffer branch of the internal <c>Resize</c> path (where the live
    /// region is not copied). Also exercises the <c>_count == newCapacity</c> ternary's false branch.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsEmpty_ShouldShrinkToOneSlotWithoutCopying()
    {
        var buffer = new CircularBuffer<int>(8);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Clear();

        buffer.TrimExcess();

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(1, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.TrimExcess" /> on a partially-filled buffer reduces the
    /// capacity to exactly the live count, exercising the <c>_count == newCapacity</c> ternary's true branch in
    /// the internal <c>Resize</c> path.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsPartiallyFilled_ShouldShrinkCapacityToExactCount()
    {
        var buffer = new CircularBuffer<int>(8);
        buffer.Enqueue(10);
        buffer.Enqueue(20);
        buffer.Enqueue(30);

        buffer.TrimExcess();

        Assert.AreEqual(3, buffer.Count);
        Assert.AreEqual(3, buffer.Capacity);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, buffer.ToArray());
    }

}
