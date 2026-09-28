// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        Blake3Core.KernelKind kernel = Blake3Core.SelectKernel();

        Assert.AreNotEqual(Blake3Core.KernelKind.Auto, kernel);
        Assert.IsTrue(Blake3Core.IsSupported(kernel), kernel.ToString());
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
        Assert.IsTrue(Blake3Core.IsSupported(Enum.Parse<Blake3Core.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(Blake3Core.IsSupported((Blake3Core.KernelKind)99));
    }

    /// <summary>
    /// Verifies that dispatch prefers the sixteen-way AVX-512 kernels wherever their gate is open and the runtime
    /// prefers 512-bit vectors.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512AndVector512AreAvailable_ShouldReturnAvx512Wide()
    {
        if (!SimdCapabilities.Avx512FVL || !System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
            Assert.Inconclusive("AVX-512VL is not available, or the runtime does not prefer 512-bit vectors, on this processor.");

        Assert.AreEqual(Blake3Core.KernelKind.Avx512Wide, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch keeps to the eight-way AVX-512 kernels where the runtime does not prefer 512-bit vectors.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512IsAvailableWithoutVector512_ShouldReturnAvx512()
    {
        if (!SimdCapabilities.Avx512FVL || System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
            Assert.Inconclusive("AVX-512VL is not available, or the runtime prefers 512-bit vectors, on this processor.");

        Assert.AreEqual(Blake3Core.KernelKind.Avx512, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AVX2 kernels on an x64 processor without AVX-512.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsTheWidestGateOpen_ShouldReturnAvx2()
    {
        if (!SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with AVX2 and without AVX-512.");

        Assert.AreEqual(Blake3Core.KernelKind.Avx2, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AdvSimd kernels on ARM64.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailable_ShouldReturnAdvSimd()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

        Assert.AreEqual(Blake3Core.KernelKind.AdvSimd, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch falls back to the SSSE3 kernel on an x64 processor without AVX2.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenSsse3IsTheWidestGateOpen_ShouldReturnSsse3()
    {
        if (!SimdCapabilities.Ssse3 || SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with SSSE3 and without AVX2.");

        Assert.AreEqual(Blake3Core.KernelKind.Ssse3, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that each vector kernel is reported as supported exactly when the processor has its instruction sets,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        bool avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported;

        Assert.AreEqual(System.Runtime.Intrinsics.X86.Ssse3.IsSupported, Blake3Core.IsSupported(Blake3Core.KernelKind.Ssse3));
        Assert.AreEqual(avx2, Blake3Core.IsSupported(Blake3Core.KernelKind.Avx2));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, Blake3Core.IsSupported(Blake3Core.KernelKind.Avx512));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, Blake3Core.IsSupported(Blake3Core.KernelKind.Avx512Wide));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Blake3Core.IsSupported(Blake3Core.KernelKind.AdvSimd));
    }
}
