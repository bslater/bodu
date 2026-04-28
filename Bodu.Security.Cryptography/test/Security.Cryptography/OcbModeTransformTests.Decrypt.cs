// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    // ── Non-default tag length ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a one-bit mutation of the authentication tag causes
    /// <see cref="OcbModeTransform.Decrypt" /> to throw <see cref="CryptographicException" />
    /// when the transform is configured with a non-default tag length.
    /// </summary>
    /// <remarks>
    /// Exercises tag lengths shorter than the default 128 bits to confirm that the
    /// fixed-time tag comparison in <see cref="CryptographicOperations.FixedTimeEquals" />
    /// operates correctly over a truncated tag window (8 or 12 bytes) rather than
    /// the full 16-byte block.
    /// </remarks>
    [TestMethod]
    [DataRow(8, DisplayName = "tagLen = 8")]
    [DataRow(12, DisplayName = "tagLen = 12")]
    public void Decrypt_WithNonDefaultTagLength_WhenTagTampered_ShouldThrowCryptographicException(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var enc = CreateTransform(cipher, (byte[])iv.Clone(), tagLen);
        var ct = new byte[plaintext.Length + tagLen];
        enc.Encrypt(plaintext, ct);
        ct[ct.Length - 1] ^= 0x01;         // flip last byte of tag

        var dec = CreateTransform(cipher, (byte[])iv.Clone(), tagLen);
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[plaintext.Length]),
            $"A tampered tag must always be rejected (tagLen = {tagLen}).");
    }

    /// <summary>
    /// Verifies that decrypting a ciphertext produced with <c>tagLen = 16</c> using a
    /// transform configured with <c>tagLen = 12</c> throws <see cref="CryptographicException" />.
    /// </summary>
    /// <remarks>
    /// When the tag lengths are mismatched the ciphertext/tag boundary is shifted: four
    /// real ciphertext bytes are absorbed into the 12-byte received-tag window, corrupting
    /// the plaintext that feeds the checksum and causing the recomputed tag to differ from
    /// the received tag.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenTagLengthMismatch_ShouldThrowCryptographicException()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var enc = CreateTransform(cipher, (byte[])iv.Clone(), tagLen: 16);
        var ct = new byte[plaintext.Length + 16];
        enc.Encrypt(plaintext, ct);

        var dec = CreateTransform(cipher, (byte[])iv.Clone(), tagLen: 12);
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[ct.Length - 12]),
            "Decrypting with a mismatched tagLen must fail tag verification.");
    }

    // ── Tamper detection (AES cipher) ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a one-byte mutation of any ciphertext byte causes
    /// <see cref="OcbModeTransform.Decrypt" /> to throw <see cref="CryptographicException" />.
    /// </summary>
    /// <remarks>
    /// Any alteration to the ciphertext produces incorrect plaintext during decryption,
    /// which changes the checksum accumulated across the plaintext blocks. The recomputed
    /// tag therefore diverges from the received tag and verification fails.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenCiphertextIsTampered_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
        var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        var plaintext = CryptoTestUtilities.CreateIncrementalByteSequence(0, ExpectedBlockSize);

        var enc = CreateTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);
        ct[0] ^= 0xFF;

        var dec = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[plaintext.Length]));
    }

    /// <summary>
    /// Verifies that a one-bit mutation of the authentication tag causes
    /// <see cref="OcbModeTransform.Decrypt" /> to throw <see cref="CryptographicException" />.
    /// </summary>
    /// <remarks>
    /// The tag comparison is performed with <see cref="CryptographicOperations.FixedTimeEquals" />
    /// to prevent timing side-channels. This test confirms that even a single-bit difference
    /// in the received tag is detected, regardless of the plaintext content.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var enc = CreateTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);
        ct[ct.Length - 1] ^= 0x01;

        var dec = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[plaintext.Length]));
    }

    /// <summary>
    /// Verifies that supplying different associated data during decryption than was
    /// used during encryption causes <see cref="OcbModeTransform.Decrypt" /> to throw
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <remarks>
    /// In OCB3 the associated data is authenticated but not encrypted via the
    /// <c>HASH(K, A)</c> function defined in RFC 7253 §4. Any change to the AAD alters
    /// the HASH result, which XORs directly into the authentication tag. The recomputed
    /// tag therefore diverges from the received tag even when the ciphertext itself is
    /// unmodified.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenAadIsTampered_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
        var iv = new byte[ExpectedBlockSize];
        var aad = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var plaintext = CryptoTestUtilities.CreateIncrementalByteSequence(0, ExpectedBlockSize);

        var enc = CreateTransform(cipher, (byte[])iv.Clone());
        enc.ProcessAssociatedData(aad);
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var badAad = (byte[])aad.Clone();
        badAad[0] ^= 0xFF;
        var dec = CreateTransform(cipher, (byte[])iv.Clone());
        dec.ProcessAssociatedData(badAad);
        Assert.ThrowsExactly<CryptographicException>(() =>
            dec.Decrypt(ct, new byte[plaintext.Length]));
    }

    // ── Security properties ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Decrypt" /> zeroes the entire output buffer
    /// before throwing <see cref="CryptographicException" /> when authentication fails.
    /// </summary>
    /// <remarks>
    /// Releasing unverified plaintext to the caller is a well-known AEAD security failure
    /// mode. The implementation must call <see cref="CryptographicOperations.ZeroMemory" />
    /// on the output span before propagating the exception, so that any partially decrypted
    /// bytes cannot leak through the output array even if the caller catches the exception
    /// and inspects the buffer.
    /// </remarks>
    [TestMethod]
    public void Decrypt_OnAuthenticationFailure_ShouldZeroOutputBuffer()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize * 2 + 5]; // multi-block with partial

        var enc = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);
        ct[0] ^= 0xFF;          // corrupt the first ciphertext byte

        var output = new byte[plaintext.Length];
        Array.Fill(output, (byte)0xCC);     // sentinel: any non-zero value

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() => dec.Decrypt(ct, output));

        CollectionAssert.AreEqual(
            new byte[plaintext.Length],
            output,
            "Output buffer must be completely zeroed after authentication failure " +
            "(CryptographicOperations.ZeroMemory).");
    }

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
        var ct = new byte[ptLen + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        int written = dec.Decrypt(ct, new byte[ptLen]);

        Assert.AreEqual(ptLen, written,
            $"Decrypt must return the plaintext byte count ({ptLen}) as the written value.");
    }
}