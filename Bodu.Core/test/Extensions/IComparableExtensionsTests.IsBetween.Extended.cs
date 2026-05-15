// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsBetween.Extended.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that the comparer overload of <see cref="IComparableExtensions.IsBetween{T}(T, T?, T?, IComparer{T})" /> returns
    /// <see langword="true" /> when the value lies within the inclusive range under the default comparer.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 10, true)]
    [DataRow(1, 1, 10, true)]
    [DataRow(10, 1, 10, true)]
    [DataRow(0, 1, 10, false)]
    [DataRow(11, 1, 10, false)]
    [DataRow(5, 10, 1, true)]
    public void IsBetween_WhenComparerIsDefault_ShouldHonourOrdering(int value, int lower, int upper, bool expected)
    {
        IComparer<int> comparer = Comparer<int>.Default;
        Assert.AreEqual(expected, value.IsBetween((int?)lower, (int?)upper, comparer));
    }

    /// <summary>
    /// Verifies that the comparer overload of <see cref="IComparableExtensions.IsBetween{T}(T, T?, T?, IComparer{T})" /> returns
    /// <see langword="false" /> when either boundary is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenComparerProvidedAndEitherBoundaryIsNull_ShouldReturnFalse()
    {
        IComparer<int> comparer = Comparer<int>.Default;
        Assert.IsFalse(5.IsBetween((int?)null, (int?)10, comparer));
        Assert.IsFalse(5.IsBetween((int?)1, (int?)null, comparer));
        Assert.IsFalse(5.IsBetween((int?)null, (int?)null, comparer));
    }

    /// <summary>
    /// Verifies that <see cref="IComparableExtensions.IsBetween{T}(T, T?, T?)" /> returns <see langword="false" /> when either boundary
    /// is <see langword="null" /> for reference-type values where the nullability annotation has runtime effect.
    /// </summary>
    [TestMethod]
    public void IsBetween_WhenEitherStringBoundaryIsNull_ShouldReturnFalse()
    {
        Assert.IsFalse("banana".IsBetween(null, "cherry"));
        Assert.IsFalse("banana".IsBetween("apple", null));
        Assert.IsFalse("banana".IsBetween(null, null));
    }
    /// <summary>
    /// Verifies that <see cref="IComparableExtensions.IsBetween{T}(T, T?, T?)" /> returns the expected truth value for value inside,
    /// on-boundary, outside, reversed-boundary, and equal-boundary cases when called with integer arguments.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 10, true, DisplayName = "Value within range returns true")]
    [DataRow(1, 1, 10, true, DisplayName = "Value on lower boundary returns true")]
    [DataRow(10, 1, 10, true, DisplayName = "Value on upper boundary returns true")]
    [DataRow(0, 1, 10, false, DisplayName = "Value below range returns false")]
    [DataRow(11, 1, 10, false, DisplayName = "Value above range returns false")]
    [DataRow(5, 10, 1, true, DisplayName = "Reversed boundaries with value inside returns true")]
    [DataRow(10, 10, 1, true, DisplayName = "Reversed boundaries with value on boundary returns true")]
    [DataRow(5, 5, 5, true, DisplayName = "Equal boundaries with matching value returns true")]
    [DataRow(4, 5, 5, false, DisplayName = "Equal boundaries with non-matching value returns false")]
    public void IsBetween_WhenEvaluatingIntegerValues_ShouldReturnExpectedResult(
        int value, int lower, int upper, bool expected)
    {
        Assert.AreEqual(expected, value.IsBetween(lower, upper));
    }

}
