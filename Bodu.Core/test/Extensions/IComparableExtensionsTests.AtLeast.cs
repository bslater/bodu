// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.AtLeast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // AtLeast<T>(T, T)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>AtLeast</c> returns the value when it is at or above the floor, and returns the floor otherwise.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5, 10, DisplayName = "Value above floor returns value")]
    [DataRow(5, 5, 5, DisplayName = "Value on floor returns value")]
    [DataRow(0, 5, 5, DisplayName = "Value below floor returns floor")]
    public void AtLeast_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(int value, int min, int expected) => Assert.AreEqual(expected, value.AtLeast(min));

    /// <summary>
    /// Verifies that <c>AtLeast</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("banana", "apple", "banana", DisplayName = "String above floor returns value")]
    [DataRow("aardvark", "apple", "apple", DisplayName = "String below floor returns floor")]
    public void AtLeast_WhenEvaluatingStringValues_ShouldReturnExpectedResult(string value, string min, string expected) => Assert.AreEqual(expected, value.AtLeast(min));

    // =========================================================================
    // AtLeast<T>(T, T, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>AtLeast</c> honours the supplied comparer's ordering.
    /// Under a reverse comparer the numerically smaller value is treated as the floor.
    /// </summary>
    [TestMethod]
    public void AtLeast_WhenUsingReverseComparer_ShouldRespectComparerOrdering()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        // Under a reverse comparer, 1 is "greater than" 10, so 1 is already at or above the floor (10).
        Assert.AreEqual(1, 1.AtLeast(10, comparer));

        // Under a reverse comparer, 20 is "less than" 10, so 20 is below the floor and is raised to 10.
        Assert.AreEqual(10, 20.AtLeast(10, comparer));
    }

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void AtLeast_WhenComparerIsNull_ShouldThrowArgumentNullException()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.AtLeast(1, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
