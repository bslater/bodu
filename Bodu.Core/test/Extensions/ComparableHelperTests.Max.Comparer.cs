// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelperTests.Max.Comparer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ComparableHelperTests
{

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Max{T}(T, T, IComparer{T})" /> returns <see langword="null" />
    /// when both operands are <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Max_WhenBothArgumentsAreNullWithComparer_ShouldReturnNull()
    {
        string? first = null;
        string? second = null;

        Assert.IsNull(ComparableHelper.Max(first, second, StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Max{T}(T, T, IComparer{T})" /> returns the larger value
    /// according to the supplied comparer.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2, 2)]
    [DataRow(5, 5, 5)]
    [DataRow(10, 3, 10)]
    public void Max_WhenComparerIsDefault_ShouldReturnLarger(int first, int second, int expected)
    {
        var actual = ComparableHelper.Max(first, second, Comparer<int>.Default);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Max{T}(T, T, IComparer{T})" /> throws
    /// <see cref="ArgumentNullException" /> when the comparer is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparerIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ComparableHelper.Max(1, 2, (IComparer<int>)null!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Max{T}(T, T, IComparer{T})" /> reverses the result when a
    /// reverse comparer is supplied.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparerIsReversed_ShouldReturnNaturallySmallerValue()
    {
        var reverse = Comparer<int>.Create((a, b) => b.CompareTo(a));

        Assert.AreEqual(1, ComparableHelper.Max(1, 2, reverse));
        Assert.AreEqual(3, ComparableHelper.Max(10, 3, reverse));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="ComparableHelper.Max{T}(T, T, IComparer{T})" /> returns the non-null value
    /// when one operand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Max_WhenOneArgumentIsNullWithComparer_ShouldReturnNonNullValue()
    {
        string? first = null;
        var second = "abc";

        Assert.AreEqual("abc", ComparableHelper.Max(first, second, StringComparer.Ordinal));
        Assert.AreEqual("abc", ComparableHelper.Max(second, first, StringComparer.Ordinal));
    }

}
