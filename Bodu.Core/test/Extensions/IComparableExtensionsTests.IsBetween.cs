// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsBetween.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // IsBetween<T>(T?, T?, T?)
    // =========================================================================

    /// <summary>
    /// Provides inclusive-range scenarios for <c>IsBetween</c> covering in-range, on-boundary,
    /// out-of-range, reversed-boundary, and null-input cases.
    /// </summary>
    public static IEnumerable<object?[]> IsBetweenData()
    {
        // value within range
        yield return new object?[] { 5, 1, 10, true };

        // value on lower boundary
        yield return new object?[] { 1, 1, 10, true };

        // value on upper boundary
        yield return new object?[] { 10, 1, 10, true };

        // value below range
        yield return new object?[] { 0, 1, 10, false };

        // value above range
        yield return new object?[] { 11, 1, 10, false };

        // reversed boundaries — value inside
        yield return new object?[] { 5, 10, 1, true };

        // reversed boundaries — on boundary
        yield return new object?[] { 10, 10, 1, true };

        // boundaries equal — value equal
        yield return new object?[] { 5, 5, 5, true };

        // boundaries equal — value different
        yield return new object?[] { 4, 5, 5, false };

        // null inputs
        yield return new object?[] { null, 1, 10, false };
        yield return new object?[] { 5, null, 10, false };
        yield return new object?[] { 5, 1, null, false };
        yield return new object?[] { null, null, null, false };
    }

    /// <summary>
    /// Verifies that <c>IsBetween</c> correctly reports whether a nullable integer value falls
    /// within an inclusive range for a variety of normal, boundary, reversed, and null-input cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsBetweenData), DynamicDataSourceType.Method)]
    public void IsBetween_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
        int? value,
        int? lower,
        int? upper,
        bool expected)
    {
        Assert.AreEqual(expected, value.IsBetween(lower, upper));
    }

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
        bool expected)
    {
        Assert.AreEqual(expected, value.IsBetween(lower, upper));
    }

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
    /// Verifies that the comparer overload returns <see langword="false" /> when any input value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenArgumentsAreNull_ForComparerOverload_ShouldReturnFalse()
    {
        IComparer<string> comparer = Comparer<string>.Default;

        Assert.IsFalse(((string?)null).IsBetween("a", "z", comparer));
        Assert.IsFalse("m".IsBetween((string?)null, "z", comparer));
        Assert.IsFalse("m".IsBetween("a", (string?)null, comparer));
    }

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenComparerIsNull_ShouldThrowArgumentNullException()
    {
        IComparer<int>? comparer = null;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsBetween(1, 10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
