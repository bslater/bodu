// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake2bCoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        Blake2bCore.KernelKind kernel = Blake2bCore.SelectKernel();

        Assert.AreNotEqual(Blake2bCore.KernelKind.Auto, kernel);
        Assert.IsTrue(Blake2bCore.IsSupported(kernel), kernel.ToString());
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
        Assert.IsTrue(Blake2bCore.IsSupported(Enum.Parse<Blake2bCore.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(Blake2bCore.IsSupported((Blake2bCore.KernelKind)99));
    }

    /// <summary>
    /// Verifies that dispatch prefers the AVX-512 kernel wherever its gate is open.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512IsAvailable_ShouldReturnAvx512()
    {
        if (!SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("AVX-512VL is not available on this processor.");

        Assert.AreEqual(Blake2bCore.KernelKind.Avx512, Blake2bCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch falls back to the AVX2 kernel on an x64 processor without AVX-512.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsTheWidestGateOpen_ShouldReturnAvx2()
    {
        if (!SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with AVX2 and without AVX-512.");

        Assert.AreEqual(Blake2bCore.KernelKind.Avx2, Blake2bCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the scalar kernel on ARM64, where it ran faster than the AdvSimd kernel on the
    /// processors measured; the AdvSimd kernel runs only where a caller names it.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailable_ShouldReturnScalar()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

        Assert.AreEqual(Blake2bCore.KernelKind.Scalar, Blake2bCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch falls back to the SSSE3 kernel on an x64 processor without AVX2.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenSsse3IsTheWidestGateOpen_ShouldReturnSsse3()
    {
        if (!SimdCapabilities.Ssse3 || SimdCapabilities.Avx2)
            Assert.Inconclusive("The processor is not an x64 processor with SSSE3 and without AVX2.");

        Assert.AreEqual(Blake2bCore.KernelKind.Ssse3, Blake2bCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that each vector kernel is reported as supported exactly when the processor has its instruction sets,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        bool avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported;

        Assert.AreEqual(System.Runtime.Intrinsics.X86.Ssse3.IsSupported, Blake2bCore.IsSupported(Blake2bCore.KernelKind.Ssse3));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Blake2bCore.IsSupported(Blake2bCore.KernelKind.AdvSimd));

        Assert.AreEqual(avx2, Blake2bCore.IsSupported(Blake2bCore.KernelKind.Avx2));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, Blake2bCore.IsSupported(Blake2bCore.KernelKind.Avx512));
    }
}
