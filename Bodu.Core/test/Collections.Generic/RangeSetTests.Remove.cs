// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that removing a range with a <see langword="null" /> end is rejected.
    /// </summary>
    [TestMethod]
    public void Remove_WhenEndIsNull_ShouldThrowExactly()
    {
        var sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Remove("a", null!);
        });
    }

    /// <summary>
    /// Verifies that removing a range that does not overlap any existing range returns
    /// <see langword="false" /> and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeDoesNotOverlap_ShouldReturnFalse()
    {
        RangeSet<int> sut = CreateSet((10, 20));

        var changed = sut.Remove(0, 10);

        Assert.IsFalse(changed);
        AssertContents(sut, (10, 20));
    }

    /// <summary>
    /// Verifies that removing a range that engulfs an existing range removes the entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeEngulfsExisting_ShouldRemoveExisting()
    {
        RangeSet<int> sut = CreateSet((5, 10));

        var changed = sut.Remove(0, 20);

        Assert.IsTrue(changed);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that removing a range fully contained within an existing range splits it into two.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeIsInteriorOfExisting_ShouldSplitIntoTwo()
    {
        RangeSet<int> sut = CreateSet((0, 20));

        var changed = sut.Remove(8, 12);

        Assert.IsTrue(changed);
        AssertContents(sut, (0, 8), (12, 20));
    }

    // --------------------------------------------------------
    // Remove(T, T) — full / partial / split
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing a range that exactly matches an existing range removes the entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeMatchesExisting_ShouldRemoveExisting()
    {
        RangeSet<int> sut = CreateSet((0, 10));

        var changed = sut.Remove(0, 10);

        Assert.IsTrue(changed);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that touching ranges (where end == other.start) are not considered overlapping, so removal
    /// of a touching boundary is a no-op.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeOnlyTouchesEndpoint_ShouldNotAffectSet()
    {
        RangeSet<int> sut = CreateSet((0, 5));

        var changed = sut.Remove(5, 10);

        Assert.IsFalse(changed);
        AssertContents(sut, (0, 5));
    }

    /// <summary>
    /// Verifies that removing a range that overlaps the left edge of an existing range trims the left side.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeOverlapsLeftEdge_ShouldTrimLeft()
    {
        RangeSet<int> sut = CreateSet((5, 15));

        var changed = sut.Remove(0, 10);

        Assert.IsTrue(changed);
        AssertContents(sut, (10, 15));
    }

    /// <summary>
    /// Verifies that removing a range that overlaps the right edge of an existing range trims the right side.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeOverlapsRightEdge_ShouldTrimRight()
    {
        RangeSet<int> sut = CreateSet((5, 15));

        var changed = sut.Remove(10, 20);

        Assert.IsTrue(changed);
        AssertContents(sut, (5, 10));
    }

    /// <summary>
    /// Verifies that removing a range that spans multiple existing ranges trims or removes each as needed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeSpansMultipleExisting_ShouldTrimEach()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15), (20, 25));

        var changed = sut.Remove(3, 22);

        Assert.IsTrue(changed);
        AssertContents(sut, (0, 3), (22, 25));
    }

    // --------------------------------------------------------
    // Remove(Range<T>) overload
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="RangeSet{T}.Remove(Range{T})" /> overload delegates to the endpoint
    /// overload.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeStructProvided_ShouldDelegateToEndpointOverload()
    {
        RangeSet<int> sut = CreateSet((0, 10));

        var changed = sut.Remove(new Range<int>(0, 10));

        Assert.IsTrue(changed);
        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Remove(T, T) — empty / non-overlapping
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing from an empty set returns <see langword="false" /> and leaves the set empty.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new RangeSet<int>();

        var changed = sut.Remove(0, 10);

        Assert.IsFalse(changed);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that removing a range with <c>start &gt;= end</c> is rejected.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(10, 5)]
    public void Remove_WhenStartIsNotLessThanEnd_ShouldThrowExactly(int start, int end)
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Remove(start, end);
        });
    }
    // --------------------------------------------------------
    // Remove(T, T) — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing a range with a <see langword="null" /> start is rejected.
    /// </summary>
    [TestMethod]
    public void Remove_WhenStartIsNull_ShouldThrowExactly()
    {
        var sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Remove(null!, "z");
        });
    }

}
