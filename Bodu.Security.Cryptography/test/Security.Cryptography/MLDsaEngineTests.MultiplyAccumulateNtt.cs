// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MultiplyAccumulateNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that accumulating products without reducing them, then applying the inverse transform, gives the
    /// reference's inverse transform of the reduced sum of products, for as many terms as the widest matrix row holds.
    /// </summary>
    [TestMethod]
    public void MultiplyAccumulateNtt_WhenFollowedByInvNtt_ShouldMatchTheReferenceSumOfProducts()
    {
        int[][] polynomials = Polynomials().Skip(5).Take(16).ToArray();
        int[] accumulator = new int[MLDsaEngine.N];
        int[] expected = new int[MLDsaEngine.N];
        int[] product = new int[MLDsaEngine.N];

        for (int s = 0; s < 8; s++)
        {
            int[] left = polynomials[2 * s];
            int[] right = polynomials[(2 * s) + 1];

            MLDsaEngine.MultiplyAccumulateNtt(left.Select(MLDsaEngine.ToMontgomery).ToArray(), right, accumulator);
            MLDsaReference.MultiplyNtt(left, right, product);
            for (int j = 0; j < MLDsaEngine.N; j++)
                expected[j] = (expected[j] + product[j]) % Q;
        }

        MLDsaReference.InvNtt(expected);
        MLDsaEngine.InvNtt(accumulator);

        CollectionAssert.AreEqual(expected, accumulator);
    }

    /// <summary>
    /// Verifies that the accumulating product rejects an operand or accumulator shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end.
    /// </summary>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow(0, "montgomeryLeft")]
    [DataRow(1, "right")]
    [DataRow(2, "accumulator")]
    public void MultiplyAccumulateNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(int shortSpan, string expectedParamName)
    {
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.MultiplyAccumulateNtt(spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }
}
