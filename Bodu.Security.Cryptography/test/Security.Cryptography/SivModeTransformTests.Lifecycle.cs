// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.Lifecycle.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Mode-specific lifecycle tests for <see cref="SivModeTransform" />. The shared
/// <see cref="AeadBlockCipherModeTests{TTest, TTransform}" /> base verifies the generic
/// AEAD contract (lifecycle bookkeeping, tag-mismatch zeroing, AAD ordering); this file
/// pins down the property that distinguishes SIV from every other AEAD in the library —
/// <strong>deterministic IV derivation</strong>. RFC 5297 specifies that the synthetic IV
/// is computed from the key, AAD, and plaintext via S2V/CMAC, so re-encrypting the same
/// triple under any user-supplied IV must yield identical ciphertext.
/// </summary>
public sealed partial class SivModeTransformTests
{
    /// <summary>
    /// Verifies that encrypting the same <c>(plaintext, AAD)</c> pair twice with two different
    /// user-supplied IVs produces identical ciphertext. The synthetic IV is derived from the
    /// data, so the user-supplied IV is ignored — the defining contract of AES-SIV.
    /// </summary>
    [TestMethod]
    public void Encrypt_SamePlaintextAndAad_DifferentSuppliedIVs_ShouldProduceIdenticalOutput()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] aad = [0xAA, 0xBB, 0xCC];

        byte[] ivAlpha = Convert.FromHexString("00000000000000000000000000000000");
        byte[] ivBeta = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

        using SivModeTransform transformAlpha = CreateTransform(cipher: null!, ivAlpha);
        transformAlpha.ProcessAssociatedData(aad);
        byte[] alphaOut = new byte[plaintext.Length + transformAlpha.TagSize];
        transformAlpha.Encrypt(plaintext, alphaOut);

        using SivModeTransform transformBeta = CreateTransform(cipher: null!, ivBeta);
        transformBeta.ProcessAssociatedData(aad);
        byte[] betaOut = new byte[plaintext.Length + transformBeta.TagSize];
        transformBeta.Encrypt(plaintext, betaOut);

        CollectionAssert.AreEqual(alphaOut, betaOut,
            "AES-SIV must derive a synthetic IV from the data; the user-supplied IV must not influence the ciphertext.");
    }

    /// <summary>
    /// Verifies that two distinct plaintexts encrypted under the same supplied IV produce
    /// different ciphertexts — the synthetic IV depends on the message, so distinct messages
    /// yield distinct synthetic IVs and therefore distinct ciphertext + tag pairs.
    /// </summary>
    [TestMethod]
    public void Encrypt_DifferentPlaintexts_SameSuppliedIV_ShouldProduceDifferentOutputs()
    {
        byte[] iv = new byte[16];
        byte[] aad = [0xAA, 0xBB];

        byte[] plaintextA = [0x10, 0x20, 0x30, 0x40];
        byte[] plaintextB = [0xFF, 0xEE, 0xDD, 0xCC];

        using SivModeTransform transformA = CreateTransform(cipher: null!, iv);
        transformA.ProcessAssociatedData(aad);
        byte[] outA = new byte[plaintextA.Length + transformA.TagSize];
        transformA.Encrypt(plaintextA, outA);

        using SivModeTransform transformB = CreateTransform(cipher: null!, iv);
        transformB.ProcessAssociatedData(aad);
        byte[] outB = new byte[plaintextB.Length + transformB.TagSize];
        transformB.Encrypt(plaintextB, outB);

        CollectionAssert.AreNotEqual(outA, outB,
            "Distinct plaintexts must produce distinct AES-SIV outputs (the synthetic IV depends on the message).");
    }

    /// <summary>
    /// Verifies that encrypting the same plaintext under two different AAD streams produces
    /// different ciphertexts — AAD is one of the inputs to S2V and so changes the synthetic IV.
    /// </summary>
    [TestMethod]
    public void Encrypt_DifferentAad_SamePlaintextAndIV_ShouldProduceDifferentOutputs()
    {
        byte[] plaintext = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[] iv = new byte[16];

        using SivModeTransform transformA = CreateTransform(cipher: null!, iv);
        transformA.ProcessAssociatedData([0x01]);
        byte[] outA = new byte[plaintext.Length + transformA.TagSize];
        transformA.Encrypt(plaintext, outA);

        using SivModeTransform transformB = CreateTransform(cipher: null!, iv);
        transformB.ProcessAssociatedData([0x02]);
        byte[] outB = new byte[plaintext.Length + transformB.TagSize];
        transformB.Encrypt(plaintext, outB);

        CollectionAssert.AreNotEqual(outA, outB,
            "Distinct AAD streams must produce distinct AES-SIV outputs (S2V mixes AAD into the synthetic IV).");
    }
}
