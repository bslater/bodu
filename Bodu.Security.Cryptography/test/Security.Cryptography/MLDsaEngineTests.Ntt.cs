// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
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

            MLDsaReference.Ntt(expected);
            MLDsaEngine.Ntt(actual);

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

            MLDsaReference.InvNtt(expected);
            MLDsaEngine.InvNtt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the inverse transform accepts coefficients up to q − 1 in magnitude, the ends of the range it
    /// documents, including the patterns that drive the last layer's sum and difference to 256(q − 1), and transforms
    /// their residues.
    /// </summary>
    [TestMethod]
    public void InvNtt_WhenCoefficientsReachQInMagnitude_ShouldTransformTheirResidues()
    {
        var random = new Random(0x0204_0005);

        for (int iteration = 0; iteration < 64; iteration++)
        {
            int[] coefficients = Enumerable.Range(0, MLDsaEngine.N)
                .Select(i => iteration switch
                {
                    0 => Q - 1,
                    1 => -(Q - 1),
                    2 => i % 2 == 0 ? Q - 1 : -(Q - 1),
                    3 => i < MLDsaEngine.N / 2 ? Q - 1 : -(Q - 1),
                    _ => random.Next(-Q + 1, Q),
                })
                .ToArray();
            int[] expected = coefficients.Select(value => Mod(value)).ToArray();

            MLDsaReference.InvNtt(expected);
            MLDsaEngine.InvNtt(coefficients);

            CollectionAssert.AreEqual(expected, coefficients, $"iteration {iteration}");
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

            MLDsaEngine.Ntt(roundTrip);
            MLDsaEngine.InvNtt(roundTrip);

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
            MLDsaEngine.Ntt(new int[MLDsaEngine.N - 1]);
        });
        var inverse = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.InvNtt(new int[MLDsaEngine.N - 1]);
        });

        Assert.AreEqual("w", forward.ParamName);
        Assert.AreEqual("w", inverse.ParamName);
    }
}
