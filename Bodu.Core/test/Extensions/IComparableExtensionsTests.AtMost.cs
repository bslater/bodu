// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.AtMost.cs" company="Bodu Pty. Ltd.">
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
    public void AtMost_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.AtMost(10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
    // =========================================================================
    // AtMost<T>(T, T)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>AtMost</c> returns the value when it is at or below the ceiling, and returns the ceiling otherwise.
    /// </summary>
    [TestMethod]
    [DataRow(0, 5, 0, DisplayName = "Value below ceiling returns value")]
    [DataRow(5, 5, 5, DisplayName = "Value on ceiling returns value")]
    [DataRow(10, 5, 5, DisplayName = "Value above ceiling returns ceiling")]
    public void AtMost_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int max, int expected) => Assert.AreEqual(expected, value.AtMost(max));

    /// <summary>
    /// Verifies that <c>AtMost</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("aardvark", "apple", "aardvark", DisplayName = "String below ceiling returns value")]
    [DataRow("banana", "apple", "apple", DisplayName = "String above ceiling returns ceiling")]
    public void AtMost_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string max, string expected) => Assert.AreEqual(expected, value.AtMost(max));

    // =========================================================================
    // AtMost<T>(T, T, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>AtMost</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer the numerically larger value is treated as the ceiling.
    /// </summary>
    [TestMethod]
    public void AtMost_WhenUsingReverseComparer_ShouldRespectComparerOrdering()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        // Under a reverse comparer, 20 is "less than" 10, so 20 is already at or below the ceiling (10).
        Assert.AreEqual(20, 20.AtMost(10, comparer));

        // Under a reverse comparer, 1 is "greater than" 10, so 1 is above the ceiling and is lowered to 10.
        Assert.AreEqual(10, 1.AtMost(10, comparer));
    }

}
