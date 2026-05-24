// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsGreaterThanOrEqual.cs" company="Bodu Pty. Ltd.">
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
    public void IsGreaterThanOrEqual_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsGreaterThanOrEqual(1, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
    // =========================================================================
    // IsGreaterThanOrEqual<T>(T, T?)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>IsGreaterThanOrEqual</c> reports whether an integer value is greater than or equal to a reference value
    /// for representative above, equal, and below cases.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5, true, DisplayName = "Value above reference returns true")]
    [DataRow(5, 5, true, DisplayName = "Value equal to reference returns true")]
    [DataRow(0, 5, false, DisplayName = "Value below reference returns false")]
    public void IsGreaterThanOrEqual_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int other, bool expected) => Assert.AreEqual(expected, value.IsGreaterThanOrEqual(other));

    /// <summary>
    /// Verifies that <c>IsGreaterThanOrEqual</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("banana", "apple", true, DisplayName = "String after reference returns true")]
    [DataRow("apple", "apple", true, DisplayName = "String equal to reference returns true")]
    [DataRow("aardvark", "apple", false, DisplayName = "String before reference returns false")]
    public void IsGreaterThanOrEqual_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string other, bool expected) => Assert.AreEqual(expected, value.IsGreaterThanOrEqual(other));

    /// <summary>
    /// Verifies that the comparer overload returns <see langword="false"/> when the reference value is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IsGreaterThanOrEqual_WhenOtherIsNull_ForComparerOverload_ShouldReturnFalse()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        Assert.IsFalse(5.IsGreaterThanOrEqual(null, comparer));
    }

    /// <summary>
    /// Verifies that <c>IsGreaterThanOrEqual</c> returns <see langword="false"/> when the reference value is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IsGreaterThanOrEqual_WhenOtherIsNull_ShouldReturnFalse()
    {
        string? other = null;

        Assert.IsFalse("anything".IsGreaterThanOrEqual(other));
    }

    // =========================================================================
    // IsGreaterThanOrEqual<T>(T, T?, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>IsGreaterThanOrEqual</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer, numerically smaller or equal values are treated as greater-or-equal.
    /// </summary>
    [TestMethod]
    public void IsGreaterThanOrEqual_WhenUsingReverseComparer_ShouldReturnExpectedResult()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.IsTrue(1.IsGreaterThanOrEqual(5, comparer));
        Assert.IsTrue(5.IsGreaterThanOrEqual(5, comparer));
        Assert.IsFalse(10.IsGreaterThanOrEqual(5, comparer));
    }

}
