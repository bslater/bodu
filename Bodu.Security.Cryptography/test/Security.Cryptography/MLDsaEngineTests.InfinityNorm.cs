// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.InfinityNorm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel returns the largest magnitude of the coefficients' centered representatives over edge,
    /// patterned and seeded polynomials and the coefficients either side of (q − 1) / 2.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InfinityNorm_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheCenteredMagnitudes(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials().Concat(AsPolynomials(BoundaryCoefficients(WideGamma2))))
            AssertInfinityNormMatches(kind, polynomial);
    }

    /// <summary>
    /// Verifies that each kernel finds the largest coefficient wherever it lies: (q − 1) / 2 and (q + 1) / 2, the two
    /// coefficients of the largest centered magnitude, in each position of an otherwise small polynomial.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InfinityNorm_WhenOneCoefficientHasTheLargestMagnitude_ForEachKernel_ShouldFindItInEveryPosition(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int largest in new[] { (Q - 1) / 2, (Q + 1) / 2 })
        {
            for (int position = 0; position < MLDsaEngine.N; position++)
            {
                int[] polynomial = Enumerable.Range(0, MLDsaEngine.N).Select(i => i % 2 == 0 ? 1 : Q - 1).ToArray();
                polynomial[position] = largest;

                AssertInfinityNormMatches(kind, polynomial);
            }
        }
    }

    /// <summary>
    /// Verifies that the infinity norm without a named kernel is the one the kernel dispatch selects returns.
    /// </summary>
    [TestMethod]
    public void InfinityNorm_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
            Assert.AreEqual(MLDsaEngine.InfinityNorm(MLDsaEngine.SelectKernel(), polynomial), MLDsaEngine.InfinityNorm(polynomial));
    }

    /// <summary>
    /// Verifies that the infinity norm rejects a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, whichever kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InfinityNorm_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MLDsaEngine.InfinityNorm(kind, new int[MLDsaEngine.N - 1]);
        });

        Assert.AreEqual("poly", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's infinity norm forbids inlining and is aggressively optimized.
    /// </summary>
    [TestMethod]
    public void InfinityNorm_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.InfinityNorm));
    }

    /// <summary>
    /// Asserts that a kernel's infinity norm of a polynomial is the largest centered magnitude of its coefficients.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="polynomial">The polynomial, coefficients in [0, q).</param>
    private static void AssertInfinityNormMatches(MLDsaEngine.KernelKind kernel, int[] polynomial)
    {
        int expected = polynomial.Max(value => value > (Q - 1) / 2 ? Q - value : value);

        Assert.AreEqual(expected, MLDsaEngine.InfinityNorm(kernel, polynomial), $"{kernel}, from {polynomial[0]}");
    }
}
