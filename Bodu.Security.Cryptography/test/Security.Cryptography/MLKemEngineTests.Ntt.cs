// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that the forward transform matches the remainder-operator reference on edge and seeded polynomials.
    /// </summary>
    [TestMethod]
    public void Ntt_WhenCoefficientsAreInRange_ShouldMatchTheReference()
    {
        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemReference.Ntt(expected);
            MLKemEngine.Ntt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the inverse transform matches the remainder-operator reference on edge and seeded polynomials.
    /// </summary>
    [TestMethod]
    public void InvNtt_WhenCoefficientsAreInRange_ShouldMatchTheReference()
    {
        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemReference.InvNtt(expected);
            MLKemEngine.InvNtt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the inverse transform undoes the forward transform.
    /// </summary>
    [TestMethod]
    public void InvNtt_WhenAppliedAfterNtt_ShouldRestoreThePolynomial()
    {
        foreach (int[] polynomial in Polynomials())
        {
            int[] roundTrip = (int[])polynomial.Clone();

            MLKemEngine.Ntt(roundTrip);
            MLKemEngine.InvNtt(roundTrip);

            CollectionAssert.AreEqual(polynomial, roundTrip);
        }
    }

    /// <summary>
    /// Verifies that the transforms reject a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading past its end.
    /// </summary>
    [TestMethod]
    public void Ntt_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException()
    {
        var forward = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.Ntt(new int[MLKemEngine.N - 1]);
        });
        var inverse = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.InvNtt(new int[MLKemEngine.N - 1]);
        });

        Assert.AreEqual("f", forward.ParamName);
        Assert.AreEqual("f", inverse.ParamName);
    }
}
