// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using Bodu.Testing.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

public sealed partial class OcbModeTransformTests
{
    // ── Output length ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Encrypt" /> writes exactly
    /// <c>|PT| + tagLen</c> bytes to the output buffer when a non-default tag length
    /// is used, and that the return value equals the same quantity.
    /// </summary>
    [TestMethod]
    [DataRow(8, DisplayName = "tagLen = 8")]
    [DataRow(12, DisplayName = "tagLen = 12")]
    [DataRow(16, DisplayName = "tagLen = 16")]
    public void Encrypt_WithGivenTagLength_OutputShouldBePlaintextLengthPlusTagLen(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var plaintext = new byte[ExpectedBlockSize];
        var transform = CreateTransform(cipher, new byte[ExpectedBlockSize], tagLen);
        var output = new byte[plaintext.Length + tagLen];

        int written = transform.Encrypt(plaintext, output);

        Assert.AreEqual(plaintext.Length + tagLen, written,
            $"Encrypt must return |PT| + tagLen bytes (tagLen = {tagLen}).");
    }

    /// <summary>
    /// Verifies that the value returned by <see cref="OcbModeTransform.Encrypt" /> equals
    /// <c>|PT| + TagSize</c> for a multi-block plaintext using the default tag length.
    /// </summary>
    [TestMethod]
    public void Encrypt_OutputLength_ShouldBePlaintextLengthPlusTagSize()
    {
        var enc = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00), new byte[ExpectedBlockSize]);
        var pt = new byte[ExpectedBlockSize * 3];
        var output = new byte[pt.Length + enc.TagSize];
        int n = enc.Encrypt(pt, output);
        Assert.AreEqual(pt.Length + enc.TagSize, n);
    }

    // ── Domain separation ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that changing the tag length produces completely different ciphertext
    /// and tag, confirming OCB3's intentional domain separation across TAGLEN configurations.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.4 encodes TAGLEN as <c>num2str(TAGLEN mod 128, 7)</c> in the first
    /// seven bits of the nonce word: for TAGLEN=128 byte 0 is <c>0x00</c>; for TAGLEN=96
    /// it is <c>0xC0</c>. The different nonce word produces a different Ktop, Stretch, and
    /// Offset_0, so the entire computation diverges from the first cipher call onwards.
    /// This design prevents a forger from truncating a valid 128-bit ciphertext to produce
    /// a valid 96-bit authentication — the two computations are entirely unrelated.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentTagLengths_ShouldProduceDifferentCiphertextAndTag()
    {
        using var cipher16 = new AesBlockCipherFixture(new byte[16]);
        using var cipher12 = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var enc16 = CreateTransform(cipher16, (byte[])iv.Clone(), 16);
        var enc12 = CreateTransform(cipher12, (byte[])iv.Clone(), 12);
        var ct16 = new byte[plaintext.Length + 16];
        var ct12 = new byte[plaintext.Length + 12];
        enc16.Encrypt(plaintext, ct16);
        enc12.Encrypt(plaintext, ct12);

        CollectionAssert.AreNotEqual(
            ct16.Take(plaintext.Length).ToArray(),
            ct12.Take(plaintext.Length).ToArray(),
            "Different TAGLEN values produce different nonce words and therefore " +
            "different ciphertext (RFC 7253 §2.4 domain separation).");

        CollectionAssert.AreNotEqual(
            ct16.Skip(plaintext.Length).Take(12).ToArray(),
            ct12.Skip(plaintext.Length).Take(12).ToArray(),
            "Tags computed under different TAGLEN values must differ (no prefix relationship).");
    }

    // ── Nonce / key sensitivity ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that encrypting an empty plaintext produces output consisting of exactly
    /// <see cref="OcbModeTransform.TagSize" /> bytes — the authentication tag only, with
    /// no ciphertext bytes preceding it.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.6 defines the tag for empty plaintext as
    /// <c>T = ENCIPHER(K, L_$ XOR Offset_0 XOR zeros(128)) XOR HASH(K, A)</c>, where
    /// Offset_0 is not advanced (no block loop runs) and the checksum is the zero block.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithEmptyPlaintext_ShouldProduceTagOnly()
    {
        var enc = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00), new byte[ExpectedBlockSize]);
        var output = new byte[enc.TagSize];
        int n = enc.Encrypt(ReadOnlySpan<byte>.Empty, output);
        Assert.AreEqual(enc.TagSize, n,
            "Encrypting empty plaintext must write exactly TagSize bytes.");
    }

    /// <summary>
    /// Verifies that two encryptions of the same plaintext under the same key but different
    /// nonces produce different ciphertext, confirming that the nonce uniquely determines
    /// each encryption.
    /// </summary>
    /// <remarks>
    /// The nonce determines Offset_0 via the K_top stretch (RFC 7253 §2.4); any difference
    /// in the nonce propagates into every per-block offset and therefore into every
    /// ciphertext byte. Nonce reuse with the same key would expose plaintext XOR
    /// relationships between two messages.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var plaintext = CryptoTestUtilities.CreateIncrementalByteSequence(0, ExpectedBlockSize);
        var ivA = new byte[ExpectedBlockSize];
        var ivB = new byte[ExpectedBlockSize];
        ivB[0] = 0x01;

        var encA = CreateTransform(cipher, ivA);
        var encB = CreateTransform(cipher, ivB);
        var ctA = new byte[plaintext.Length + encA.TagSize];
        var ctB = new byte[plaintext.Length + encB.TagSize];
        encA.Encrypt(plaintext, ctA);
        encB.Encrypt(plaintext, ctB);

        CollectionAssert.AreNotEqual(ctA, ctB,
            "Different nonces must produce different OCB ciphertext.");
    }

    /// <summary>
    /// Verifies that encrypting the same plaintext and AAD under two different keys produces
    /// different ciphertext and tag.
    /// </summary>
    /// <remarks>
    /// This is a basic key-sensitivity sanity check. Because the L values (<c>L_*</c>,
    /// <c>L_$</c>, <c>L[i]</c>) and the K_top all depend on the key, a different key
    /// changes every intermediate value in both the HASH and the encryption pass.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentKeys_ShouldProduceDifferentOutput()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(Enumerable.Repeat((byte)0xFF, 16).ToArray());
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var enc1 = new OcbModeTransform(cipher1, iv);
        var ct1 = new byte[plaintext.Length + enc1.TagSize];
        enc1.Encrypt(plaintext, ct1);

        var enc2 = new OcbModeTransform(cipher2, iv);
        var ct2 = new byte[plaintext.Length + enc2.TagSize];
        enc2.Encrypt(plaintext, ct2);

        CollectionAssert.AreNotEqual(ct1, ct2,
            "Different keys must produce different ciphertext and tag.");
    }

    // ── Determinism ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Encrypt" /> is deterministic: two independent
    /// instances constructed from the same key and nonce, given the same AAD and plaintext,
    /// must produce identical ciphertext and tag.
    /// </summary>
    /// <remarks>
    /// OCB3 is a deterministic AEAD — unlike nonce-misuse-resistant schemes, it does not
    /// introduce internal randomness. Determinism is required for correctness: the receiver
    /// must be able to reproduce the sender's tag exactly in order to verify it.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithIdenticalInputs_ShouldAlwaysProduceSameOutput()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var aad = new byte[] { 0x01, 0x02, 0x03 };
        var plaintext = new byte[ExpectedBlockSize * 2 + 7]; // multi-block with partial

        var enc1 = new OcbModeTransform(cipher1, (byte[])iv.Clone());
        enc1.ProcessAssociatedData(aad);
        var ct1 = new byte[plaintext.Length + enc1.TagSize];
        enc1.Encrypt(plaintext, ct1);

        var enc2 = new OcbModeTransform(cipher2, (byte[])iv.Clone());
        enc2.ProcessAssociatedData(aad);
        var ct2 = new byte[plaintext.Length + enc2.TagSize];
        enc2.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "OCB3 is deterministic: identical key, nonce, AAD, and plaintext must always " +
            "produce identical ciphertext and tag.");
    }

    // ── AAD equivalence ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that explicitly passing an empty span to
    /// <see cref="OcbModeTransform.ProcessAssociatedData" /> produces the same ciphertext
    /// and tag as never calling <c>ProcessAssociatedData</c> at all.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §4 defines <c>HASH(K, A)</c> of an empty string as <c>zeros(128)</c>.
    /// Whether the caller explicitly supplies an empty span or omits the call (causing
    /// <c>EnsureAadProcessed</c> to default to an empty array), the tag computation
    /// path is identical.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithExplicitEmptyAad_ShouldProduceSameOutputAsNoAadCall()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize * 2 + 5];

        var withEmpty = new OcbModeTransform(cipher1, (byte[])iv.Clone());
        withEmpty.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        var ct1 = new byte[plaintext.Length + withEmpty.TagSize];
        withEmpty.Encrypt(plaintext, ct1);

        var withNone = new OcbModeTransform(cipher2, (byte[])iv.Clone());
        // No ProcessAssociatedData call — EnsureAadProcessed defaults to empty.
        var ct2 = new byte[plaintext.Length + withNone.TagSize];
        withNone.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "ProcessAssociatedData with an empty span must be equivalent to omitting the call.");
    }
}