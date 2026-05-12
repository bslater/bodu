// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsBetween.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // IsBetween<T>(T?, T?, T?)
    // =========================================================================

    //TOFIX - Error(active)  CS1929	'int' does not contain a definition for 'IsBetween' and the best extension method overload 'IComparableExtensions.IsBetween<int?>(int?, int?, int?)' requires a receiver of type 'int?'	Bodu.Core.Test C:\Users\bslater\OneDrive\Code\Git\Bodu\Bodu.Core\test\Extensions\IComparableExtensionsTests.IsBetween.cs	41	
    ///// <summary>
    ///// Verifies that <c>IsBetween</c> correctly reports whether a nullable integer value falls
    ///// within an inclusive range for a variety of normal, boundary, reversed, and null-input cases.
    ///// </summary>
    //[TestMethod]
    //[DataRow(5, 1, 10, true, DisplayName = "Value within range returns true")]
    //[DataRow(1, 1, 10, true, DisplayName = "Value on lower boundary returns true")]
    //[DataRow(10, 1, 10, true, DisplayName = "Value on upper boundary returns true")]
    //[DataRow(0, 1, 10, false, DisplayName = "Value below range returns false")]
    //[DataRow(11, 1, 10, false, DisplayName = "Value above range returns false")]
    //[DataRow(5, 10, 1, true, DisplayName = "Reversed boundaries with value inside returns true")]
    //[DataRow(10, 10, 1, true, DisplayName = "Reversed boundaries with value on boundary returns true")]
    //[DataRow(5, 5, 5, true, DisplayName = "Equal boundaries with matching value returns true")]
    //[DataRow(4, 5, 5, false, DisplayName = "Equal boundaries with non-matching value returns false")]
    //[DataRow(5, null, 10, false, DisplayName = "Null lower bound returns false")]
    //[DataRow(5, 1, null, false, DisplayName = "Null upper bound returns false")]
    //public void IsBetween_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
    //    int value,
    //    int? lower,
    //    int? upper,
    //    bool expected)
    //{
    //    Assert.AreEqual(expected, value.IsBetween(lower, upper));
    //}

    /// <summary>
    /// Verifies that <c>IsBetween</c> works correctly with a reference type (string) using natural ordering.
    /// </summary>
    [TestMethod]
    [DataRow("banana", "apple", "cherry", true, DisplayName = "String in range")]
    [DataRow("apple", "apple", "cherry", true, DisplayName = "String on lower boundary")]
    [DataRow("cherry", "apple", "cherry", true, DisplayName = "String on upper boundary")]
    [DataRow("aardvark", "apple", "cherry", false, DisplayName = "String below range")]
    [DataRow("date", "apple", "cherry", false, DisplayName = "String above range")]
    public void IsBetween_WhenEvaluatingStringValues_ShouldReturnExpectedResult(
        string value,
        string lower,
        string upper,
        bool expected) => Assert.AreEqual(expected, value.IsBetween(lower, upper));

    // =========================================================================
    // IsBetween<T>(T?, T?, T?, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>IsBetween</c> honours the supplied comparer's ordering.
    /// A reverse comparer changes which boundary is considered lower, so the same numeric arguments
    /// yield the same logical in-range result regardless of which boundary ordering is passed.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenUsingReverseComparer_ShouldReturnExpectedResult()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.IsTrue(5.IsBetween(1, 10, comparer));
        Assert.IsTrue(1.IsBetween(1, 10, comparer));
        Assert.IsTrue(10.IsBetween(1, 10, comparer));
        Assert.IsFalse(0.IsBetween(1, 10, comparer));
        Assert.IsFalse(11.IsBetween(1, 10, comparer));
    }


    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenComparerIsNull_ShouldThrowArgumentNullException()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsBetween(1, 10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
