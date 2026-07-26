// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the equality operator considers values with equal components equal.
    /// </summary>
    [TestMethod]
    public void EqualityOperator_WhenComponentsMatch_ShouldReturnTrue()
    {
        Assert.IsTrue(new Complex<double>(1.0, 2.0) == new Complex<double>(1.0, 2.0));
        Assert.IsTrue(new Complex<double>(1.0, 2.0) != new Complex<double>(1.0, 3.0));
    }

    /// <summary>
    /// Verifies that the equality operator treats a NaN component as unequal to itself, while
    /// <see cref="Complex{T}.Equals(Complex{T})" /> treats it as equal, matching
    /// <see cref="System.Numerics.Complex" />.
    /// </summary>
    [TestMethod]
    public void EqualityOperatorAndEquals_WhenComponentIsNaN_ShouldDiverge()
    {
        var value = new Complex<double>(double.NaN, 1.0);

        Assert.IsFalse(value == value);
        Assert.IsTrue(value.Equals(value));
    }

    /// <summary>
    /// Verifies that the equality operator treats positive and negative zero components as equal.
    /// </summary>
    [TestMethod]
    public void EqualityOperator_WhenComponentsAreSignedZero_ShouldReturnTrue()
    {
        Assert.IsTrue(new Complex<double>(0.0, 0.0) == new Complex<double>(-0.0, -0.0));
    }

    /// <summary>
    /// Verifies that positive and negative zero produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenComponentsAreSignedZero_ShouldMatch()
    {
        Assert.AreEqual(
            new Complex<double>(0.0, 0.0).GetHashCode(),
            new Complex<double>(-0.0, -0.0).GetHashCode());
    }

    /// <summary>
    /// Verifies that equal values produce equal hash codes.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenValuesAreEqual_ShouldMatch()
    {
        Assert.AreEqual(
            new Complex<double>(2.5, -1.5).GetHashCode(),
            new Complex<double>(2.5, -1.5).GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="object.Equals(object)" /> returns <see langword="false" /> for a non-matching type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToOtherType_ShouldReturnFalse()
    {
        Assert.IsFalse(new Complex<double>(1.0, 2.0).Equals("not a complex"));
    }
}
