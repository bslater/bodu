// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdOptOutTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the SIMD opt-out. This assembly sets the <c>Bodu.Security.Cryptography.DisableSimd</c> feature switch via
/// its <c>runtimeconfig.template.json</c>, so every test here runs with SIMD dispatch forced off; the accelerated
/// primitives must therefore fall back to their scalar reference paths and still produce the published digests.
/// </summary>
[TestClass]
public sealed class SimdOptOutTests
{
    /// <summary>
    /// Verifies that with the disable switch set, the SIMD capability gates report unavailable regardless of the host's
    /// hardware — the switch overrides every intrinsic check.
    /// </summary>
    [TestMethod]
    public void SimdCapabilities_WhenDisableSwitchSet_ShouldReportGatesDisabled()
    {
        Assert.IsFalse(SimdCapabilities.Avx512F, "Expected the disable switch to force Avx512F off.");
        Assert.IsFalse(SimdCapabilities.Avx512FVL, "Expected the disable switch to force Avx512FVL off.");
        Assert.IsFalse(SimdCapabilities.Pclmulqdq, "Expected the disable switch to force the carry-less GHASH gate off.");
        Assert.IsFalse(SimdCapabilities.Avx2, "Expected the disable switch to force Avx2 off.");
        Assert.IsFalse(SimdCapabilities.Ssse3, "Expected the disable switch to force Ssse3 off.");
        Assert.IsFalse(SimdCapabilities.AdvSimd, "Expected the disable switch to force AdvSimd off.");
        Assert.IsFalse(SimdCapabilities.Pmull, "Expected the disable switch to force the polynomial-multiply GHASH gate off.");
    }

    /// <summary>
    /// Verifies that with SIMD disabled, Argon2 dispatches to its scalar compression kernel whatever the processor
    /// supports, so the linked Argon2 vectors in this assembly hold the scalar kernel to them.
    /// </summary>
    [TestMethod]
    public void Argon2CoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(Argon2Core.KernelKind.Scalar, Argon2Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, GHASH and POLYVAL keys are prepared for the scalar kernel whatever the processor
    /// supports, so every consumer of the module runs that kernel in this assembly.
    /// </summary>
    [TestMethod]
    public void GhashSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(Ghash.KernelKind.Scalar, Ghash.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, GCM — hashing through the scalar GHASH kernel — still matches the platform's
    /// <see cref="System.Security.Cryptography.AesGcm" /> on messages that fill several four-block groups and end in a
    /// partial block, with associated data of several alignments.
    /// </summary>
    [TestMethod]
    public void GcmModeTransformEncrypt_WhenSimdDisabled_ShouldMatchPlatformAesGcm()
    {
        if (!System.Security.Cryptography.AesGcm.IsSupported)
            Assert.Inconclusive("AesGcm is not supported on this platform.");

        byte[] key = RandomBytes(16, 1);
        byte[] nonce = RandomBytes(12, 2);
        using var platform = new System.Security.Cryptography.AesGcm(key, 16);
        using var cipher = new AesBlockCipher(key);

        foreach (int length in new[] { 0, 17, 4097, 20000 })
        {
            foreach (int aadLength in new[] { 0, 17, 100 })
            {
                byte[] plaintext = RandomBytes(length, length + 3);
                byte[] aad = RandomBytes(aadLength, aadLength + 5);
                byte[] expected = new byte[length + 16];
                platform.Encrypt(nonce, plaintext, expected.AsSpan(0, length), expected.AsSpan(length), aad);

                using var transform = new GcmModeTransform(cipher, nonce);
                transform.ProcessAssociatedData(aad);
                byte[] actual = new byte[length + 16];
                transform.Encrypt(plaintext, actual);

                CollectionAssert.AreEqual(expected, actual, $"{length} bytes, {aadLength} bytes of AAD");
            }
        }
    }

    /// <summary>
    /// Verifies that with SIMD disabled, <see cref="GaloisField128.Multiply" /> falls back to the scalar reference and
    /// still reproduces the documented GCM Test Case 2 GHASH product <c>C₁ · H</c> (NIST SP 800-38D), confirming the
    /// carry-less path's scalar fallback is correct.
    /// </summary>
    [TestMethod]
    public void GaloisField128Multiply_WhenSimdDisabled_ShouldReproduceDocumentedGhashProduct()
    {
        byte[] c1 = Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78");
        byte[] h = Convert.FromHexString("66e94bd4ef8a2c3b884cfa59ca342b2e");
        byte[] expected = Convert.FromHexString("5e2ec746917062882c85b0685353deb7");

        Span<byte> actual = stackalloc byte[16];
        GaloisField128.Multiply(c1, h, actual);

        CollectionAssert.AreEqual(expected, actual.ToArray(),
            "Scalar-fallback GHASH multiply did not match the documented product.");
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

    /// <summary>
    /// Returns a deterministic pseudo-random buffer.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] RandomBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
