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
}
