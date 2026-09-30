// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.AdvSimdSingleState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SimdCapabilitiesTests
{
    /// <summary>
    /// Verifies that the gate on the AdvSimd kernels that spread one state across their lanes stays closed on every
    /// processor, so dispatch runs the scalar kernels, which ran faster on the ARM64 processors measured.
    /// </summary>
    [TestMethod]
    public void AdvSimdSingleState_WhenRead_ShouldBeClosed()
    {
        Assert.IsFalse(SimdCapabilities.AdvSimdSingleState);
    }
}
