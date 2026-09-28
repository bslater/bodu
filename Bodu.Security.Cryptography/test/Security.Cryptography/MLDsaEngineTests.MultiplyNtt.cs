// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MultiplyNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the coefficient-wise product of a polynomial in Montgomery form and a plain one is the plain
    /// product, matching the remainder-operator reference modulo q on edge and seeded polynomials.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenLeftIsInMontgomeryForm_ShouldMatchTheReference()
    {
        int[][] polynomials = Polynomials().ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i * 7 + 1) % polynomials.Length];
            int[] montgomeryLeft = left.Select(MLDsaEngine.ToMontgomery).ToArray();
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaReference.MultiplyNtt(left, right, expected);
            MLDsaEngine.MultiplyNtt(montgomeryLeft, right, actual);

            CollectionAssert.AreEqual(expected, actual.Select(value => Mod(value)).ToArray(), $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that the coefficient-wise product rejects an operand or destination shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end.
    /// </summary>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow(0, "montgomeryLeft")]
    [DataRow(1, "right")]
    [DataRow(2, "destination")]
    public void MultiplyNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(int shortSpan, string expectedParamName)
    {
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.MultiplyNtt(spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }
}
