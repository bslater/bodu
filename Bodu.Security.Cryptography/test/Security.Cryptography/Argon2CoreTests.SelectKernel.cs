// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2CoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        Argon2Core.KernelKind kernel = Argon2Core.SelectKernel();

        Assert.AreNotEqual(Argon2Core.KernelKind.Auto, kernel);
        Assert.IsTrue(Argon2Core.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch prefers the 256-bit kernel with AVX-512VL's rotation wherever that gate is open.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512VLIsAvailable_ShouldReturnAvx512()
    {
        if (!SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("AVX-512VL is not available on this processor.");

        Assert.AreEqual(Argon2Core.KernelKind.Avx512, Argon2Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the AVX2 kernel on an x64 processor with AVX2 and without AVX-512VL.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsTheWidestGateOpen_ShouldReturnAvx2()
    {
        if (!SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("The processor is not an x64 processor with AVX2 and without AVX-512VL.");

        Assert.AreEqual(Argon2Core.KernelKind.Avx2, Argon2Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the hybrid kernel on every ARM64 processor under both runtimes: it ran 1.24 to
    /// 1.28 times as fast as the scalar kernel on a Neoverse N2, and 1.3 to 1.6 times as fast as the AdvSimd kernel on
    /// an Apple M1.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailable_ShouldReturnAdvSimdHybrid()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

        Assert.AreEqual(Argon2Core.KernelKind.AdvSimdHybrid, Argon2Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch falls back to the SSSE3 kernel on an x64 processor without AVX2.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenSsse3IsTheWidestGateOpen_ShouldReturnSsse3()
    {
        if (!SimdCapabilities.Ssse3 || SimdCapabilities.Avx2)
            Assert.Inconclusive("The processor is not an x64 processor with SSSE3 and without AVX2.");

        Assert.AreEqual(Argon2Core.KernelKind.Ssse3, Argon2Core.SelectKernel());
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
        Assert.IsTrue(Argon2Core.IsSupported(Enum.Parse<Argon2Core.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(Argon2Core.IsSupported((Argon2Core.KernelKind)99));
    }

    /// <summary>
    /// Verifies that each vector kernel is reported as supported exactly when the processor has its instruction set,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Avx2.IsSupported, Argon2Core.IsSupported(Argon2Core.KernelKind.Avx2));
        Assert.AreEqual(
            System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
            Argon2Core.IsSupported(Argon2Core.KernelKind.Avx512));
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Ssse3.IsSupported, Argon2Core.IsSupported(Argon2Core.KernelKind.Ssse3));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Argon2Core.IsSupported(Argon2Core.KernelKind.AdvSimd));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Argon2Core.IsSupported(Argon2Core.KernelKind.AdvSimdHybrid));
    }

    /// <summary>
    /// Verifies that options naming no kernel resolve to the one dispatch selects, and options naming one resolve to
    /// it whether or not the processor supports it.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void ResolveKernel_WhenKernelIsNamed_ShouldReturnIt(string kernel)
    {
        Argon2Core.KernelKind named = Enum.Parse<Argon2Core.KernelKind>(kernel);

        Assert.AreEqual(named, new Argon2Core.FillOptions(1, kernel: named).ResolveKernel());
        Assert.AreEqual(Argon2Core.SelectKernel(), new Argon2Core.FillOptions(1).ResolveKernel());
    }
}
