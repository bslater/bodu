// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsOutside.Extended.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="IComparableExtensions.IsOutside{T}(T, T?, T?)" /> returns the expected truth value for value inside,
    /// on-boundary, outside, reversed-boundary, and equal-boundary cases when called with integer arguments.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 10, false, DisplayName = "Value inside range returns false")]
    [DataRow(1, 1, 10, false, DisplayName = "Value on lower boundary returns false")]
    [DataRow(10, 1, 10, false, DisplayName = "Value on upper boundary returns false")]
    [DataRow(0, 1, 10, true, DisplayName = "Value below range returns true")]
    [DataRow(11, 1, 10, true, DisplayName = "Value above range returns true")]
    [DataRow(5, 10, 1, false, DisplayName = "Reversed boundaries with value inside returns false")]
    [DataRow(0, 10, 1, true, DisplayName = "Reversed boundaries with value outside returns true")]
    [DataRow(5, 5, 5, false, DisplayName = "Equal boundaries with matching value returns false")]
    [DataRow(4, 5, 5, true, DisplayName = "Equal boundaries with non-matching value returns true")]
    public void IsOutside_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(
        int value, int lower, int upper, bool expected)
    {
        Assert.AreEqual(expected, value.IsOutside(lower, upper));
    }

    /// <summary>
    /// Verifies that <see cref="IComparableExtensions.IsOutside{T}(T, T?, T?)" /> returns <see langword="false" /> when either boundary
    /// is <see langword="null" /> for reference-type values where the nullability annotation has runtime effect.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenEvaluatingStringValues_ShouldReturnExpectedResult()
    {
        Assert.IsFalse("banana".IsOutside("apple", "cherry"));
        Assert.IsFalse("apple".IsOutside("apple", "cherry"));
        Assert.IsTrue("aardvark".IsOutside("apple", "cherry"));
        Assert.IsTrue("date".IsOutside("apple", "cherry"));
        Assert.IsFalse("banana".IsOutside(null, "cherry"));
        Assert.IsFalse("banana".IsOutside("apple", null));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="IComparableExtensions.IsOutside{T}(T, T?, T?, IComparer{T})" /> returns
    /// <see langword="false" /> when either boundary is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenComparerProvidedAndEitherBoundaryIsNull_ShouldReturnFalse()
    {
        IComparer<int> comparer = Comparer<int>.Default;
        Assert.IsFalse(5.IsOutside((int?)null, (int?)10, comparer));
        Assert.IsFalse(5.IsOutside((int?)1, (int?)null, comparer));
        Assert.IsFalse(5.IsOutside((int?)null, (int?)null, comparer));
    }
}
