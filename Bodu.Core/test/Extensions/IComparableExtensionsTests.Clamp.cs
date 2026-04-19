// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.Clamp.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // Clamp<T>(T?, T?, T?)
    // =========================================================================

    /// <summary>
    /// Provides clamp scenarios covering below-min, above-max, in-range, unbounded-min, unbounded-max,
    /// and null-value cases.
    /// </summary>
    public static IEnumerable<object?[]> ClampData()
    {
        // value below min → returns min
        yield return new object?[] { 0, 1, 10, 1 };

        // value above max → returns max
        yield return new object?[] { 20, 1, 10, 10 };

        // value within range → returns value
        yield return new object?[] { 5, 1, 10, 5 };

        // value on boundaries → returns value
        yield return new object?[] { 1, 1, 10, 1 };
        yield return new object?[] { 10, 1, 10, 10 };

        // null min → unbounded lower
        yield return new object?[] { -5, null, 10, -5 };
        yield return new object?[] { 50, null, 10, 10 };

        // null max → unbounded upper
        yield return new object?[] { 50, 1, null, 50 };
        yield return new object?[] { -5, 1, null, 1 };

        // both bounds null → value is unchanged
        yield return new object?[] { 5, null, null, 5 };

        // null value → returns null
        yield return new object?[] { null, 1, 10, null };
    }

    /// <summary>
    /// Verifies that <c>Clamp</c> restricts a nullable integer value to the inclusive [min, max] range,
    /// honouring unbounded (<see langword="null" />) bounds and returning <see langword="null" /> when the value is null.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ClampData), DynamicDataSourceType.Method)]
    public void Clamp_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
        int? value,
        int? min,
        int? max,
        int? expected)
    {
        // Explicit type parameter is required because the 3-argument overload has a
        // 'where T : IComparable<T>' constraint, and int? cannot satisfy an interface
        // constraint — type inference would otherwise pick T = int? and fail.
        Assert.AreEqual(expected, value.Clamp<int>(min, max));
    }

    // =========================================================================
    // Clamp<T>(T?, T?, T?, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the comparer overload clamps using the supplied comparer's ordering,
    /// producing the expected inversion when a reverse comparer is used.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenUsingReverseComparer_ShouldRespectComparerOrdering()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        // Under a reverse comparer, 20 is "less than" 10, so 20 is below min (10) and is clamped to 10.
        Assert.AreEqual(10, 20.Clamp(10, 1, comparer));

        // Under a reverse comparer, 0 is "greater than" 1, so 0 is above max (1) and is clamped to 1.
        Assert.AreEqual(1, 0.Clamp(10, 1, comparer));

        // A value inside the reversed range is returned unchanged.
        Assert.AreEqual(5, 5.Clamp(10, 1, comparer));
    }

    /// <summary>
    /// Verifies that the comparer overload returns <see langword="null" /> (the type default) when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenValueIsNull_ForComparerOverload_ShouldReturnNull()
    {
        IComparer<string> comparer = Comparer<string>.Default;
        string? value = null;

        Assert.IsNull(value.Clamp("a", "z", comparer));
    }

    /// <summary>
    /// Verifies that the comparer overload treats a <see langword="null" /> min or max as an unbounded side.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenBoundIsNull_ForComparerOverload_ShouldTreatBoundAsUnbounded()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        // Explicit type parameter and int? cast on 'null' are required so the compiler binds
        // to IComparableExtensions.Clamp<T>(this T? value, T? min, T? max, IComparer<T> comparer)
        // with T = int, rather than picking an unrelated Clamp overload during type inference.
        Assert.AreEqual(50, ((int?)50).Clamp<int>(1, null, comparer));
        Assert.AreEqual(-5, ((int?)(-5)).Clamp<int>(null, 10, comparer));
    }

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenComparerIsNull_ShouldThrowArgumentNullException()
    {
        IComparer<int>? comparer = null;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.Clamp(1, 10, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
