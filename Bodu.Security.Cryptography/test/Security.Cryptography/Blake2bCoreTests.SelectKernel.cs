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
}
