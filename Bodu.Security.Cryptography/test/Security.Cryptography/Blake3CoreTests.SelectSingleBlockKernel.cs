// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.SelectSingleBlockKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run for a single block.
    /// </summary>
    [TestMethod]
    public void SelectSingleBlockKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        Blake3Core.KernelKind kernel = Blake3Core.SelectSingleBlockKernel();

        Assert.AreNotEqual(Blake3Core.KernelKind.Auto, kernel);
        Assert.IsTrue(Blake3Core.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that a single block runs on the scalar kernel on ARM64, where it ran faster than the 128-bit kernel,
    /// while inputs compressed four at a time keep the AdvSimd kernels.
    /// </summary>
    [TestMethod]
    public void SelectSingleBlockKernel_WhenAdvSimdIsAvailable_ShouldReturnScalar()
    {
        if (!SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is not available on this processor.");

        Assert.AreEqual(Blake3Core.KernelKind.Scalar, Blake3Core.SelectSingleBlockKernel());
        Assert.AreEqual(Blake3Core.KernelKind.AdvSimd, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that a single block runs on the kernel dispatch selects for several inputs wherever that kernel is not
    /// the AdvSimd one, as on every x64 processor.
    /// </summary>
    [TestMethod]
    public void SelectSingleBlockKernel_WhenAdvSimdIsUnavailable_ShouldReturnTheSelectedKernel()
    {
        if (SimdCapabilities.AdvSimd)
            Assert.Inconclusive("AdvSimd is available on this processor.");

        Assert.AreEqual(Blake3Core.SelectKernel(), Blake3Core.SelectSingleBlockKernel());
    }
}
