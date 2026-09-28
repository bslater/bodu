// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.Pmull.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public sealed partial class SimdCapabilitiesTests
{
    /// <summary>
    /// Verifies that an ARM64 process whose processor has the cryptography extension opens the polynomial-multiply
    /// gate, so a run on such hardware exercises the PMULL GHASH kernel rather than the scalar one.
    /// </summary>
    [TestMethod]
    public void Pmull_WhenProcessorHasTheCryptographyExtension_ShouldBeEnabled()
    {
        if (!System.Runtime.Intrinsics.Arm.Aes.IsSupported)
            Assert.Inconclusive("The processor does not have the ARMv8 cryptography extension.");

        Assert.IsTrue(SimdCapabilities.Pmull);
    }

    /// <summary>
    /// Verifies that a process on any architecture other than ARM64 keeps the polynomial-multiply gate closed.
    /// </summary>
    [TestMethod]
    public void Pmull_WhenProcessIsNotArm64_ShouldBeDisabled()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            Assert.Inconclusive("The process is running on ARM64.");

        Assert.IsFalse(SimdCapabilities.Pmull);
    }
}
