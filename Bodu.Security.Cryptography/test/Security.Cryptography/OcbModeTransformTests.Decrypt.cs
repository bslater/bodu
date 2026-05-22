// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public sealed partial class OcbModeTransformTests
{
    // ── Non-default tag length ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a one-bit mutation of the authentication tag causes
    /// <see cref="OcbModeTransform.Decrypt" /> to throw <see cref="CryptographicException" />
    /// when the transform is configured with a non-default tag size.
    /// </summary>
    /// <remarks>
    /// Exercises tag sizes shorter than the default 128 bits to confirm that the
    /// fixed-time tag comparison in <see cref="CryptographicOperations.FixedTimeEquals" />
    /// operates correctly over a truncated tag window (64 or 96 bits / 8 or 12 bytes)
    /// rather than the full 128-bit (16-byte) block.
    /// </remarks>
    [TestMethod]
    [DataRow(64, DisplayName = "tagSize = 64 bits")]
    [DataRow(96, DisplayName = "tagSize = 96 bits")]
    public void Decrypt_WithNonDefaultTagSize_WhenTagTampered_ShouldThrowExactly(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var tagBytes = tagSize / 8;
        OcbModeTransform enc = CreateTransform(cipher, (byte[])iv.Clone(), tagSize);
        var ct = new byte[plaintext.Length + tagBytes];
        enc.Encrypt(plaintext, ct);
        ct[ct.Length - 1] ^= 0x01;         // flip last byte of tag

        OcbModeTransform dec = CreateTransform(cipher, (byte[])iv.Clone(), tagSize);
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[plaintext.Length]),
            $"A tampered tag must always be rejected (tagSize = {tagSize} bits).");
    }

    /// <summary>
    /// Verifies that decrypting a ciphertext produced with <c>tagSize = 128</c> bits using a
    /// transform configured with <c>tagSize = 96</c> bits throws <see cref="CryptographicException" />.
    /// </summary>
    /// <remarks>
    /// When the tag sizes are mismatched the ciphertext/tag boundary is shifted: four
    /// real ciphertext bytes are absorbed into the 12-byte received-tag window, corrupting
    /// the plaintext that feeds the checksum and causing the recomputed tag to differ from
    /// the received tag.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenTagSizeMismatch_ShouldThrowExactly()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        OcbModeTransform enc = CreateTransform(cipher, (byte[])iv.Clone(), tagSize: 128);
        var ct = new byte[plaintext.Length + 16];
        enc.Encrypt(plaintext, ct);

        OcbModeTransform dec = CreateTransform(cipher, (byte[])iv.Clone(), tagSize: 96);
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[ct.Length - 12]),
            "Decrypting with a mismatched tagSize must fail tag verification.");
    }

    // ── Security properties ───────────────────────────────────────────────────────────────────

    // Decrypt_OnAuthenticationFailure_ShouldZeroOutputBuffer is now in AeadBlockCipherModeTests.Decrypt.cs.

    // ── Return value ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Decrypt" /> returns exactly
    /// <c>ciphertextWithTag.Length - TagSize</c> as the number of plaintext bytes written,
    /// for a range of plaintext lengths spanning empty, partial-block, and multi-block inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0, DisplayName = "0 bytes  — empty plaintext")]
    [DataRow(7, DisplayName = "7 bytes  — sub-block partial")]
    [DataRow(16, DisplayName = "16 bytes — exactly one full block")]
    [DataRow(23, DisplayName = "23 bytes — full block + 7-byte partial")]
    [DataRow(32, DisplayName = "32 bytes — exactly two full blocks")]
    public void Decrypt_WithValidCiphertext_ShouldReturnPlaintextLengthAsWrittenCount(int ptLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ptLen];

        var enc = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[ptLen + (enc.TagSize / 8)];
        enc.Encrypt(plaintext, ct);

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var written = dec.Decrypt(ct, new byte[ptLen]);

        Assert.AreEqual(ptLen, written,
            $"Decrypt must return the plaintext byte count ({ptLen}) as the written value.");
    }
}
