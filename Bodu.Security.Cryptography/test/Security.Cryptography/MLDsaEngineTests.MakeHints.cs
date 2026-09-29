// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MakeHints.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel computes, for every coefficient, the hint the coefficient function gives for
    /// MakeHint(−ct₀, w − cs₂ + ct₀), and counts them, for both values of γ₂ over edge, patterned and seeded pairs
    /// of polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void MakeHints_WhenPolynomialsAreInRange_ForEachKernel_ShouldMatchEachCoefficientsHint(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach ((int[] ct0, int[] wMinusCs2) in PolynomialPairs())
            AssertHintsMatch(kind, gamma2, ct0, wMinusCs2);
    }

    /// <summary>
    /// Verifies that each kernel computes the hints the coefficient function gives where they are made in signing:
    /// ct₀ small, below γ₂ in magnitude, and w − cs₂ around every decomposition boundary, so that many hints are 1.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void MakeHints_WhenCt0IsSmallAndWIsAtBoundaries_ForEachKernel_ShouldMatchEachCoefficientsHint(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x0204_000B + gamma2);

        foreach (int[] wMinusCs2 in AsPolynomials(BoundaryCoefficients(gamma2)))
        {
            int[] ct0 = Enumerable.Range(0, MLDsaEngine.N).Select(_ => Mod(random.Next(-gamma2 + 1, gamma2))).ToArray();
            AssertHintsMatch(kind, gamma2, ct0, wMinusCs2);
        }
    }

    /// <summary>
    /// Verifies that the hints without a named kernel are those the kernel dispatch selects computes.
    /// </summary>
    [TestMethod]
    public void MakeHints_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach ((int[] ct0, int[] wMinusCs2) in PolynomialPairs().Take(20))
        {
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            int expectedWeight = MLDsaEngine.MakeHints(MLDsaEngine.SelectKernel(), NarrowGamma2, ct0, wMinusCs2, expected);
            int actualWeight = MLDsaEngine.MakeHints(NarrowGamma2, ct0, wMinusCs2, actual);

            CollectionAssert.AreEqual(expected, actual);
            Assert.AreEqual(expectedWeight, actualWeight);
        }
    }

    /// <summary>
    /// Verifies that the hints reject a span shorter than a polynomial with <see cref="ArgumentOutOfRangeException" />
    /// naming it, whichever kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "ct0")]
    [DataRow("Auto", 1, "wMinusCs2")]
    [DataRow("Auto", 2, "hints")]
    [DataRow("Scalar", 0, "ct0")]
    [DataRow("Scalar", 1, "wMinusCs2")]
    [DataRow("Scalar", 2, "hints")]
    [DataRow("Avx2", 0, "ct0")]
    [DataRow("Avx2", 1, "wMinusCs2")]
    [DataRow("Avx2", 2, "hints")]
    public void MakeHints_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MLDsaEngine.MakeHints(kind, WideGamma2, spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's hints forbid inlining and are aggressively optimized.
    /// </summary>
    [TestMethod]
    public void MakeHints_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.MakeHints));
    }

    /// <summary>
    /// Asserts that a kernel's hints for a pair of polynomials, and their number, are those the coefficient function
    /// gives.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="ct0">The polynomial ct₀, coefficients in [0, q).</param>
    /// <param name="wMinusCs2">The polynomial w − cs₂, coefficients in [0, q).</param>
    private static void AssertHintsMatch(MLDsaEngine.KernelKind kernel, int gamma2, int[] ct0, int[] wMinusCs2)
    {
        int[] expected = Enumerable.Range(0, MLDsaEngine.N)
            .Select(i => MLDsaEngine.MakeHint(gamma2, Mod(-ct0[i]), Mod((long)wMinusCs2[i] + ct0[i])))
            .ToArray();
        int[] actual = new int[MLDsaEngine.N];

        int weight = MLDsaEngine.MakeHints(kernel, gamma2, ct0, wMinusCs2, actual);

        CollectionAssert.AreEqual(expected, actual, $"{kernel}, γ₂ = {gamma2}, from {ct0[0]} and {wMinusCs2[0]}");
        Assert.AreEqual(expected.Sum(), weight, $"{kernel}, γ₂ = {gamma2}, from {ct0[0]} and {wMinusCs2[0]}");
    }
}
