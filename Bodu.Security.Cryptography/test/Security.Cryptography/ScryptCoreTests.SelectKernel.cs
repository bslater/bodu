// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        ScryptCore.KernelKind kernel = ScryptCore.SelectKernel();

        Assert.AreNotEqual(ScryptCore.KernelKind.Auto, kernel);
        Assert.IsTrue(ScryptCore.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch selects the SSE2 kernel on x64.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenSse2IsAvailable_ShouldReturnSse2()
    {
        if (!SimdCapabilities.Sse2)
            Assert.Inconclusive("SSE2 is not available on this processor.");

        Assert.AreEqual(ScryptCore.KernelKind.Sse2, ScryptCore.SelectKernel());
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

        Assert.AreEqual(ScryptCore.KernelKind.Scalar, ScryptCore.SelectKernel());
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
        Assert.IsTrue(ScryptCore.IsSupported(Enum.Parse<ScryptCore.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(ScryptCore.IsSupported((ScryptCore.KernelKind)99));
    }

    /// <summary>
    /// Verifies that each vector kernel is reported as supported exactly when the processor has its instruction set,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Sse2.IsSupported, ScryptCore.IsSupported(ScryptCore.KernelKind.Sse2));
        Assert.AreEqual(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, ScryptCore.IsSupported(ScryptCore.KernelKind.AdvSimd));
    }

    /// <summary>
    /// Verifies that options naming no kernel resolve to the one dispatch selects, and options naming one resolve to it
    /// whether or not the processor supports it.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void ResolveKernel_WhenKernelIsNamed_ShouldReturnIt(string kernel)
    {
        ScryptCore.KernelKind named = Enum.Parse<ScryptCore.KernelKind>(kernel);

        Assert.AreEqual(named, new ScryptCore.MixOptions(1, kernel: named).ResolveKernel());
        Assert.AreEqual(ScryptCore.SelectKernel(), new ScryptCore.MixOptions(1).ResolveKernel());
    }
}
