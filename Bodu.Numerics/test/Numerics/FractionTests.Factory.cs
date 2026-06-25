// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Factory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryCreate(T, T, out Fraction{T})" /> reports failure when the canonical
    /// numerator cannot be represented by the backing type — here, negating <see cref="int.MinValue" /> overflows.
    /// </summary>
    [TestMethod]
    public void TryCreate_WhenCanonicalNumeratorOverflowsBackingType_ShouldReturnFalse()
    {
        Assert.IsFalse(Fraction<int>.TryCreate(int.MinValue, -1, out Fraction<int> result));
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryFromBigInteger(BigInteger, BigInteger, out Fraction{T})" /> reports
    /// failure when the denominator is zero rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryFromBigInteger_WhenDenominatorIsZero_ShouldReturnFalse()
    {
        Assert.IsFalse(Fraction<int>.TryFromBigInteger(BigInteger.One, BigInteger.Zero, out Fraction<int> result));
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryFromBigInteger(BigInteger, BigInteger, out Fraction{T})" /> reports
    /// failure when the reduced numerator exceeds the range of the backing type.
    /// </summary>
    [TestMethod]
    public void TryFromBigInteger_WhenNumeratorOverflowsBackingType_ShouldReturnFalse()
    {
        Assert.IsFalse(Fraction<int>.TryFromBigInteger(BigInteger.Pow(10, 20), BigInteger.One, out Fraction<int> result));
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryFromBigInteger(BigInteger, BigInteger, out Fraction{T})" /> normalizes a
    /// negative denominator by moving the sign onto the numerator.
    /// </summary>
    [TestMethod]
    public void TryFromBigInteger_WhenDenominatorIsNegative_ShouldNormalizeSign()
    {
        Assert.IsTrue(Fraction<int>.TryFromBigInteger(BigInteger.One, BigInteger.MinusOne * 2, out Fraction<int> result));

        Assert.AreEqual(new Fraction<int>(-1, 2), result);
        Assert.IsGreaterThan(0, result.Denominator);
    }
}
