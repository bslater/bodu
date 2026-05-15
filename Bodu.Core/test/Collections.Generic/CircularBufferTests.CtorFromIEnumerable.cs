// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.CtorFromIEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that the array-source branch of <see cref="RingBackedCollection{T}" />'s
    /// <c>(IEnumerable&lt;T&gt;, int)</c> constructor copies the entire source when it fits within capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArraySourceFitsInCapacity_ShouldCopyAllElements()
    {
        int[] source = [1, 2, 3];
        var buffer = new CircularBuffer<int>(source, capacity: 5);

        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }
    /// <summary>
    /// Verifies that the array-source branch of <see cref="RingBackedCollection{T}" />'s
    /// <c>(IEnumerable&lt;T&gt;, int)</c> constructor retains the trailing window when the source array is longer than the
    /// supplied capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArraySourceLongerThanCapacity_ShouldRetainTrailingWindow()
    {
        int[] source = [1, 2, 3, 4, 5, 6, 7, 8];
        var buffer = new CircularBuffer<int>(source, capacity: 3);

        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 6, 7, 8 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that the non-array source branch of <see cref="RingBackedCollection{T}" />'s
    /// <c>(IEnumerable&lt;T&gt;, int)</c> constructor copies the entire source when it fits within capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonArraySourceFitsInCapacity_ShouldCopyAllElements()
    {
        IEnumerable<int> source = YieldRange(1, 3);
        var buffer = new CircularBuffer<int>(source, capacity: 5);

        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that the non-array source branch of <see cref="RingBackedCollection{T}" />'s
    /// <c>(IEnumerable&lt;T&gt;, int)</c> constructor takes the trailing window when the source is a lazy enumerable longer than
    /// the supplied capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonArraySourceLongerThanCapacity_ShouldRetainTrailingWindow()
    {
        IEnumerable<int> source = YieldRange(1, 8);
        var buffer = new CircularBuffer<int>(source, capacity: 3);

        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 6, 7, 8 }, buffer.ToArray());
    }

    private static IEnumerable<int> YieldRange(int start, int count)
    {
        for (var i = 0; i < count; i++)
            yield return start + i;
    }

}
