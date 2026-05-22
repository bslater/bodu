// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> copies every element when the destination array is
    /// exactly large enough.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayLargeEnough_ShouldCopyEveryElement()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        var array = new int[3];

        set.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> begins copying at the supplied destination index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexSupplied_ShouldCopyStartingAtThatIndex()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2 });
        var array = new int[5];

        set.CopyTo(array, 2);

        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(0, array[1]);
        Assert.AreEqual(0, array[4]);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, new[] { array[2], array[3] });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> destination array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1 });

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.CopyTo(null!, 0);
        });

        Assert.AreEqual("array", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> throws <see cref="ArgumentOutOfRangeException" /> for a
    /// negative destination index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1 });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            set.CopyTo(new int[1], -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> throws <see cref="ArgumentException" /> when the
    /// destination array has insufficient space.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayTooSmall_ShouldThrowArgumentException()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            set.CopyTo(new int[2], 0);
        });
    }
}
