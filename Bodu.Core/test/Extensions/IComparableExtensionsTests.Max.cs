// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.Max.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.Max(10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
    // =========================================================================
    // Max<T>(T, T)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>Max</c> returns the larger of two integer values for representative orderings,
    /// and returns the receiver when the two values compare equal.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 5, DisplayName = "Receiver larger returns receiver")]
    [DataRow(1, 5, 5, DisplayName = "Argument larger returns argument")]
    [DataRow(5, 5, 5, DisplayName = "Equal values return receiver")]
    public void Max_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int other, int expected) => Assert.AreEqual(expected, value.Max(other));

    /// <summary>
    /// Verifies that <c>Max</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("banana", "apple", "banana", DisplayName = "Receiver larger returns receiver")]
    [DataRow("apple", "banana", "banana", DisplayName = "Argument larger returns argument")]
    public void Max_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string other, string expected) => Assert.AreEqual(expected, value.Max(other));

    // =========================================================================
    // Max<T>(T, T, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>Max</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer the numerically smaller value is considered the maximum.
    /// </summary>
    [TestMethod]
    public void Max_WhenUsingReverseComparer_ShouldReturnSmallestNumeric()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.AreEqual(1, 1.Max(10, comparer));
        Assert.AreEqual(1, 10.Max(1, comparer));
        Assert.AreEqual(5, 5.Max(5, comparer));
    }

}
