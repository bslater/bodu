// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.MultiplyNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that the base-case products match the remainder-operator reference on edge and seeded polynomials,
    /// including every coefficient at q − 1, where the products and their sums are largest.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenCoefficientsAreInRange_ShouldMatchTheReference()
    {
        int[][] polynomials = Polynomials().ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i * 7 + 1) % polynomials.Length];
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemReference.MultiplyNtt(left, right, expected);
            MLKemEngine.MultiplyNtt(left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that the base-case products stay exact when the left factor's coefficients reach 4095, the largest
    /// value 12-bit decoding yields: FIPS 203's decapsulation-key check does not bound the packed secret vector by q.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenLeftCoefficientsReachTheTwelveBitMaximum_ShouldMatchTheReference()
    {
        int[] left = Enumerable.Repeat(4095, MLKemEngine.N).ToArray();

        foreach (int[] right in Polynomials().Take(20))
        {
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemReference.MultiplyNtt(left, right, expected);
            MLKemEngine.MultiplyNtt(left, right, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the NTT-domain product rejects an operand or destination shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end.
    /// </summary>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow(0, "left")]
    [DataRow(1, "right")]
    [DataRow(2, "destination")]
    public void MultiplyNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(int shortSpan, string expectedParamName)
    {
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLKemEngine.N - 1 : MLKemEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.MultiplyNtt(spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }
}
