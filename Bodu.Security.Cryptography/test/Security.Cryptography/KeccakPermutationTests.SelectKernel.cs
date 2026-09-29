// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutationTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class KeccakPermutationTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete four-way kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        KeccakPermutation.KernelKind kernel = KeccakPermutation.SelectKernel();

        Assert.AreNotEqual(KeccakPermutation.KernelKind.Auto, kernel);
        Assert.IsTrue(KeccakPermutation.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch prefers the AVX-512 kernel wherever its gate is open.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512IsAvailable_ShouldReturnAvx512()
    {
        if (!SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("AVX-512VL is not available on this processor.");

        Assert.AreEqual(KeccakPermutation.KernelKind.Avx512, KeccakPermutation.SelectKernel());
    }

    /// <summary>
    /// Verifies that the scalar kernel, and the dispatched kind, run on every processor.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    public void IsSupported_WhenKernelRunsEverywhere_ShouldReturnTrue(string kernel)
    {
        Assert.IsTrue(KeccakPermutation.IsSupported(Enum.Parse<KeccakPermutation.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(KeccakPermutation.IsSupported((KeccakPermutation.KernelKind)99));
    }

    /// <summary>
    /// Verifies that each vector kernel is reported as supported exactly when the processor has its instruction sets,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        bool avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported;

        Assert.AreEqual(avx2, KeccakPermutation.IsSupported(KeccakPermutation.KernelKind.Avx2));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, KeccakPermutation.IsSupported(KeccakPermutation.KernelKind.Avx512));
    }

    /// <summary>
    /// Verifies that four-way sampling is reported accelerated exactly where dispatch selects a vector kernel.
    /// </summary>
    [TestMethod]
    public void IsFourWayAccelerated_WhenQueried_ShouldMatchWhetherDispatchSelectsAVectorKernel()
    {
        Assert.AreEqual(KeccakPermutation.SelectKernel() != KeccakPermutation.KernelKind.Scalar, KeccakPermutation.IsFourWayAccelerated);
    }
}
