// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.DefaultValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that a default-initialized fraction exposes the canonical components of zero.
    /// </summary>
    [TestMethod]
    public void Default_WhenInspected_ShouldExposeCanonicalZeroComponents()
    {
        Fraction<int> value = default;

        Assert.AreEqual(0, value.Numerator);
        Assert.AreEqual(1, value.Denominator);
        Assert.AreEqual(0, value.Sign);
        Assert.IsTrue(value.IsZero);
        Assert.IsTrue(value.IsInteger);
    }

    /// <summary>
    /// Verifies that a default-initialized fraction behaves as zero through arithmetic operators.
    /// </summary>
    [TestMethod]
    public void Default_WhenUsedInArithmetic_ShouldBehaveAsZero()
    {
        var half = new Fraction<int>(1, 2);

        Assert.AreEqual(half, default(Fraction<int>) + half);
        Assert.AreEqual(-half, default(Fraction<int>) - half);
        Assert.AreEqual(Fraction<int>.Zero, default(Fraction<int>) * half);
    }

    /// <summary>
    /// Verifies that a default-initialized fraction formats, hashes, and compares as zero.
    /// </summary>
    [TestMethod]
    public void Default_WhenFormattedHashedOrCompared_ShouldMatchZero()
    {
        Fraction<int> value = default;

        Assert.AreEqual("0", value.ToString());
        Assert.AreEqual(Fraction<int>.Zero.GetHashCode(), value.GetHashCode());
        Assert.AreEqual(0, value.CompareTo(Fraction<int>.Zero));
        Assert.IsTrue(value == Fraction<int>.Zero);
    }

    /// <summary>
    /// Verifies that expanding a default-initialized fraction yields the continued fraction of zero.
    /// </summary>
    [TestMethod]
    public void Default_WhenExpandedToContinuedFraction_ShouldYieldZero() => CollectionAssert.AreEqual(new[] { 0 }, default(Fraction<int>).ToContinuedFraction());

    /// <summary>
    /// Verifies that taking the reciprocal of a default-initialized fraction throws
    /// <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Default_WhenReciprocalRequested_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = default(Fraction<int>).Reciprocal();
        });
    }
}
