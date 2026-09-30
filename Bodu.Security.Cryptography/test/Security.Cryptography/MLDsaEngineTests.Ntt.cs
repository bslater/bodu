// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that each kernel's forward transform matches the remainder-operator reference on edge and seeded
    /// polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Ntt_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLDsaReference.Ntt(expected);
            MLDsaEngine.Ntt(kind, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that each kernel's forward transform accepts coefficients up to q − 1 in magnitude, the ends of the
    /// range it documents, and transforms their residues as the reference and the scalar kernel do.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Ntt_WhenCoefficientsAreSigned_ForEachKernel_ShouldTransformTheirResidues(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int iteration = 0;

        foreach (int[] polynomial in SignedPolynomials())
        {
            int[] expected = polynomial.Select(value => Mod(value)).ToArray();
            int[] scalar = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLDsaReference.Ntt(expected);
            MLDsaEngine.Ntt(MLDsaEngine.KernelKind.Scalar, scalar);
            MLDsaEngine.Ntt(kind, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
            CollectionAssert.AreEqual(scalar, actual, $"iteration {iteration}");
            iteration++;
        }
    }

    /// <summary>
    /// Verifies that the forward transform without a named kernel transforms as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void Ntt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in SignedPolynomials().Take(20))
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLDsaEngine.Ntt(MLDsaEngine.SelectKernel(), expected);
            MLDsaEngine.Ntt(actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that each kernel's inverse transform matches the remainder-operator reference on edge and seeded
    /// polynomials.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InvNtt_WhenCoefficientsAreInRange_ForEachKernel_ShouldMatchTheReference(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials())
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLDsaReference.InvNtt(expected);
            MLDsaEngine.InvNtt(kind, actual);

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that each kernel's inverse transform accepts coefficients up to q − 1 in magnitude, the ends of the
    /// range it documents, including the patterns that drive the last layer's sum and difference to 256(q − 1), and
    /// transforms their residues as the reference and the scalar kernel do.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void InvNtt_WhenCoefficientsReachQInMagnitude_ForEachKernel_ShouldTransformTheirResidues(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        int iteration = 0;

        foreach (int[] coefficients in SignedPolynomials())
        {
            int[] expected = coefficients.Select(value => Mod(value)).ToArray();
            int[] scalar = (int[])coefficients.Clone();
            int[] actual = (int[])coefficients.Clone();

            MLDsaReference.InvNtt(expected);
            MLDsaEngine.InvNtt(MLDsaEngine.KernelKind.Scalar, scalar);
            MLDsaEngine.InvNtt(kind, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
            CollectionAssert.AreEqual(scalar, actual, $"iteration {iteration}");
            iteration++;
        }
    }

    /// <summary>
    /// Verifies that the inverse transform without a named kernel transforms as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void InvNtt_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in SignedPolynomials().Take(20))
        {
            int[] expected = (int[])polynomial.Clone();
            int[] actual = (int[])polynomial.Clone();

            MLDsaEngine.InvNtt(MLDsaEngine.SelectKernel(), expected);
            MLDsaEngine.InvNtt(actual);

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
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials())
        {
            int[] roundTrip = (int[])polynomial.Clone();

            MLDsaEngine.Ntt(kind, roundTrip);
            MLDsaEngine.InvNtt(kind, roundTrip);

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
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var forward = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.Ntt(kind, new int[MLDsaEngine.N - 1]);
        });
        var inverse = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.InvNtt(kind, new int[MLDsaEngine.N - 1]);
        });

        Assert.AreEqual("w", forward.ParamName);
        Assert.AreEqual("w", inverse.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's transforms forbid inlining and are aggressively optimized, so each is compiled
    /// on its own rather than into the dispatcher.
    /// </summary>
    [TestMethod]
    public void Ntt_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.Ntt));
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.InvNtt));
    }
}
