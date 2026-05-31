// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.Lifecycle.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

/// <summary>
/// Mode-specific lifecycle tests for <see cref="CcmModeTransform" />. The shared
/// <see cref="AeadBlockCipherModeTests{TTest, TTransform}" /> base verifies the generic AEAD
/// lifecycle; this file pins down properties specific to NIST SP 800-38C — the
/// <c>(N, q, T)</c> parameter triple is hard-coded at <c>(12, 3, 16)</c>, and the resulting
/// 16-byte tag length is the documented <see cref="IAeadBlockCipherModeTransform.TagSize" />.
/// </summary>
public sealed partial class CcmModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.TagSize" /> is exactly 128 bits (16 bytes) — the
    /// fixed tag length this implementation produces (NIST SP 800-38C Appendix A profile <c>M = 16</c>).
    /// </summary>
    [TestMethod]
    public void TagSize_ShouldBe128Bits()
    {
        using CcmModeTransform transform = MakeTransform();

        Assert.AreEqual(128, transform.TagSize);
    }

    /// <summary>
    /// Verifies that two encryptions of the same <c>(plaintext, AAD)</c> under the same nonce
    /// produce identical ciphertext — CCM is fully deterministic given fixed inputs. This is a
    /// regression guard for any accidental injection of entropy (per-instance nonces, per-call
    /// salts, etc.) into the CTR or CBC-MAC pipelines.
    /// </summary>
    [TestMethod]
    public void Encrypt_SamePlaintextSameNonceSameAad_ShouldBeDeterministic()
    {
        var iv = new byte[16];
        for (var i = 0; i < iv.Length; i++) iv[i] = (byte)i;

        byte[] aad = [0xA1, 0xA2, 0xA3];
        byte[] plaintext = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80];

        using CcmModeTransform transformA = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformA.ProcessAssociatedData(aad);
        var outA = new byte[plaintext.Length + (transformA.TagSize / 8)];
        transformA.Encrypt(plaintext, outA);

        using CcmModeTransform transformB = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformB.ProcessAssociatedData(aad);
        var outB = new byte[plaintext.Length + (transformB.TagSize / 8)];
        transformB.Encrypt(plaintext, outB);

        CollectionAssert.AreEqual(outA, outB,
            "CCM must be fully deterministic for the same key, nonce, AAD, and plaintext.");
    }

    /// <summary>
    /// Verifies that swapping AAD between two encryptions of the same <c>(nonce, plaintext)</c>
    /// produces different ciphertext+tag pairs — confirms the CBC-MAC chain folds AAD into the
    /// tag rather than treating it as a no-op.
    /// </summary>
    [TestMethod]
    public void Encrypt_SamePlaintextSameNonceDifferentAad_ShouldProduceDistinctTags()
    {
        var iv = new byte[16];
        byte[] plaintext = [0xDE, 0xAD, 0xBE, 0xEF];

        using CcmModeTransform transformA = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformA.ProcessAssociatedData([0x01]);
        var outA = new byte[plaintext.Length + (transformA.TagSize / 8)];
        transformA.Encrypt(plaintext, outA);

        using CcmModeTransform transformB = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformB.ProcessAssociatedData([0x02]);
        var outB = new byte[plaintext.Length + (transformB.TagSize / 8)];
        transformB.Encrypt(plaintext, outB);

        CollectionAssert.AreNotEqual(outA, outB,
            "CCM must bind AAD into the CBC-MAC tag — distinct AAD must produce distinct outputs.");
    }
}
