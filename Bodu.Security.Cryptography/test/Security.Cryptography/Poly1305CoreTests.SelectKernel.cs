// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that dispatch keeps every run shorter than <see cref="Poly1305Core.KernelMinimumBytes" /> on the scalar
    /// loop, whatever the processor supports, as the block loop assumes when it sends such runs there without
    /// consulting dispatch.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenRunIsShorterThanTheKernelMinimum_ShouldReturnScalar()
    {
        for (int length = 0; length < Poly1305Core.KernelMinimumBytes; length += Poly1305Core.BlockBytes)
            Assert.AreEqual(Poly1305Core.KernelKind.Scalar, Poly1305Core.SelectKernel(length), $"length {length}");
    }

    /// <summary>
    /// Verifies that the shortest run any kernel takes is <see cref="Poly1305Core.AppleAdvSimdMinimumBytes" /> on
    /// Apple's cores, <see cref="Poly1305Core.AdvSimdMinimumBytes" /> on other ARM64 processors, and
    /// <see cref="Poly1305Core.Avx2MinimumBytes" /> everywhere else, so that the block loop sends every shorter run to
    /// the scalar loop without consulting dispatch.
    /// </summary>
    [TestMethod]
    public void KernelMinimumBytes_WhenRead_ShouldBeTheShortestRunAKernelTakesOnThisProcessor()
    {
        int expected = Poly1305Core.Avx2MinimumBytes;
        if (SimdCapabilities.AppleSilicon)
            expected = Poly1305Core.AppleAdvSimdMinimumBytes;
        else if (System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported)
            expected = Poly1305Core.AdvSimdMinimumBytes;

        Assert.AreEqual(expected, Poly1305Core.KernelMinimumBytes);
    }

    /// <summary>
    /// Verifies that dispatch makes the choices of the platform <see cref="SimdCapabilities.AppleSilicon" /> reports
    /// when the caller names none.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenNoPlatformIsNamed_ShouldMakeThisPlatformsChoices()
    {
        for (int length = 0; length <= Poly1305Core.Avx512MinimumBytes; length += Poly1305Core.BlockBytes)
            Assert.AreEqual(Poly1305Core.SelectKernel(length, SimdCapabilities.AppleSilicon), Poly1305Core.SelectKernel(length), $"length {length}");
    }

    /// <summary>
    /// Verifies that the platform changes no choice dispatch makes on a processor other than ARM64, on which Apple's
    /// cores differ from others only in the AdvSimd kernel's threshold.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenProcessorIsNotArm64_ShouldIgnoreThePlatform()
    {
        if (System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported)
            Assert.Inconclusive("The processor is an ARM64 processor.");

        for (int length = 0; length <= Poly1305Core.Avx512MinimumBytes; length += Poly1305Core.BlockBytes)
            Assert.AreEqual(Poly1305Core.SelectKernel(length, appleSilicon: false), Poly1305Core.SelectKernel(length, appleSilicon: true), $"length {length}");
    }

    /// <summary>
    /// Verifies that dispatch gives runs from <see cref="Poly1305Core.Avx2MinimumBytes" /> up to
    /// <see cref="Poly1305Core.Avx2PairedMinimumBytes" /> to the AVX2 kernel's one-group loop wherever AVX2 is
    /// available.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.Avx2MinimumBytes)]
    [DataRow(Poly1305Core.Avx2PairedMinimumBytes - Poly1305Core.BlockBytes)]
    public void SelectKernel_WhenRunReachesTheAvx2Minimum_ShouldReturnAvx2(int length)
    {
        if (!SimdCapabilities.Avx2)
            Assert.Inconclusive("AVX2 is not available on this processor, or the process allows no vector code.");

        Assert.AreEqual(Poly1305Core.KernelKind.Avx2, Poly1305Core.SelectKernel(length));
    }

    /// <summary>
    /// Verifies that dispatch gives runs from <see cref="Poly1305Core.Avx2PairedMinimumBytes" /> up to
    /// <see cref="Poly1305Core.Avx512MinimumBytes" /> to the AVX2 kernel's paired loop where AVX-512VL provides the
    /// registers it needs.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.Avx2PairedMinimumBytes)]
    [DataRow(Poly1305Core.Avx512MinimumBytes - Poly1305Core.BlockBytes)]
    public void SelectKernel_WhenRunReachesThePairedMinimumWithAvx512VL_ShouldReturnAvx2Paired(int length)
    {
        if (!SimdCapabilities.Avx2 || !SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("AVX2 or AVX-512VL is not available on this processor, or the process allows no vector code.");

        Assert.AreEqual(Poly1305Core.KernelKind.Avx2Paired, Poly1305Core.SelectKernel(length));
    }

    /// <summary>
    /// Verifies that dispatch keeps runs of <see cref="Poly1305Core.Avx2PairedMinimumBytes" /> or more on the AVX2
    /// kernel's one-group loop where AVX2 is available without AVX-512VL, whose 16 registers the paired loop overflows.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.Avx2PairedMinimumBytes)]
    [DataRow(Poly1305Core.Avx512MinimumBytes - Poly1305Core.BlockBytes)]
    public void SelectKernel_WhenRunReachesThePairedMinimumWithoutAvx512VL_ShouldReturnAvx2(int length)
    {
        if (!SimdCapabilities.Avx2 || SimdCapabilities.Avx512FVL)
            Assert.Inconclusive("AVX2 is not available, or AVX-512VL is, on this processor.");

        Assert.AreEqual(Poly1305Core.KernelKind.Avx2, Poly1305Core.SelectKernel(length));
    }

    /// <summary>
    /// Verifies that dispatch gives runs of <see cref="Poly1305Core.Avx512MinimumBytes" /> or more to the AVX-512
    /// kernel where AVX-512F is available and the runtime prefers 512-bit vectors.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.Avx512MinimumBytes)]
    [DataRow(1 << 20)]
    public void SelectKernel_WhenRunReachesTheAvx512Minimum_ShouldReturnAvx512(int length)
    {
        if (!SimdCapabilities.Avx512F || !Vector512.IsHardwareAccelerated)
            Assert.Inconclusive("AVX-512F is not available, or the runtime does not prefer 512-bit vectors, on this processor.");

        Assert.AreEqual(Poly1305Core.KernelKind.Avx512, Poly1305Core.SelectKernel(length));
    }

    /// <summary>
    /// Verifies that dispatch keeps long runs on the AVX2 kernel where the runtime does not prefer 512-bit vectors,
    /// or the processor has no AVX-512F: the paired loop with AVX-512VL, the one-group loop without it.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsTheWidestKernelAllowed_ShouldReturnAnAvx2LoopForLongRuns()
    {
        if (!SimdCapabilities.Avx2 || (SimdCapabilities.Avx512F && Vector512.IsHardwareAccelerated))
            Assert.Inconclusive("AVX2 is not available, or the AVX-512 kernel is, on this processor.");

        Poly1305Core.KernelKind expected = SimdCapabilities.Avx512FVL ? Poly1305Core.KernelKind.Avx2Paired : Poly1305Core.KernelKind.Avx2;
        Assert.AreEqual(expected, Poly1305Core.SelectKernel(1 << 20));
    }

    /// <summary>
    /// Verifies that dispatch gives runs of <see cref="Poly1305Core.AdvSimdMinimumBytes" /> or more to the AdvSimd
    /// kernel on every ARM64 processor, Apple's included.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    /// <param name="appleSilicon">Whether dispatch makes the choices for Apple's cores.</param>
    [TestMethod]
    [DataRow(Poly1305Core.AdvSimdMinimumBytes, false)]
    [DataRow(1 << 20, false)]
    [DataRow(Poly1305Core.AdvSimdMinimumBytes, true)]
    [DataRow(1 << 20, true)]
    public void SelectKernel_WhenRunReachesTheAdvSimdMinimum_ShouldReturnAdvSimd(int length, bool appleSilicon)
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor, or the process allows no vector code.");

        Assert.AreEqual(Poly1305Core.KernelKind.AdvSimd, Poly1305Core.SelectKernel(length, appleSilicon));
    }

    /// <summary>
    /// Verifies that on Apple's cores dispatch gives runs from <see cref="Poly1305Core.AppleAdvSimdMinimumBytes" /> up
    /// to <see cref="Poly1305Core.AdvSimdMinimumBytes" /> to the AdvSimd kernel, which caught the scalar loop at 128
    /// bytes on an Apple M1.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.AppleAdvSimdMinimumBytes)]
    [DataRow(Poly1305Core.AdvSimdMinimumBytes - Poly1305Core.BlockBytes)]
    public void SelectKernel_WhenRunReachesTheAppleMinimumOnAppleSilicon_ShouldReturnAdvSimd(int length)
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor, or the process allows no vector code.");

        Assert.AreEqual(Poly1305Core.KernelKind.AdvSimd, Poly1305Core.SelectKernel(length, appleSilicon: true));
    }

    /// <summary>
    /// Verifies that on Apple's cores dispatch keeps every run shorter than
    /// <see cref="Poly1305Core.AppleAdvSimdMinimumBytes" /> on the scalar loop.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenRunIsShorterThanTheAppleMinimumOnAppleSilicon_ShouldReturnScalar()
    {
        for (int length = 0; length < Poly1305Core.AppleAdvSimdMinimumBytes; length += Poly1305Core.BlockBytes)
            Assert.AreEqual(Poly1305Core.KernelKind.Scalar, Poly1305Core.SelectKernel(length, appleSilicon: true), $"length {length}");
    }

    /// <summary>
    /// Verifies that on ARM64 processors other than Apple's, dispatch keeps runs shorter than
    /// <see cref="Poly1305Core.AdvSimdMinimumBytes" /> on the scalar loop, which finished them first on a Neoverse N2.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.AppleAdvSimdMinimumBytes)]
    [DataRow(Poly1305Core.AdvSimdMinimumBytes - Poly1305Core.BlockBytes)]
    public void SelectKernel_WhenRunIsShorterThanTheAdvSimdMinimumElsewhere_ShouldReturnScalar(int length)
    {
        Assert.AreEqual(Poly1305Core.KernelKind.Scalar, Poly1305Core.SelectKernel(length, appleSilicon: false));
    }

    /// <summary>
    /// Verifies that dispatch keeps every run on the scalar loop where the processor has neither AVX2 nor AdvSimd, or
    /// the process allows no vector code.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenNoVectorKernelIsAvailable_ShouldReturnScalarForLongRuns()
    {
        if (SimdCapabilities.Avx2 || SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AVX2 or AdvSimd is available on this processor.");

        Assert.AreEqual(Poly1305Core.KernelKind.Scalar, Poly1305Core.SelectKernel(1 << 20));
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
        Assert.IsTrue(Poly1305Core.IsSupported(Enum.Parse<Poly1305Core.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that an undefined kind is not supported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(Poly1305Core.IsSupported((Poly1305Core.KernelKind)99));
    }

    /// <summary>
    /// Verifies that each vector kind is supported exactly when the processor has the instructions it uses, whether or
    /// not the process allows vector code.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Avx2.IsSupported, Poly1305Core.IsSupported(Poly1305Core.KernelKind.Avx2));
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Avx2.IsSupported, Poly1305Core.IsSupported(Poly1305Core.KernelKind.Avx2Paired));
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Avx512F.IsSupported, Poly1305Core.IsSupported(Poly1305Core.KernelKind.Avx512));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Poly1305Core.IsSupported(Poly1305Core.KernelKind.AdvSimd));
    }
}
