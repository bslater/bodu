// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.Lifecycle.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

/// <summary>
/// Mode-specific lifecycle tests for <see cref="GcmSivModeTransform" /> (RFC 8452). The shared
/// <see cref="AeadBlockCipherModeTests{TTest, TTransform}" /> base verifies the generic AEAD
/// lifecycle; this file pins down the property that distinguishes GCM-SIV from GCM —
/// <strong>nonce-misuse resistance</strong>. Re-encrypting the same <c>(key, nonce, AAD,
/// plaintext)</c> tuple must produce identical ciphertext (revealing only message equality);
/// distinct plaintexts under the same nonce must remain confidential and authentic.
/// </summary>
public sealed partial class GcmSivModeTransformTests
{
    /// <summary>
    /// Verifies that encrypting the same <c>(key, nonce, AAD, plaintext)</c> tuple twice on two
    /// independently-constructed instances yields identical ciphertext. RFC 8452's misuse-resistance
    /// contract requires the algorithm to be deterministic: re-issuing the same nonce after a crash
    /// or replication retry must not corrupt confidentiality beyond confirming message equality.
    /// </summary>
    [TestMethod]
    public void Encrypt_SameKeyNonceAadAndPlaintext_ShouldProduceIdenticalOutput()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] aad = [0xAA, 0xBB, 0xCC];
        var iv = new byte[16];
        for (var i = 0; i < iv.Length; i++) iv[i] = (byte)i;

        using GcmSivModeTransform transformA = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformA.ProcessAssociatedData(aad);
        var outA = new byte[plaintext.Length + (transformA.TagSize / 8)];
        transformA.Encrypt(plaintext, outA);

        using GcmSivModeTransform transformB = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformB.ProcessAssociatedData(aad);
        var outB = new byte[plaintext.Length + (transformB.TagSize / 8)];
        transformB.Encrypt(plaintext, outB);

        CollectionAssert.AreEqual(outA, outB,
            "GCM-SIV must be deterministic for the same key, nonce, AAD, and plaintext.");
    }

    /// <summary>
    /// Verifies that two distinct plaintexts encrypted under the same key + nonce + AAD produce
    /// different ciphertexts — the misuse-resistance contract leaks only message equality, not
    /// message content. Confirms the keystream is not trivially fixed when the nonce repeats.
    /// </summary>
    [TestMethod]
    public void Encrypt_DistinctPlaintextsUnderSameNonce_ShouldProduceDistinctOutputs()
    {
        var iv = new byte[16];
        byte[] aad = [0xAA, 0xBB];

        byte[] plaintextA = [0x10, 0x20, 0x30, 0x40];
        byte[] plaintextB = [0xFF, 0xEE, 0xDD, 0xCC];

        using GcmSivModeTransform transformA = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformA.ProcessAssociatedData(aad);
        var outA = new byte[plaintextA.Length + (transformA.TagSize / 8)];
        transformA.Encrypt(plaintextA, outA);

        using GcmSivModeTransform transformB = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformB.ProcessAssociatedData(aad);
        var outB = new byte[plaintextB.Length + (transformB.TagSize / 8)];
        transformB.Encrypt(plaintextB, outB);

        CollectionAssert.AreNotEqual(outA, outB,
            "Distinct plaintexts under the same nonce must remain distinct in GCM-SIV.");
    }

    /// <summary>
    /// Verifies that encrypting the same plaintext under different AAD streams produces different
    /// outputs — AAD is bound into the POLYVAL accumulator that derives the synthetic tag, so the
    /// tag and (via tag-as-counter-base) the keystream differ.
    /// </summary>
    [TestMethod]
    public void Encrypt_SamePlaintextDifferentAad_ShouldProduceDistinctOutputs()
    {
        byte[] plaintext = [0xDE, 0xAD, 0xBE, 0xEF];
        var iv = new byte[16];

        using GcmSivModeTransform transformA = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformA.ProcessAssociatedData([0x01]);
        var outA = new byte[plaintext.Length + (transformA.TagSize / 8)];
        transformA.Encrypt(plaintext, outA);

        using GcmSivModeTransform transformB = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA), iv);
        transformB.ProcessAssociatedData([0x02]);
        var outB = new byte[plaintext.Length + (transformB.TagSize / 8)];
        transformB.Encrypt(plaintext, outB);

        CollectionAssert.AreNotEqual(outA, outB,
            "Distinct AAD streams must produce distinct GCM-SIV outputs (POLYVAL binds AAD into the tag).");
    }
}
