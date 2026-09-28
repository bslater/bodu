// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GhashTests
{
    /// <summary>
    /// Verifies that dispatch selects a kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        Assert.IsTrue(Ghash.IsSupported(Ghash.SelectKernel()));
    }

    /// <summary>
    /// Verifies that dispatch selects the carry-less multiply wherever its gate is open.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenPclmulqdqIsAvailable_ShouldReturnPclmulqdq()
    {
        if (!SimdCapabilities.Pclmulqdq)
            Assert.Inconclusive("PCLMULQDQ is not available on this processor.");

        Assert.AreEqual(Ghash.KernelKind.Pclmulqdq, Ghash.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch selects the polynomial multiply on ARM64 with the cryptography extension.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenPmullIsAvailable_ShouldReturnPmull()
    {
        if (!SimdCapabilities.Pmull)
            Assert.Inconclusive("PMULL is not available on this processor.");

        Assert.AreEqual(Ghash.KernelKind.Pmull, Ghash.SelectKernel());
    }
}
