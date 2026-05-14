// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelperTests.Min.Comparer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ComparableHelperTests
{
    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Min{T}(T, T, IComparer{T})" /> returns the smaller value
    /// according to the supplied comparer.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2, 1)]
    [DataRow(5, 5, 5)]
    [DataRow(10, 3, 3)]
    public void Min_WhenComparerIsDefault_ShouldReturnSmaller(int first, int second, int expected)
    {
        var actual = ComparableHelper.Min(first, second, Comparer<int>.Default);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Min{T}(T, T, IComparer{T})" /> reverses the result when a
    /// reverse comparer is supplied.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparerIsReversed_ShouldReturnNaturallyLargerValue()
    {
        var reverse = Comparer<int>.Create((a, b) => b.CompareTo(a));

        Assert.AreEqual(2, ComparableHelper.Min(1, 2, reverse));
        Assert.AreEqual(10, ComparableHelper.Min(10, 3, reverse));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Min{T}(T, T, IComparer{T})" /> returns the non-null value
    /// when one operand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Min_WhenOneArgumentIsNullWithComparer_ShouldReturnNonNullValue()
    {
        string? first = null;
        var second = "abc";

        Assert.AreEqual("abc", ComparableHelper.Min(first, second, StringComparer.Ordinal));
        Assert.AreEqual("abc", ComparableHelper.Min(second, first, StringComparer.Ordinal));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Min{T}(T, T, IComparer{T})" /> returns <see langword="null" />
    /// when both operands are <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Min_WhenBothArgumentsAreNullWithComparer_ShouldReturnNull()
    {
        string? first = null;
        string? second = null;

        Assert.IsNull(ComparableHelper.Min(first, second, StringComparer.Ordinal));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Min{T}(T, T, IComparer{T})" /> throws
    /// <see cref="ArgumentNullException" /> when the comparer is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparerIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ComparableHelper.Min(1, 2, (IComparer<int>)null!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
