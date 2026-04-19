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
    /// Verifies that <c>Clamp</c> restricts a nullable integer value to the inclusive [min, max] range,
    /// honouring unbounded (<see langword="null" />) bounds and returning <see langword="null" /> when the value is null.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10, 1, DisplayName = "Value below min returns min")]
    [DataRow(20, 1, 10, 10, DisplayName = "Value above max returns max")]
    [DataRow(5, 1, 10, 5, DisplayName = "Value within range returns value")]
    [DataRow(1, 1, 10, 1, DisplayName = "Value on lower boundary returns value")]
    [DataRow(10, 1, 10, 10, DisplayName = "Value on upper boundary returns value")]
    [DataRow(-5, null, 10, -5, DisplayName = "Null min with value below max returns value")]
    [DataRow(50, null, 10, 10, DisplayName = "Null min with value above max returns max")]
    [DataRow(50, 1, null, 50, DisplayName = "Null max with value above min returns value")]
    [DataRow(-5, 1, null, 1, DisplayName = "Null max with value below min returns min")]
    [DataRow(5, null, null, 5, DisplayName = "Both bounds null returns value unchanged")]
    public void Clamp_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
        int value,
        int? min,
        int? max,
        int? expected)
    {
        Assert.AreEqual(expected, value.Clamp(min, max));
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
    /// Verifies that the comparer overload treats a <see langword="null" /> min or max as an unbounded side.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenBoundIsNull_ForComparerOverload_ShouldTreatBoundAsUnbounded()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        Assert.AreEqual(50, 50.Clamp(1, null, comparer));
        Assert.AreEqual(-5, (-5).Clamp(null, 10, comparer));
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
