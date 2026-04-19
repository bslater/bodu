// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsOutside.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // IsOutside<T>(T?, T?, T?)
    // =========================================================================

    /// <summary>
    /// Provides scenarios for <c>IsOutside</c>, which returns <see langword="true" /> only when the
    /// value strictly lies outside the inclusive range defined by the two boundaries.
    /// </summary>
    public static IEnumerable<object?[]> IsOutsideData()
    {
        // value inside range → not outside
        yield return new object?[] { 5, 1, 10, false };

        // value on lower boundary → not outside (inclusive)
        yield return new object?[] { 1, 1, 10, false };

        // value on upper boundary → not outside (inclusive)
        yield return new object?[] { 10, 1, 10, false };

        // value below range → outside
        yield return new object?[] { 0, 1, 10, true };

        // value above range → outside
        yield return new object?[] { 11, 1, 10, true };

        // reversed boundaries, value inside
        yield return new object?[] { 5, 10, 1, false };

        // reversed boundaries, value outside
        yield return new object?[] { 0, 10, 1, true };

        // null inputs never report outside
        yield return new object?[] { null, 1, 10, false };
        yield return new object?[] { 5, null, 10, false };
        yield return new object?[] { 5, 1, null, false };
    }

    /// <summary>
    /// Verifies that <c>IsOutside</c> returns the correct complement of <c>IsBetween</c>
    /// for in-range, on-boundary, outside, reversed-boundary, and null-input cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsOutsideData), DynamicDataSourceType.Method)]
    public void IsOutside_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
        int? value,
        int? lower,
        int? upper,
        bool expected)
    {
        // Explicit type parameter is required because the 3-argument overload has a
        // 'where T : IComparable<T>' constraint, and int? cannot satisfy an interface
        // constraint — type inference would otherwise pick T = int? and fail.
        Assert.AreEqual(expected, value.IsOutside<int>(lower, upper));
    }

    // =========================================================================
    // IsOutside<T>(T?, T?, T?, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload of <c>IsOutside</c> honours the supplied comparer's ordering and
    /// yields the logical complement of <c>IsBetween</c>.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenUsingReverseComparer_ShouldReturnExpectedResult()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.IsFalse(5.IsOutside(1, 10, comparer));
        Assert.IsFalse(1.IsOutside(1, 10, comparer));
        Assert.IsTrue(0.IsOutside(1, 10, comparer));
        Assert.IsTrue(11.IsOutside(1, 10, comparer));
    }

    /// <summary>
    /// Verifies that the comparer overload returns <see langword="false" /> when any input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenArgumentsAreNull_ForComparerOverload_ShouldReturnFalse()
    {
        IComparer<string> comparer = Comparer<string>.Default;

        Assert.IsFalse(((string?)null).IsOutside("a", "z", comparer));
        Assert.IsFalse("m".IsOutside((string?)null, "z", comparer));
        Assert.IsFalse("m".IsOutside("a", (string?)null, comparer));
    }

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenComparerIsNull_ShouldThrowArgumentNullException()
    {
        IComparer<int>? comparer = null;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsOutside(1, 10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
