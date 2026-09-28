// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        ChaCha20Core.KernelKind kernel = ChaCha20Core.SelectKernel();

        Assert.AreNotEqual(ChaCha20Core.KernelKind.Auto, kernel);
        Assert.IsTrue(ChaCha20Core.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch selects the sixteen-block kernel where AVX-512VL is available and the runtime prefers
    /// 512-bit vectors.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512AndVector512AreAvailable_ShouldReturnAvx512Wide()
    {
        if (!SimdCapabilities.Avx512FVL || !System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
            Assert.Inconclusive("AVX-512VL is not available, or the runtime does not prefer 512-bit vectors, on this processor.");

        Assert.AreEqual(ChaCha20Core.KernelKind.Avx512Wide, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AVX-512 kernels without the sixteen-block one where the runtime does not
    /// prefer 512-bit vectors.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512IsAvailableWithoutVector512_ShouldReturnAvx512()
    {
        if (!SimdCapabilities.Avx512FVL || System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
            Assert.Inconclusive("AVX-512VL is not available, or the runtime prefers 512-bit vectors, on this processor.");

        Assert.AreEqual(ChaCha20Core.KernelKind.Avx512, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AVX2 kernels on an x64 processor with AVX2 but without AVX-512.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsTheWidestGateOpen_ShouldReturnAvx2()
    {
        if (!SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with AVX2 and without AVX-512.");

        Assert.AreEqual(ChaCha20Core.KernelKind.Avx2, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AdvSimd kernel on ARM64.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailable_ShouldReturnAdvSimd()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

        Assert.AreEqual(ChaCha20Core.KernelKind.AdvSimd, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the SSSE3 kernel on an x64 processor with SSSE3 but without AVX2.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenSsse3IsTheWidestGateOpen_ShouldReturnSsse3()
    {
        if (!SimdCapabilities.Ssse3 || SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with SSSE3 and without AVX2.");

        Assert.AreEqual(ChaCha20Core.KernelKind.Ssse3, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that the kinds that run everywhere are always supported.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    public void IsSupported_WhenKernelRunsEverywhere_ShouldReturnTrue(string kernel)
    {
        Assert.IsTrue(ChaCha20Core.IsSupported(Enum.Parse<ChaCha20Core.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that an undefined kind is not supported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(ChaCha20Core.IsSupported((ChaCha20Core.KernelKind)99));
    }

    /// <summary>
    /// Verifies that each vectorized kind is supported exactly when the processor has the instructions it uses.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        bool avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported;

        Assert.AreEqual(System.Runtime.Intrinsics.X86.Ssse3.IsSupported, ChaCha20Core.IsSupported(ChaCha20Core.KernelKind.Ssse3));
        Assert.AreEqual(avx2, ChaCha20Core.IsSupported(ChaCha20Core.KernelKind.Avx2));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, ChaCha20Core.IsSupported(ChaCha20Core.KernelKind.Avx512));
        Assert.AreEqual(avx2 && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, ChaCha20Core.IsSupported(ChaCha20Core.KernelKind.Avx512Wide));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, ChaCha20Core.IsSupported(ChaCha20Core.KernelKind.AdvSimd));
    }
}
