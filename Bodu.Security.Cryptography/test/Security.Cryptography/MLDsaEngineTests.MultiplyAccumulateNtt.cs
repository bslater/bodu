// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MultiplyAccumulateNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel, accumulating products without reducing them, then reducing the sums with Reduce32
    /// and applying the inverse transform, gives the reference's inverse transform of the reduced sum of products, for
    /// as many terms as the widest matrix row holds.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyAccumulateNtt_WhenFollowedByInvNtt_ForEachKernel_ShouldMatchTheReferenceSumOfProducts(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] polynomials = Polynomials().Skip(5).Take(16).ToArray();
        int[] accumulator = new int[MLDsaEngine.N];
        int[] expected = new int[MLDsaEngine.N];
        int[] product = new int[MLDsaEngine.N];

        for (int s = 0; s < 8; s++)
        {
            int[] left = polynomials[2 * s];
            int[] right = polynomials[(2 * s) + 1];

            MLDsaEngine.MultiplyAccumulateNtt(kind, left.Select(MLDsaEngine.ToMontgomery).ToArray(), right, accumulator);
            MLDsaReference.MultiplyNtt(left, right, product);
            for (int j = 0; j < MLDsaEngine.N; j++)
                expected[j] = (expected[j] + product[j]) % Q;
        }

        for (int j = 0; j < MLDsaEngine.N; j++)
            accumulator[j] = MLDsaEngine.Reduce32(accumulator[j]);

        MLDsaReference.InvNtt(expected);
        MLDsaEngine.InvNtt(kind, accumulator);

        CollectionAssert.AreEqual(expected, accumulator);
    }

    /// <summary>
    /// Verifies that each kernel, adding eight products of operands up to q − 1 in magnitude, either sign, into an
    /// accumulator that starts at seeded values, leaves the accumulator exactly as the scalar kernel does.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyAccumulateNtt_WhenOperandsAreSigned_ForEachKernel_ShouldMatchTheScalarKernel(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] polynomials = SignedPolynomials().ToArray();
        var random = new Random(0x0204_0007);

        for (int trial = 0; trial < 20; trial++)
        {
            int[] expected = Enumerable.Range(0, MLDsaEngine.N).Select(_ => random.Next(-8 * Q, 8 * Q)).ToArray();
            int[] actual = (int[])expected.Clone();

            for (int term = 0; term < 8; term++)
            {
                int[] left = polynomials[((trial * 8) + term) % polynomials.Length];
                int[] right = polynomials[((trial * 13) + (term * 5) + 1) % polynomials.Length];

                MLDsaEngine.MultiplyAccumulateNtt(MLDsaEngine.KernelKind.Scalar, left, right, expected);
                MLDsaEngine.MultiplyAccumulateNtt(kind, left, right, actual);
            }

            CollectionAssert.AreEqual(expected, actual, $"trial {trial}");
        }
    }

    /// <summary>
    /// Verifies that the accumulating product without a named kernel accumulates as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void MultiplyAccumulateNtt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        int[][] polynomials = SignedPolynomials().Take(16).ToArray();
        int[] expected = new int[MLDsaEngine.N];
        int[] actual = new int[MLDsaEngine.N];

        for (int s = 0; s < 8; s++)
        {
            MLDsaEngine.MultiplyAccumulateNtt(MLDsaEngine.SelectKernel(), polynomials[2 * s], polynomials[(2 * s) + 1], expected);
            MLDsaEngine.MultiplyAccumulateNtt(polynomials[2 * s], polynomials[(2 * s) + 1], actual);
        }

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the accumulating product rejects an operand or accumulator shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end, whichever
    /// kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "montgomeryLeft")]
    [DataRow("Auto", 1, "right")]
    [DataRow("Auto", 2, "accumulator")]
    [DataRow("Scalar", 0, "montgomeryLeft")]
    [DataRow("Scalar", 1, "right")]
    [DataRow("Scalar", 2, "accumulator")]
    [DataRow("Avx2", 0, "montgomeryLeft")]
    [DataRow("Avx2", 1, "right")]
    [DataRow("Avx2", 2, "accumulator")]
    public void MultiplyAccumulateNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.MultiplyAccumulateNtt(kind, spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's accumulating product forbids inlining and is aggressively optimized, so it is
    /// compiled on its own rather than into the dispatcher.
    /// </summary>
    [TestMethod]
    public void MultiplyAccumulateNtt_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.MultiplyAccumulateNtt));
    }
}
