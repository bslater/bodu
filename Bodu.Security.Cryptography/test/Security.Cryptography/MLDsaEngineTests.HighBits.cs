// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.HighBits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel replaces every coefficient of a polynomial by the high part the coefficient function
    /// computes, for both values of γ₂, over edge, patterned and seeded polynomials and every decomposition boundary.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void HighBits_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchEachCoefficientsHighBits(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials().Concat(AsPolynomials(BoundaryCoefficients(gamma2))))
            AssertHighBitsMatch(kind, gamma2, polynomial);
    }

    /// <summary>
    /// Verifies that each kernel computes the high part the coefficient function computes for every coefficient in
    /// [0, q), for both values of γ₂.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void HighBits_WhenEveryCoefficientIsTried_ForEachKernel_ShouldMatchEachCoefficientsHighBits(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in AsPolynomials(Enumerable.Range(0, Q)))
            AssertHighBitsMatch(kind, gamma2, polynomial);
    }

    /// <summary>
    /// Verifies that each kernel may write the high parts over the coefficients themselves.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void HighBits_WhenDestinationIsTheSource_ForEachKernel_ShouldReplaceEachCoefficient(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials().Take(20))
        {
            int[] expected = polynomial.Select(value => MLDsaEngine.HighBits(WideGamma2, value)).ToArray();
            int[] actual = (int[])polynomial.Clone();

            MLDsaEngine.HighBits(kind, WideGamma2, actual, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the high parts without a named kernel are those the kernel dispatch selects computes.
    /// </summary>
    [TestMethod]
    public void HighBits_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
        {
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaEngine.HighBits(MLDsaEngine.SelectKernel(), NarrowGamma2, polynomial, expected);
            MLDsaEngine.HighBits(NarrowGamma2, polynomial, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the high parts of a polynomial reject a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, whichever kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0 or 1.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "r")]
    [DataRow("Auto", 1, "r1")]
    [DataRow("Scalar", 0, "r")]
    [DataRow("Scalar", 1, "r1")]
    [DataRow("Avx2", 0, "r")]
    [DataRow("Avx2", 1, "r1")]
    public void HighBits_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 2).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.HighBits(kind, WideGamma2, spans[0], spans[1]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's high parts forbid inlining and are aggressively optimized.
    /// </summary>
    [TestMethod]
    public void HighBits_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.HighBits));
    }

    /// <summary>
    /// Asserts that a kernel's high parts of a polynomial are those the coefficient function computes.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="polynomial">The polynomial, coefficients in [0, q).</param>
    private static void AssertHighBitsMatch(MLDsaEngine.KernelKind kernel, int gamma2, int[] polynomial)
    {
        int[] expected = polynomial.Select(value => MLDsaEngine.HighBits(gamma2, value)).ToArray();
        int[] actual = new int[MLDsaEngine.N];

        MLDsaEngine.HighBits(kernel, gamma2, polynomial, actual);

        CollectionAssert.AreEqual(expected, actual, $"{kernel}, γ₂ = {gamma2}, from {polynomial[0]}");
    }
}
