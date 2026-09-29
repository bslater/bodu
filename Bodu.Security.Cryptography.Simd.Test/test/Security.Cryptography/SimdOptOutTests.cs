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
        Assert.IsFalse(SimdCapabilities.Sse2, "Expected the disable switch to force Sse2 off.");
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
    /// Verifies that with SIMD disabled, BLAKE2b and BLAKE2s dispatch to their scalar compression kernels whatever the
    /// processor supports, so the linked BLAKE2 vectors in this assembly hold the scalar kernels to them.
    /// </summary>
    [TestMethod]
    public void Blake2SelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernels()
    {
        Assert.AreEqual(Blake2bCore.KernelKind.Scalar, Blake2bCore.SelectKernel());
        Assert.AreEqual(Blake2sCore.KernelKind.Scalar, Blake2sCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, BLAKE3 dispatches to its scalar compression kernel whatever the processor
    /// supports, so the linked BLAKE3 vectors in this assembly hold the scalar kernel to them.
    /// </summary>
    [TestMethod]
    public void Blake3CoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(Blake3Core.KernelKind.Scalar, Blake3Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, ChaCha20 and Salsa20, which share the dispatch, take their scalar block
    /// functions whatever the processor supports.
    /// </summary>
    [TestMethod]
    public void ChaCha20CoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(ChaCha20Core.KernelKind.Scalar, ChaCha20Core.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, Poly1305 absorbs every run of whole blocks, however long, through its scalar
    /// loop, whatever the processor supports.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    [TestMethod]
    [DataRow(Poly1305Core.Avx2MinimumBytes)]
    [DataRow(Poly1305Core.Avx2PairedMinimumBytes)]
    [DataRow(Poly1305Core.Avx512MinimumBytes)]
    [DataRow(1 << 20)]
    public void Poly1305CoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarLoop(int length)
    {
        Assert.AreEqual(Poly1305Core.KernelKind.Scalar, Poly1305Core.SelectKernel(length));
    }

    /// <summary>
    /// Verifies that with SIMD disabled, Serpent-128 encrypts and decrypts runs of blocks with its scalar rounds, one
    /// block at a time, whatever the processor supports.
    /// </summary>
    [TestMethod]
    public void SerpentCoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(SerpentCore.KernelKind.Scalar, SerpentCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, scrypt dispatches to its scalar BlockMix kernel whatever the processor supports.
    /// </summary>
    [TestMethod]
    public void ScryptCoreSelectKernel_WhenSimdDisabled_ShouldReturnTheScalarKernel()
    {
        Assert.AreEqual(ScryptCore.KernelKind.Scalar, ScryptCore.SelectKernel());
    }

    /// <summary>
    /// Verifies that with SIMD disabled, scrypt — mixing through the scalar kernel, on one thread and on several —
    /// still reproduces RFC 7914, Section 12's first two vectors.
    /// </summary>
    [TestMethod]
    public void ScryptDeriveKey_WhenSimdDisabled_ShouldMatchRfc7914Vectors()
    {
        byte[] empty = Scrypt.DeriveKey([], [], 16, 1, 1, 64);
        byte[] nacl = new Scrypt(1024, 8, 16, maxDegreeOfParallelism: 4).GetBytes("password"u8, "NaCl"u8, 64);

        Assert.AreEqual(
            "77d6576238657b203b19ca42c18a0497f16b4844e3074ae8dfdffa3fede21442" +
            "fcd0069ded0948f8326a753a0fc81f17e8d3e0fb2e0d3628cf35e20c38d18906",
            Convert.ToHexString(empty).ToLowerInvariant());
        Assert.AreEqual(
            "fdbabe1c9d3472007856e7190d01e9fe7c6ad7cbc8237830e77376634b373162" +
            "2eaf30d92e22a3886ff109279d9830dac727afb94a83ee6d8360cbdfa2cc0640",
            Convert.ToHexString(nacl).ToLowerInvariant());
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
    /// Verifies that with SIMD disabled, one GHASH step through the dispatched kernel still reproduces the documented GCM
    /// test case 2 product <c>C₁ · H</c> (NIST SP 800-38D), confirming the scalar kernel the switch selects is correct.
    /// </summary>
    [TestMethod]
    public void GhashUpdate_WhenSimdDisabled_ShouldReproduceDocumentedGhashProduct()
    {
        var key = Ghash.Key.ForGhash(Convert.FromHexString("66e94bd4ef8a2c3b884cfa59ca342b2e"), Ghash.SelectKernel());
        byte[] state = new byte[16];

        Ghash.Update(in key, state, Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78"));

        CollectionAssert.AreEqual(Convert.FromHexString("5e2ec746917062882c85b0685353deb7"), state,
            "Scalar-kernel GHASH did not match the documented product.");
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
