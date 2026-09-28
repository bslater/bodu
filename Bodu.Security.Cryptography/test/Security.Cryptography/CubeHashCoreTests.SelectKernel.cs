// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCoreTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CubeHashCoreTests
{
    /// <summary>
    /// Verifies that dispatch selects a kernel the processor supports.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnASupportedKernel()
    {
        CubeHashCore.KernelKind kernel = CubeHashCore.SelectKernel();

        Assert.IsTrue(CubeHashCore.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch selects the 512-bit kernel wherever AVX-512F is available and allowed, as the kernel
    /// replaced here always did.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx512IsAvailable_ShouldSelectTheAvx512Kernel()
    {
        if (!SimdCapabilities.Avx512F)
            Assert.Inconclusive("AVX-512F is not available on this processor, or vector code is disabled.");

        Assert.AreEqual(CubeHashCore.KernelKind.Avx512, CubeHashCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that the automatic and scalar kinds are supported on every processor.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    public void IsSupported_WhenKernelRunsEverywhere_ShouldReturnTrue(string kernel)
    {
        Assert.IsTrue(CubeHashCore.IsSupported(Enum.Parse<CubeHashCore.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a kind outside the enumeration is not supported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(CubeHashCore.IsSupported((CubeHashCore.KernelKind)99));
    }
}
