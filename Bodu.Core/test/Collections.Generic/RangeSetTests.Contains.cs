// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Contains.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Contains(T)" /> on an empty set returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new RangeSet<int>();

        Assert.IsFalse(sut.Contains(5));
    }
    // --------------------------------------------------------
    // Contains(T) — value membership
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Contains(T)" /> rejects a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueIsNull_ShouldThrowExactly()
    {
        var sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains((string)null!);
        });
    }

    /// <summary>
    /// Verifies the value-membership outcome across boundary and interior positions.
    /// </summary>
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, true)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(14, true)]
    [DataRow(15, false)]
    [DataRow(int.MaxValue, false)]
    public void Contains_WhenValuesVary_ShouldReturnExpected(int value, bool expected)
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        Assert.AreEqual(expected, sut.Contains(value));
    }

    // --------------------------------------------------------
    // Contains(T, T) — range containment
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the range-containment overload rejects <see langword="null" /> endpoints.
    /// </summary>
    [TestMethod]
    public void ContainsRange_WhenEndpointIsNull_ShouldThrowExactly()
    {
        var sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!, "z");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains("a", null!);
        });
    }

    /// <summary>
    /// Verifies the range-containment outcome across representative combinations.
    /// </summary>
    [TestMethod]
    [DataRow(0, 5, true)]
    [DataRow(0, 4, true)]
    [DataRow(1, 4, true)]
    [DataRow(0, 6, false)]
    [DataRow(5, 10, false)]
    [DataRow(4, 11, false)]
    public void ContainsRange_WhenInputsVary_ShouldReturnExpected(int start, int end, bool expected)
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        Assert.AreEqual(expected, sut.Contains(start, end));
    }

    /// <summary>
    /// Verifies that the range-containment overload on an empty set always returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ContainsRange_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new RangeSet<int>();

        Assert.IsFalse(sut.Contains(0, 10));
    }

    /// <summary>
    /// Verifies that the range-containment overload rejects degenerate ranges.
    /// </summary>
    [TestMethod]
    public void ContainsRange_WhenStartIsNotLessThanEnd_ShouldThrowExactly()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Contains(5, 5);
        });
    }

}
