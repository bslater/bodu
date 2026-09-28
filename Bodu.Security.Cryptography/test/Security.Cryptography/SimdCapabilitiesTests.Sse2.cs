// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.Sse2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public sealed partial class SimdCapabilitiesTests
{
    /// <summary>
    /// Verifies that an x64 process opens the SSE2 gate, which belongs to the x64 baseline, so a run on any x64 hardware
    /// exercises the 128-bit scrypt kernel rather than the scalar one.
    /// </summary>
    [TestMethod]
    public void Sse2_WhenProcessIsX64_ShouldBeEnabled()
    {
        if (!System.Runtime.Intrinsics.X86.Sse2.IsSupported)
            Assert.Inconclusive("The process is not running on x64.");

        Assert.IsTrue(SimdCapabilities.Sse2);
    }

    /// <summary>
    /// Verifies that an ARM64 process keeps the SSE2 gate closed.
    /// </summary>
    [TestMethod]
    public void Sse2_WhenProcessIsArm64_ShouldBeDisabled()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            Assert.Inconclusive("The process is not running on ARM64.");

        Assert.IsFalse(SimdCapabilities.Sse2);
    }
}
