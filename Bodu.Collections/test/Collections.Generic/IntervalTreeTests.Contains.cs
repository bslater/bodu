// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Contains" /> returns <see langword="true" /> for an exactly stored
    /// interval.
    /// </summary>
    [TestMethod]
    public void Contains_WhenExactIntervalStored_ShouldReturnTrue()
    {
        var sut = CreateTree((1, 5), (10, 15));

        Assert.IsTrue(sut.Contains(1, 5));
        Assert.IsTrue(sut.Contains(10, 15));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Contains" /> returns <see langword="false" /> for an interval that
    /// overlaps stored intervals without matching one exactly.
    /// </summary>
    [TestMethod]
    public void Contains_WhenIntervalMerelyOverlaps_ShouldReturnFalse()
    {
        var sut = CreateTree((1, 10));

        Assert.IsFalse(sut.Contains(1, 9));
        Assert.IsFalse(sut.Contains(2, 10));
        Assert.IsFalse(sut.Contains(2, 9));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Contains" /> returns <see langword="false" /> on an empty tree.
    /// </summary>
    [TestMethod]
    public void Contains_WhenTreeEmpty_ShouldReturnFalse()
    {
        var sut = new IntervalTree<int>();

        Assert.IsFalse(sut.Contains(1, 5));
    }

    /// <summary>
    /// Verifies that a duplicated interval still reports as contained after all but one occurrence is removed.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDuplicateOccurrenceRemoved_ShouldStillReturnTrue()
    {
        var sut = CreateTree((1, 5), (1, 5));

        sut.Remove(1, 5);

        Assert.IsTrue(sut.Contains(1, 5));
    }

    /// <summary>
    /// Verifies that querying containment with a lower endpoint ordering after the upper endpoint throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenLowExceedsHigh_ShouldThrowArgumentException()
    {
        var sut = CreateTree((1, 5));

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Contains(5, 1);
        });

        Assert.AreEqual("low", ex.ParamName);
    }

    /// <summary>
    /// Verifies that querying containment with a <see langword="null" /> endpoint throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Contains(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Contains("a", null!);
        });
    }
}
