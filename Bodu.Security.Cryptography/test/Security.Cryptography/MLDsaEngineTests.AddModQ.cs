// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.AddModQ.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel adds two polynomials coefficient-wise modulo q over edge, patterned and seeded pairs,
    /// returning each result in [0, q).
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void AddModQ_WhenCoefficientsAreInRange_ForEachKernel_ShouldReturnTheCanonicalSum(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach ((int[] left, int[] right) in PolynomialPairs())
        {
            int[] expected = Enumerable.Range(0, MLDsaEngine.N).Select(i => Mod((long)left[i] + right[i])).ToArray();
            int[] actual = new int[MLDsaEngine.N];

            MLDsaEngine.AddModQ(kind, left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"{kernel}, from {left[0]} and {right[0]}");
        }
    }

    /// <summary>
    /// Verifies that each kernel may write the sum over either operand.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void AddModQ_WhenDestinationIsAnOperand_ForEachKernel_ShouldReturnTheCanonicalSum(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach ((int[] left, int[] right) in PolynomialPairs().Take(20))
        {
            int[] expected = Enumerable.Range(0, MLDsaEngine.N).Select(i => Mod((long)left[i] + right[i])).ToArray();
            int[] intoLeft = (int[])left.Clone();
            int[] intoRight = (int[])right.Clone();

            MLDsaEngine.AddModQ(kind, intoLeft, right, intoLeft);
            MLDsaEngine.AddModQ(kind, left, intoRight, intoRight);

            CollectionAssert.AreEqual(expected, intoLeft);
            CollectionAssert.AreEqual(expected, intoRight);
        }
    }

    /// <summary>
    /// Verifies that the sum without a named kernel is the one the kernel dispatch selects computes.
    /// </summary>
    [TestMethod]
    public void AddModQ_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach ((int[] left, int[] right) in PolynomialPairs().Take(20))
        {
            int[] expected = new int[MLDsaEngine.N];
            int[] actual = new int[MLDsaEngine.N];

            MLDsaEngine.AddModQ(MLDsaEngine.SelectKernel(), left, right, expected);
            MLDsaEngine.AddModQ(left, right, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that the sum rejects a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, whichever kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "left")]
    [DataRow("Auto", 1, "right")]
    [DataRow("Auto", 2, "sum")]
    [DataRow("Scalar", 0, "left")]
    [DataRow("Scalar", 1, "right")]
    [DataRow("Scalar", 2, "sum")]
    [DataRow("Avx2", 0, "left")]
    [DataRow("Avx2", 1, "right")]
    [DataRow("Avx2", 2, "sum")]
    public void AddModQ_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLDsaEngine.N - 1 : MLDsaEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.AddModQ(kind, spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's sum forbids inlining and is aggressively optimized.
    /// </summary>
    [TestMethod]
    public void AddModQ_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.AddModQ));
    }
}
