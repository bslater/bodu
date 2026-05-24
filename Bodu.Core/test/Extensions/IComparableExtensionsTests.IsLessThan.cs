// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsLessThan.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void IsLessThan_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsLessThan(10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
    // =========================================================================
    // IsLessThan<T>(T, T?)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>IsLessThan</c> reports whether an integer value is strictly less than a reference value
    /// for representative above, equal, and below cases.
    /// </summary>
    [TestMethod]
    [DataRow(0, 5, true, DisplayName = "Value below reference returns true")]
    [DataRow(5, 5, false, DisplayName = "Value equal to reference returns false")]
    [DataRow(10, 5, false, DisplayName = "Value above reference returns false")]
    public void IsLessThan_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int other, bool expected) => Assert.AreEqual(expected, value.IsLessThan(other));

    /// <summary>
    /// Verifies that <c>IsLessThan</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("aardvark", "apple", true, DisplayName = "String before reference returns true")]
    [DataRow("apple", "apple", false, DisplayName = "String equal to reference returns false")]
    [DataRow("banana", "apple", false, DisplayName = "String after reference returns false")]
    public void IsLessThan_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string other, bool expected) => Assert.AreEqual(expected, value.IsLessThan(other));

    /// <summary>
    /// Verifies that the comparer overload returns <see langword="false"/> when the reference value is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IsLessThan_WhenOtherIsNull_ForComparerOverload_ShouldReturnFalse()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        Assert.IsFalse(5.IsLessThan(null, comparer));
    }

    /// <summary>
    /// Verifies that <c>IsLessThan</c> returns <see langword="false"/> when the reference value is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IsLessThan_WhenOtherIsNull_ShouldReturnFalse()
    {
        string? other = null;

        Assert.IsFalse("anything".IsLessThan(other));
    }

    // =========================================================================
    // IsLessThan<T>(T, T?, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>IsLessThan</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer, numerically greater values are treated as less.
    /// </summary>
    [TestMethod]
    public void IsLessThan_WhenUsingReverseComparer_ShouldReturnExpectedResult()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.IsTrue(10.IsLessThan(5, comparer));
        Assert.IsFalse(5.IsLessThan(5, comparer));
        Assert.IsFalse(1.IsLessThan(5, comparer));
    }

}
