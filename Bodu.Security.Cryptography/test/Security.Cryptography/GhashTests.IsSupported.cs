// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.IsSupported.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public sealed partial class GhashTests
{
    /// <summary>
    /// Verifies that the scalar kernel is reported as runnable on every processor.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsScalar_ShouldReturnTrue()
    {
        Assert.IsTrue(Ghash.IsSupported(Ghash.KernelKind.Scalar));
    }

    /// <summary>
    /// Verifies that the ARM64 polynomial-multiply kernel is reported as unavailable on any other architecture.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsPmullAndProcessIsNotArm64_ShouldReturnFalse()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            Assert.Inconclusive("The process is running on ARM64.");

        Assert.IsFalse(Ghash.IsSupported(Ghash.KernelKind.Pmull));
    }

    /// <summary>
    /// Verifies that the x64 carry-less-multiply kernel is reported as unavailable on any other architecture.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsPclmulqdqAndProcessIsNotX64_ShouldReturnFalse()
    {
        if (RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86)
            Assert.Inconclusive("The process is running on x86 or x64.");

        Assert.IsFalse(Ghash.IsSupported(Ghash.KernelKind.Pclmulqdq));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported, so it can never be dispatched.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(Ghash.IsSupported((Ghash.KernelKind)99));
    }
}
