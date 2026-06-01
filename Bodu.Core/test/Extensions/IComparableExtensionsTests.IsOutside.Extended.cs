// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsOutside.Extended.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that the comparer overload of <see cref="IComparableExtensions.IsOutside{T}(T, T?, T?, IComparer{T})" /> matches
    /// <c>IsBetween</c>'s complement under the default comparer for inside, on-boundary, outside, and reversed-boundary cases.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 10, false, DisplayName = "Default comparer: value inside range returns false")]
    [DataRow(1, 1, 10, false, DisplayName = "Default comparer: value on lower boundary returns false")]
    [DataRow(10, 1, 10, false, DisplayName = "Default comparer: value on upper boundary returns false")]
    [DataRow(0, 1, 10, true, DisplayName = "Default comparer: value below range returns true")]
    [DataRow(11, 1, 10, true, DisplayName = "Default comparer: value above range returns true")]
    [DataRow(5, 10, 1, false, DisplayName = "Default comparer: reversed boundaries with value inside returns false")]
    [DataRow(0, 10, 1, true, DisplayName = "Default comparer: reversed boundaries with value outside returns true")]
    [DataRow(11, 10, 1, true, DisplayName = "Default comparer: reversed boundaries with value above outside returns true")]
    [DataRow(5, 5, 5, false, DisplayName = "Default comparer: equal boundaries with matching value returns false")]
    [DataRow(4, 5, 5, true, DisplayName = "Default comparer: equal boundaries with non-matching value returns true")]
    public void IsOutside_WhenComparerProvided_ShouldReturnExpectedResult(
        int value, int lower, int upper, bool expected)
    {
        IComparer<int> comparer = Comparer<int>.Default;

        Assert.AreEqual(expected, value.IsOutside((int?)lower, (int?)upper, comparer));
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
        int value, int lower, int upper, bool expected) => Assert.AreEqual(expected, value.IsOutside(lower, upper));

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
    /// Verifies that the comparer overload honours a custom comparer whose ordering is reversed from the natural ordering — items
    /// that are "outside" the natural range are "inside" the reversed range, and vice versa.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenUsingReverseComparer_ShouldHonorComparerOrdering()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        // With reverse ordering, 10 is the "lower" boundary and 1 is the "upper" boundary, so:
        //  - 5 is inside,
        //  - 0 (which is "greater" under reverse ordering) is outside,
        //  - 11 (which is "less" under reverse ordering) is outside.
        Assert.IsFalse(5.IsOutside((int?)1, (int?)10, comparer), "5 should be inside the reverse-ordered range.");
        Assert.IsFalse(1.IsOutside((int?)1, (int?)10, comparer), "1 is on the boundary and must not be reported as outside.");
        Assert.IsFalse(10.IsOutside((int?)1, (int?)10, comparer), "10 is on the boundary and must not be reported as outside.");
        Assert.IsTrue(0.IsOutside((int?)1, (int?)10, comparer), "0 falls outside the reverse-ordered range.");
        Assert.IsTrue(11.IsOutside((int?)1, (int?)10, comparer), "11 falls outside the reverse-ordered range.");
    }

}
