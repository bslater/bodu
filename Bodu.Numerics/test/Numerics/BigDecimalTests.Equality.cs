// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that numerically equal values with different trailing zeros compare equal and share a hash code — the
    /// canonical-form contract that makes <c>1.0 == 1.00</c>.
    /// </summary>
    [TestMethod]
    public void Equals_WhenValuesDifferOnlyInTrailingZeros_ShouldBeEqual()
    {
        BigDecimal a = BD(10, 1);    // 1.0
        BigDecimal b = BD(100, 2);   // 1.00
        BigDecimal c = BD(1, 0);     // 1

        Assert.AreEqual(a, b);
        Assert.AreEqual(a, c);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreEqual(a.GetHashCode(), c.GetHashCode());
    }

    /// <summary>
    /// Verifies that parsing texts that differ only in trailing zeros yields equal values in canonical form, so the
    /// canonicalization invariant holds across the parse construction path as well as the constructor path.
    /// </summary>
    [TestMethod]
    public void Equals_WhenParsedValuesDifferOnlyInTrailingZeros_ShouldBeEqual()
    {
        BigDecimal parsed = BigDecimal.Parse("1.00");
        BigDecimal plain = BigDecimal.Parse("1");

        Assert.AreEqual(plain, parsed);
        Assert.AreEqual(plain.GetHashCode(), parsed.GetHashCode());
        Assert.AreEqual(0, parsed.Scale);
        Assert.AreEqual(System.Numerics.BigInteger.One, parsed.UnscaledValue);
    }

    /// <summary>
    /// Verifies that an integer keeps its digits (scale zero) and is distinct from a same-digit fractional value.
    /// </summary>
    [TestMethod]
    public void Equals_WhenValuesDiffer_ShouldNotBeEqual()
    {
        Assert.AreNotEqual(BD(100, 0), BD(1, 0));    // 100 != 1
        Assert.AreNotEqual(BD(1, 1), BD(1, 0));      // 0.1 != 1
        Assert.IsTrue(BD(100, 0) != BD(10, 1));      // 100 != 1.0
    }

    /// <summary>
    /// Verifies that the default value equals zero.
    /// </summary>
    [TestMethod]
    public void DefaultValue_ShouldEqualZero()
    {
        BigDecimal value = default;

        Assert.AreEqual(BigDecimal.Zero, value);
        Assert.IsTrue(value.IsZero);
        Assert.AreEqual("0", value.ToString());
    }
}
