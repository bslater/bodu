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
    /// Verifies that dispatch prefers the AVX2 kernel wherever its gate is open, whatever the platform.
    /// </summary>
    /// <param name="appleSilicon">Whether dispatch makes the choice for Apple's cores.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SelectKernel_WhenAvx2IsAvailable_ShouldReturnAvx2(bool appleSilicon)
    {
        if (!SimdCapabilities.Avx2)
            Assert.Inconclusive("AVX2 is not available on this processor.");

        Assert.AreEqual(Argon2Core.KernelKind.Avx2, Argon2Core.SelectKernel(appleSilicon));
    }

    /// <summary>
    /// Verifies that dispatch makes the choice of the platform <see cref="SimdCapabilities.AppleSilicon" /> reports when
    /// the caller names none.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenNoPlatformIsNamed_ShouldMakeThisPlatformsChoice()
    {
        Assert.AreEqual(Argon2Core.SelectKernel(SimdCapabilities.AppleSilicon), Argon2Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that under .NET 10 dispatch selects the scalar kernel on every ARM64 processor, Apple's included: it
    /// ran faster than the AdvSimd kernel on a Neoverse N2 and as fast on an Apple M1.
    /// </summary>
    /// <param name="appleSilicon">Whether dispatch makes the choice for Apple's cores.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SelectKernel_WhenAdvSimdIsAvailableUnderNet10_ShouldReturnScalar(bool appleSilicon)
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

#if NET10_0_OR_GREATER
        Assert.AreEqual(Argon2Core.KernelKind.Scalar, Argon2Core.SelectKernel(appleSilicon));
#else
        Assert.Inconclusive("The runtime is not .NET 10 or later.");
#endif
    }

    /// <summary>
    /// Verifies that under .NET 8 dispatch selects the AdvSimd kernel on Apple's cores, where it ran 1.3 to 1.6 times
    /// as fast as the scalar kernel on an Apple M1, and took about a third of 1.0.0's CPU where the scalar kernel took
    /// nearly two thirds.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailableUnderNet8OnAppleSilicon_ShouldReturnAdvSimd()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

#if NET10_0_OR_GREATER
        Assert.Inconclusive("The runtime is .NET 10 or later.");
#else
        Assert.AreEqual(Argon2Core.KernelKind.AdvSimd, Argon2Core.SelectKernel(appleSilicon: true));
#endif
    }

    /// <summary>
    /// Verifies that under .NET 8 dispatch selects the scalar kernel on ARM64 processors other than Apple's: on a
    /// Neoverse N2 the AdvSimd kernel ran at 0.77 to 0.80 of its speed.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAdvSimdIsAvailableUnderNet8ElsewhereThanAppleSilicon_ShouldReturnScalar()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

#if NET10_0_OR_GREATER
        Assert.Inconclusive("The runtime is .NET 10 or later.");
#else
        Assert.AreEqual(Argon2Core.KernelKind.Scalar, Argon2Core.SelectKernel(appleSilicon: false));
#endif
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
    [DataRow("ScalarResident")]
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
    public void ResolveKernel_WhenKernelIsNamed_ShouldReturnIt(string kernel)
    {
        Argon2Core.KernelKind named = Enum.Parse<Argon2Core.KernelKind>(kernel);

        Assert.AreEqual(named, new Argon2Core.FillOptions(1, kernel: named).ResolveKernel());
        Assert.AreEqual(Argon2Core.SelectKernel(), new Argon2Core.FillOptions(1).ResolveKernel());
    }
}
