// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MultiplyNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel's coefficient-wise product of a polynomial in Montgomery form and a plain one is the
    /// plain product, matching the remainder-operator reference modulo q on edge and seeded polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyNtt_WhenLeftIsInMontgomeryForm_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] polynomials = Polynomials().ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i * 7 + 1) % polynomials.Length];
            int[] montgomeryLeft = left.Select(MLDsaEngine.ToMontgomery).ToArray();
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaReference.MultiplyNtt(left, right, expected);
            MLDsaEngine.MultiplyNtt(kind, montgomeryLeft, right, actual);

            CollectionAssert.AreEqual(expected, actual.Select(value => Mod(value)).ToArray(), $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that each kernel's coefficient-wise product of operands up to q − 1 in magnitude, either sign, is the
    /// scalar kernel's to the bit: the product is exact but not canonical, so a kernel must return the same
    /// representative, not merely a congruent one.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyNtt_WhenOperandsAreSigned_ForEachKernel_ShouldMatchTheScalarKernel(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] polynomials = SignedPolynomials().ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i * 7 + 1) % polynomials.Length];
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaEngine.MultiplyNtt(MLDsaEngine.KernelKind.Scalar, left, right, expected);
            MLDsaEngine.MultiplyNtt(kind, left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that the coefficient-wise product without a named kernel multiplies as the kernel dispatch selects
    /// does.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        int[][] polynomials = SignedPolynomials().Take(20).ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i + 1) % polynomials.Length];
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaEngine.MultiplyNtt(MLDsaEngine.SelectKernel(), left, right, expected);
            MLDsaEngine.MultiplyNtt(left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that the coefficient-wise product rejects an operand or destination shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end, whichever
    /// kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "montgomeryLeft")]
    [DataRow("Auto", 1, "right")]
    [DataRow("Auto", 2, "destination")]
    [DataRow("Scalar", 0, "montgomeryLeft")]
    [DataRow("Scalar", 1, "right")]
    [DataRow("Scalar", 2, "destination")]
    [DataRow("Avx2", 0, "montgomeryLeft")]
    [DataRow("Avx2", 1, "right")]
    [DataRow("Avx2", 2, "destination")]
    public void MultiplyNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.MultiplyNtt(kind, spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's coefficient-wise product forbids inlining and is aggressively optimized, so it
    /// is compiled on its own rather than into the dispatcher.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.MultiplyNtt));
    }
}
