// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdOptOutTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the AVX-512 opt-out. This assembly sets the <c>Bodu.Security.Cryptography.DisableSimd</c> feature switch via
/// its <c>runtimeconfig.template.json</c>, so every test here runs with SIMD dispatch forced off; the accelerated
/// primitives must therefore fall back to their scalar reference paths and still produce the published digests.
/// </summary>
[TestClass]
public sealed class SimdOptOutTests
{
    /// <summary>
    /// Verifies that with the disable switch set, the SIMD capability gates report unavailable regardless of the host's
    /// hardware — the switch overrides the AVX-512 intrinsic check.
    /// </summary>
    [TestMethod]
    public void SimdCapabilities_WhenDisableSwitchSet_ShouldReportGatesDisabled()
    {
        Assert.IsFalse(SimdCapabilities.Avx512F, "Expected the disable switch to force Avx512F off.");
        Assert.IsFalse(SimdCapabilities.Avx512FVL, "Expected the disable switch to force Avx512FVL off.");
    }

    /// <summary>
    /// Verifies that BLAKE3 — one of the AVX-512-accelerated primitives — reproduces the official empty-input reference
    /// digest when SIMD is disabled, exercising the scalar fallback.
    /// </summary>
    [TestMethod]
    public void Blake3_WhenSimdDisabled_ShouldReproduceReferenceDigest()
    {
        using var hasher = new Blake3();

        byte[] digest = hasher.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(
            "af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262",
            Convert.ToHexString(digest).ToLowerInvariant());
    }
}
