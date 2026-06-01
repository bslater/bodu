// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.Min.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.Min(1, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
    // =========================================================================
    // Min<T>(T, T)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>Min</c> returns the smaller of two integer values for representative orderings,
    /// and returns the receiver when the two values compare equal.
    /// </summary>
    [TestMethod]
    [DataRow(1, 5, 1, DisplayName = "Receiver smaller returns receiver")]
    [DataRow(5, 1, 1, DisplayName = "Argument smaller returns argument")]
    [DataRow(5, 5, 5, DisplayName = "Equal values return receiver")]
    public void Min_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int other, int expected) => Assert.AreEqual(expected, value.Min(other));

    /// <summary>
    /// Verifies that <c>Min</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("apple", "banana", "apple", DisplayName = "Receiver smaller returns receiver")]
    [DataRow("banana", "apple", "apple", DisplayName = "Argument smaller returns argument")]
    public void Min_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string other, string expected) => Assert.AreEqual(expected, value.Min(other));

    // =========================================================================
    // Min<T>(T, T, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>Min</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer the numerically larger value is considered the minimum.
    /// </summary>
    [TestMethod]
    public void Min_WhenUsingReverseComparer_ShouldReturnLargestNumeric()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.AreEqual(10, 10.Min(1, comparer));
        Assert.AreEqual(10, 1.Min(10, comparer));
        Assert.AreEqual(5, 5.Min(5, comparer));
    }

}
