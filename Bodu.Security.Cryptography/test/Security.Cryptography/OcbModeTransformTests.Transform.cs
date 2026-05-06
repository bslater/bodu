// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using Bodu.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

public sealed partial class OcbModeTransformTests
{
    // ── Non-default tag length ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a plaintext encrypted with a non-default tag length is fully recovered
    /// by a subsequent decrypt operation using the same key, nonce, and tag length.
    /// </summary>
    /// <remarks>
    /// Exercises tag lengths 8, 12, and 16 bytes to confirm that the tag-length parameter
    /// is consistently applied across both the encrypt and decrypt paths, including the
    /// nonce-word construction (RFC 7253 §2.4), block processing, and tag output or
    /// verification.
    /// </remarks>
    [TestMethod]
    [DataRow(8, DisplayName = "tagLen = 8")]
    [DataRow(12, DisplayName = "tagLen = 12")]
    [DataRow(16, DisplayName = "tagLen = 16")]
    public void EncryptThenDecrypt_WithNonDefaultTagLength_ShouldRoundTrip(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = Enumerable.Repeat((byte)0x5A, ExpectedBlockSize).ToArray();
        var plaintext = TestHelpers.GenerateRandomNonZeroBytes(ExpectedBlockSize * 3);

        var enc = CreateTransform(cipher, (byte[])iv.Clone(), tagLen);
        var ct = new byte[plaintext.Length + tagLen];
        enc.Encrypt(plaintext, ct);

        var dec = CreateTransform(cipher, (byte[])iv.Clone(), tagLen);
        var recovered = new byte[plaintext.Length];
        dec.Decrypt(ct, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Round-trip must recover plaintext for tagLen = {tagLen}.");
    }

    // ── Long plaintext / L-array coverage ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that plaintext spanning many full blocks is encrypted and decrypted correctly,
    /// exercising L-array indices beyond L[1] and confirming that the block loop accumulates
    /// offsets and checksums without error over a large number of iterations.
    /// </summary>
    /// <remarks>
    /// The OCB3 offset sequence advances per block as
    /// <c>Offset_i = Offset_{i-1} XOR L_{ntz(i)}</c> (RFC 7253 §2.6), where <c>ntz(i)</c>
    /// is the number of trailing zero bits in the block index. A new L value is first needed
    /// at each power-of-2 block boundary:
    /// <list type="table">
    ///   <listheader><term>L index</term><term>First used at block</term><term>Min plaintext</term></listheader>
    ///   <item><term>L[0]</term><term>1</term><term>16 bytes</term></item>
    ///   <item><term>L[1]</term><term>2</term><term>32 bytes</term></item>
    ///   <item><term>L[2]</term><term>4</term><term>64 bytes</term></item>
    ///   <item><term>L[3]</term><term>8</term><term>128 bytes</term></item>
    ///   <item><term>L[4]</term><term>16</term><term>256 bytes</term></item>
    /// </list>
    /// Each <c>DataRow</c> is the first input length that exercises the corresponding L index
    /// for the first time. The final row adds a partial last block to verify that the
    /// partial-block path still operates correctly after a long run of full blocks.
    /// </remarks>
    [TestMethod]
    [DataRow(64, DisplayName = "64 bytes  — 4 full blocks, first use of L[2]")]
    [DataRow(128, DisplayName = "128 bytes — 8 full blocks, first use of L[3]")]
    [DataRow(256, DisplayName = "256 bytes — 16 full blocks, first use of L[4]")]
    [DataRow(256 + 7, DisplayName = "263 bytes — 16 full blocks + 7-byte partial")]
    public void EncryptThenDecrypt_WithLongPlaintext_ShouldRoundTrip(int plaintextLength)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = TestHelpers.GenerateRandomNonZeroBytes(plaintextLength);

        var enc = CreateTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var dec = CreateTransform(cipher, (byte[])iv.Clone());
        var recovered = new byte[plaintext.Length];
        dec.Decrypt(ct, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Round-trip must recover all {plaintextLength} plaintext bytes.");
    }

    // ── Plaintext length boundary conditions ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a full encrypt-then-decrypt round-trip succeeds for plaintext lengths
    /// that sit exactly at block boundaries and one byte either side of them, exercising
    /// all distinct code paths through the partial-block and full-block processing branches.
    /// </summary>
    /// <remarks>
    /// OCB3 has three structurally different code paths depending on whether the last block
    /// is absent (empty plaintext), a full block (uses <c>L_{ntz(m)}</c>), or a partial block
    /// (uses <c>L_*</c> and the keystream pad). The boundary lengths 15, 16, and 17 exercise
    /// the transition from partial-only, to exactly-full, to full-plus-partial respectively.
    /// </remarks>
    [TestMethod]
    [DataRow(1, DisplayName = "1 byte   — minimum non-empty")]
    [DataRow(15, DisplayName = "15 bytes — partial only (blockSize - 1)")]
    [DataRow(16, DisplayName = "16 bytes — exactly one full block")]
    [DataRow(17, DisplayName = "17 bytes — one full + 1-byte partial")]
    [DataRow(31, DisplayName = "31 bytes — two-block partial boundary")]
    [DataRow(32, DisplayName = "32 bytes — exactly two full blocks")]
    public void EncryptThenDecrypt_WithPlaintextAtBlockBoundaryLengths_ShouldRoundTrip(int ptLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = Enumerable.Range(0, ptLen).Select(i => (byte)(i & 0xFF)).ToArray();

        var enc = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[ptLen + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var recovered = new byte[ptLen];
        dec.Decrypt(ct, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Round-trip must recover all {ptLen} plaintext bytes.");
    }

    // ── AAD length boundary conditions ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a full encrypt-then-decrypt round-trip succeeds for AAD lengths that sit
    /// at block boundaries and one byte either side of them, exercising all HASH function paths.
    /// </summary>
    /// <remarks>
    /// The <c>HASH(K, A)</c> function (RFC 7253 §4) has two distinct inner paths: one for
    /// full 128-bit blocks using <c>L_{ntz(i)}</c>, and one for the final partial block using
    /// <c>L_*</c> with a 10…0 padding suffix. Boundary lengths 15, 16, and 17 exercise the
    /// transition between partial-only, full-block, and full-plus-partial AAD respectively.
    /// Two-block boundaries (32, 33) confirm that the full-block loop iterates correctly for
    /// multiple blocks with two distinct L-array indices (<c>L[0]</c> and <c>L[1]</c>).
    /// </remarks>
    [TestMethod]
    [DataRow(1, DisplayName = "1 byte   — sub-block partial")]
    [DataRow(15, DisplayName = "15 bytes — partial (blockSize - 1)")]
    [DataRow(16, DisplayName = "16 bytes — exactly one full block")]
    [DataRow(17, DisplayName = "17 bytes — full + 1-byte partial")]
    [DataRow(32, DisplayName = "32 bytes — exactly two full blocks")]
    [DataRow(33, DisplayName = "33 bytes — two full + 1-byte partial")]
    public void EncryptThenDecrypt_WithAadAtBlockBoundaryLengths_ShouldRoundTrip(int aadLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var aad = Enumerable.Range(0, aadLen).Select(i => (byte)(i & 0xFF)).ToArray();
        var plaintext = new byte[ExpectedBlockSize * 2]; // fixed 32-byte plaintext

        var enc = new OcbModeTransform(cipher, (byte[])iv.Clone());
        enc.ProcessAssociatedData(aad);
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        dec.ProcessAssociatedData(aad);
        var recovered = new byte[plaintext.Length];
        dec.Decrypt(ct, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Round-trip with {aadLen}-byte AAD must recover the plaintext.");
    }

    // ── IV nonce extraction ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that changing only the trailing padding bytes of the IV (bytes 12–15) does
    /// not affect the ciphertext or tag, because <see cref="OcbModeTransform" /> uses only
    /// the first 12 bytes as the OCB3 nonce.
    /// </summary>
    /// <remarks>
    /// The constructor copies <c>iv[0..11]</c> into the internal 12-byte nonce field and
    /// discards the remainder. The IV length must equal the cipher block size (16 bytes for
    /// AES) to match the interface contract, but bytes 12–15 carry no cryptographic meaning
    /// and are padding only.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WhenIvDiffersOnlyInTrailingPaddingBytes_ShouldProduceSameOutput()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(new byte[16]);
        var iv1 = new byte[ExpectedBlockSize];            // all zeros
        var iv2 = (byte[])iv1.Clone();
        iv2[12] = 0xAA; iv2[13] = 0xBB; iv2[14] = 0xCC; iv2[15] = 0xDD; // only padding differs
        var plaintext = new byte[ExpectedBlockSize];

        var enc1 = new OcbModeTransform(cipher1, iv1);
        var ct1 = new byte[plaintext.Length + enc1.TagSize];
        enc1.Encrypt(plaintext, ct1);

        var enc2 = new OcbModeTransform(cipher2, iv2);
        var ct2 = new byte[plaintext.Length + enc2.TagSize];
        enc2.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "IV bytes 12–15 are nonce padding and must not influence the ciphertext or tag.");
    }

}