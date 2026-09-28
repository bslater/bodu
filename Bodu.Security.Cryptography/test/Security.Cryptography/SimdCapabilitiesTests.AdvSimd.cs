// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public sealed partial class SimdCapabilitiesTests
{
    /// <summary>
    /// Verifies that an ARM64 process opens the AdvSimd gate, so a run on ARM64 hardware exercises the AdvSimd kernels
    /// rather than silently falling back to the scalar ones.
    /// </summary>
    [TestMethod]
    public void AdvSimd_WhenProcessIsArm64_ShouldBeEnabled()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            Assert.Inconclusive("The process is not running on ARM64.");

        Assert.IsTrue(SimdCapabilities.AdvSimd);
    }

    /// <summary>
    /// Verifies that a process on any other architecture keeps the AdvSimd gate closed.
    /// </summary>
    [TestMethod]
    public void AdvSimd_WhenProcessIsNotArm64_ShouldBeDisabled()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            Assert.Inconclusive("The process is running on ARM64.");

        Assert.IsFalse(SimdCapabilities.AdvSimd);
    }
}
