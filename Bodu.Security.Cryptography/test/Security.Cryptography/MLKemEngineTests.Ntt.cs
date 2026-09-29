// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that each kernel's forward transform matches the remainder-operator reference on edge, patterned and
    /// seeded polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Ntt_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int iteration = 0;

        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemReference.Ntt(expected);
            MLKemEngine.Ntt(kind, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration++}");
        }
    }

    /// <summary>
    /// Verifies that the forward transform without a named kernel transforms as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void Ntt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemEngine.Ntt(MLKemEngine.SelectKernel(), expected);
            MLKemEngine.Ntt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that each kernel's inverse transform matches the remainder-operator reference on edge, patterned and
    /// seeded polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InvNtt_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int iteration = 0;

        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemReference.InvNtt(expected);
            MLKemEngine.InvNtt(kind, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration++}");
        }
    }

    /// <summary>
    /// Verifies that the inverse transform without a named kernel transforms as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void InvNtt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLKemEngine.InvNtt(MLKemEngine.SelectKernel(), expected);
            MLKemEngine.InvNtt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that each kernel's inverse transform undoes its forward transform.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InvNtt_WhenAppliedAfterNtt_ForEachKernel_ShouldRestoreThePolynomial(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials())
        {
            int[] roundTrip = (int[])polynomial.Clone();

            MLKemEngine.Ntt(kind, roundTrip);
            MLKemEngine.InvNtt(kind, roundTrip);

            CollectionAssert.AreEqual(polynomial, roundTrip);
        }
    }

    /// <summary>
    /// Verifies that the transforms reject a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than reading past its end, whichever kernel is
    /// named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Ntt_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel)
    {
        MLKemEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var forward = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.Ntt(kind, new int[MLKemEngine.N - 1]);
        });
        var inverse = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLKemEngine.InvNtt(kind, new int[MLKemEngine.N - 1]);
        });

        Assert.AreEqual("f", forward.ParamName);
        Assert.AreEqual("f", inverse.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's transforms forbid inlining and are aggressively optimized, so each is compiled
    /// on its own rather than into the dispatcher.
    /// </summary>
    /// <remarks>
    /// A kernel that cannot be inlined is compiled on its own, with its own inlining budget, whatever dynamic PGO makes
    /// of the dispatcher; one that is also aggressively optimized is compiled fully optimized at its first call.
    /// </remarks>
    [TestMethod]
    public void Ntt_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(MLKemEngine.Vector256Kernel).GetMethod(nameof(MLKemEngine.Vector256Kernel.Ntt), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(MLKemEngine.Vector256Kernel).GetMethod(nameof(MLKemEngine.Vector256Kernel.InvNtt), BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
        {
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
        }
    }
}
