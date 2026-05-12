// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

public sealed partial class OcbModeTransformTests
{
    // ── Output length — non-default tag sizes ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Encrypt" /> writes exactly
    /// <c>|PT| + (tagSize / 8)</c> bytes to the output buffer when a non-default tag size
    /// is used, and that the return value equals the same quantity.
    /// </summary>
    [TestMethod]
    [DataRow(64, DisplayName = "tagSize = 64 bits")]
    [DataRow(96, DisplayName = "tagSize = 96 bits")]
    [DataRow(128, DisplayName = "tagSize = 128 bits")]
    public void Encrypt_WithGivenTagSize_OutputShouldBePlaintextLengthPlusTagBytes(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var plaintext = new byte[ExpectedBlockSize];
        OcbModeTransform transform = CreateTransform(cipher, new byte[ExpectedBlockSize], tagSize);
        var tagBytes = tagSize / 8;
        var output = new byte[plaintext.Length + tagBytes];

        var written = transform.Encrypt(plaintext, output);

        Assert.AreEqual(plaintext.Length + tagBytes, written,
            $"Encrypt must return |PT| + (tagSize / 8) bytes (tagSize = {tagSize} bits).");
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

        OcbModeTransform enc16 = CreateTransform(cipher16, (byte[])iv.Clone(), 128);
        OcbModeTransform enc12 = CreateTransform(cipher12, (byte[])iv.Clone(), 96);
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

    // ── Key sensitivity ───────────────────────────────────────────────────────────────────────

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
        var ct1 = new byte[plaintext.Length + (enc1.TagSize / 8)];
        enc1.Encrypt(plaintext, ct1);

        var enc2 = new OcbModeTransform(cipher2, iv);
        var ct2 = new byte[plaintext.Length + (enc2.TagSize / 8)];
        enc2.Encrypt(plaintext, ct2);

        CollectionAssert.AreNotEqual(ct1, ct2,
            "Different keys must produce different ciphertext and tag.");
    }
}
