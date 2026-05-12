// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.Equality.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeTests
{
    // --------------------------------------------------------
    // Equals(Range<T>)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that ranges with identical endpoints compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEndpointsMatch_ShouldReturnTrue()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(0, 10);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(right.Equals(left));
    }

    /// <summary>
    /// Verifies that ranges with differing start endpoints compare not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStartDiffers_ShouldReturnFalse()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(1, 10);

        Assert.IsFalse(left.Equals(right));
    }

    /// <summary>
    /// Verifies that ranges with differing end endpoints compare not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEndDiffers_ShouldReturnFalse()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(0, 11);

        Assert.IsFalse(left.Equals(right));
    }

    // --------------------------------------------------------
    // Equals(object)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Range{T}.Equals(object)" /> returns <see langword="true" /> for a boxed
    /// equivalent range.
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenBoxedEquivalentRange_ShouldReturnTrue()
    {
        var left = new Range<int>(0, 10);
        object right = new Range<int>(0, 10);

        Assert.IsTrue(left.Equals(right));
    }

    /// <summary>
    /// Verifies that <see cref="Range{T}.Equals(object)" /> returns <see langword="false" /> for
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenOtherIsNull_ShouldReturnFalse()
    {
        var sut = new Range<int>(0, 10);

        Assert.IsFalse(sut.Equals((object?)null));
    }

    /// <summary>
    /// Verifies that <see cref="Range{T}.Equals(object)" /> returns <see langword="false" /> for an unrelated
    /// type.
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenOtherIsUnrelatedType_ShouldReturnFalse()
    {
        var sut = new Range<int>(0, 10);

        Assert.IsFalse(sut.Equals("not-a-range"));
    }

    /// <summary>
    /// Verifies that <see cref="Range{T}.Equals(object)" /> returns <see langword="false" /> when the boxed
    /// generic argument differs (e.g. <see cref="Range{T}" /> with different <c>T</c>).
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenGenericArgumentDiffers_ShouldReturnFalse()
    {
        var sut = new Range<int>(0, 10);
        object stringRange = new Range<string>("a", "z");

        Assert.IsFalse(sut.Equals(stringRange));
    }

    // --------------------------------------------------------
    // GetHashCode
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that two ranges with identical endpoints produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenEndpointsMatch_ShouldReturnSameValue()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(0, 10);

        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    // --------------------------------------------------------
    // operator ==, !=
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <c>operator==</c> follows <see cref="Range{T}.Equals(Range{T})" />.
    /// </summary>
    [TestMethod]
    public void OperatorEquals_WhenEndpointsMatch_ShouldReturnTrue()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(0, 10);

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
    }

    /// <summary>
    /// Verifies that <c>operator!=</c> follows <see cref="Range{T}.Equals(Range{T})" /> when endpoints differ.
    /// </summary>
    [TestMethod]
    public void OperatorNotEquals_WhenEndpointsDiffer_ShouldReturnTrue()
    {
        var left = new Range<int>(0, 10);
        var right = new Range<int>(0, 11);

        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
    }
}
