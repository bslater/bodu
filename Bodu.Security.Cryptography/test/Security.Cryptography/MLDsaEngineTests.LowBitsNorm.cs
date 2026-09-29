// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.LowBitsNorm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel returns the largest magnitude of the low parts the decomposition computes, for both
    /// values of γ₂, over edge, patterned and seeded polynomials and every decomposition boundary.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void LowBitsNorm_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheDecomposition(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials().Concat(AsPolynomials(BoundaryCoefficients(gamma2))))
            AssertLowBitsNormMatches(kind, gamma2, polynomial);
    }

    /// <summary>
    /// Verifies that each kernel finds the largest low part wherever it lies: a single coefficient of the largest low
    /// part, −γ₂ + 1 or γ₂, in each position of an otherwise zero polynomial.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void LowBitsNorm_WhenOneCoefficientHoldsTheLargestLowPart_ForEachKernel_ShouldFindItInEveryPosition(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int largest in new[] { gamma2, (2 * gamma2) + gamma2 + 1 })
        {
            for (int position = 0; position < MLDsaEngine.N; position++)
            {
                int[] polynomial = new int[MLDsaEngine.N];
                polynomial[position] = largest;

                AssertLowBitsNormMatches(kind, gamma2, polynomial);
            }
        }
    }

    /// <summary>
    /// Verifies that each kernel returns the largest magnitude of the low parts over every coefficient in [0, q), for
    /// both values of γ₂.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Scalar", WideGamma2)]
    [DataRow("Scalar", NarrowGamma2)]
    [DataRow("Avx2", WideGamma2)]
    [DataRow("Avx2", NarrowGamma2)]
    public void LowBitsNorm_WhenEveryCoefficientIsTried_ForEachKernel_ShouldMatchTheDecomposition(string kernel, int gamma2)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in AsPolynomials(Enumerable.Range(0, Q)))
            AssertLowBitsNormMatches(kind, gamma2, polynomial);
    }

    /// <summary>
    /// Verifies that the low-parts norm without a named kernel is the one the kernel dispatch selects returns.
    /// </summary>
    [TestMethod]
    public void LowBitsNorm_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
            Assert.AreEqual(MLDsaEngine.LowBitsNorm(MLDsaEngine.SelectKernel(), WideGamma2, polynomial), MLDsaEngine.LowBitsNorm(WideGamma2, polynomial));
    }

    /// <summary>
    /// Verifies that the low-parts norm rejects a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, whichever kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void LowBitsNorm_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MLDsaEngine.LowBitsNorm(kind, WideGamma2, new int[MLDsaEngine.N - 1]);
        });

        Assert.AreEqual("r", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's low-parts norm forbids inlining and is aggressively optimized.
    /// </summary>
    [TestMethod]
    public void LowBitsNorm_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.LowBitsNorm));
    }

    /// <summary>
    /// Asserts that a kernel's low-parts norm of a polynomial is the largest |r₀| the decomposition gives.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="polynomial">The polynomial, coefficients in [0, q).</param>
    private static void AssertLowBitsNormMatches(MLDsaEngine.KernelKind kernel, int gamma2, int[] polynomial)
    {
        int expected = polynomial.Max(value =>
        {
            MLDsaEngine.Decompose(gamma2, value, out _, out int low);
            return Math.Abs(low);
        });

        Assert.AreEqual(expected, MLDsaEngine.LowBitsNorm(kernel, gamma2, polynomial), $"{kernel}, γ₂ = {gamma2}, from {polynomial[0]}");
    }
}
