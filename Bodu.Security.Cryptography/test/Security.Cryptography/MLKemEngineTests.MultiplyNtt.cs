// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.MultiplyNtt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that each kernel's base-case products match the remainder-operator reference on edge, patterned and
    /// seeded polynomials, including every coefficient at q − 1, where the products and their sums are largest.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyNtt_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] polynomials = Polynomials().ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i * 7 + 1) % polynomials.Length];
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemReference.MultiplyNtt(left, right, expected);
            MLKemEngine.MultiplyNtt(kind, left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that each kernel's base-case products stay exact when the left factor's coefficients reach 4095, the
    /// largest value 12-bit decoding yields: FIPS 203's decapsulation-key check does not bound the packed secret vector
    /// by q.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void MultiplyNtt_WhenLeftCoefficientsReachTheTwelveBitMaximum_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[] left = Enumerable.Repeat(4095, MLKemEngine.N).ToArray();
        int pair = 0;

        foreach (int[] right in Polynomials())
        {
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemReference.MultiplyNtt(left, right, expected);
            MLKemEngine.MultiplyNtt(kind, left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {pair++}");
        }
    }

    /// <summary>
    /// Verifies that the base-case products without a named kernel multiply as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void MultiplyNtt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        int[][] polynomials = Polynomials().Take(20).ToArray();

        for (int i = 0; i < polynomials.Length; i++)
        {
            int[] left = polynomials[i];
            int[] right = polynomials[(i + 1) % polynomials.Length];
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemEngine.MultiplyNtt(MLKemEngine.SelectKernel(), left, right, expected);
            MLKemEngine.MultiplyNtt(left, right, actual);

            CollectionAssert.AreEqual(expected, actual, $"pair {i}");
        }
    }

    /// <summary>
    /// Verifies that the NTT-domain product rejects an operand or destination shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading or writing past its end, whichever
    /// kernel is named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="shortSpan">The position of the span one coefficient short: 0, 1 or 2.</param>
    /// <param name="expectedParamName">The name of the parameter that span is passed as.</param>
    [TestMethod]
    [DataRow("Auto", 0, "left")]
    [DataRow("Auto", 1, "right")]
    [DataRow("Auto", 2, "destination")]
    [DataRow("Scalar", 0, "left")]
    [DataRow("Scalar", 1, "right")]
    [DataRow("Scalar", 2, "destination")]
    [DataRow("Avx2", 0, "left")]
    [DataRow("Avx2", 1, "right")]
    [DataRow("Avx2", 2, "destination")]
    public void MultiplyNtt_WhenASpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel, int shortSpan, string expectedParamName)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int[][] spans = Enumerable.Range(0, 3).Select(i => new int[i == shortSpan ? MLKemEngine.N - 1 : MLKemEngine.N]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.MultiplyNtt(kind, spans[0], spans[1], spans[2]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's base-case products forbid inlining and are aggressively optimized, so they are
    /// compiled on their own rather than into the dispatcher.
    /// </summary>
    /// <remarks>
    /// A kernel that cannot be inlined is compiled on its own, with its own inlining budget, whatever dynamic PGO makes
    /// of the dispatcher; one that is also aggressively optimized is compiled fully optimized at its first call.
    /// </remarks>
    [TestMethod]
    public void MultiplyNtt_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(MLKemEngine.Vector256Kernel).GetMethod(nameof(MLKemEngine.Vector256Kernel.MultiplyNtt), BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
        {
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
        }
    }
}
