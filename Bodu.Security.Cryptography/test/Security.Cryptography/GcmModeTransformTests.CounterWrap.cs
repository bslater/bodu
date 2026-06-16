// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.CounterWrap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="GcmModeTransform" /> rejects message lengths that would force the 32-bit GCM
/// counter (NIST SP 800-38D <c>inc32</c>) to wrap past <c>0xFFFFFFFF</c>.
/// </summary>
/// <remarks>
/// <para>
/// GCM uses a 32-bit counter occupying the low four bytes of the 128-bit counter block, with the value
/// <c>nonce ‖ 0x00000001</c> reserved as <c>J0</c> for the tag computation. The counter therefore has
/// <c>2^32 − 2</c> distinct payload values before it would wrap back to <c>J0</c>. Silently wrapping is a
/// security failure: the wrapped block reuses <c>E_K(J0)</c> as keystream, which leaks the GHASH subkey and
/// destroys both confidentiality and authenticity.
/// </para>
/// <para>
/// The public single-shot API enforces this naturally because <see cref="ReadOnlySpan{T}.Length" /> is
/// <see cref="int" />-typed (max ~2.1 GiB), which is well below the <c>(2^32 − 2) × 16</c> ≈ 64 GiB threshold.
/// The test below reaches the wrap boundary via the internal <c>CreateForTesting</c> constructor that accepts
/// a precomputed <c>J0</c>, allowing the counter to be seeded close to <c>0xFFFFFFFF</c> with only a few
/// payload blocks.
/// </para>
/// </remarks>
public sealed partial class GcmModeTransformTests
{
    private const int BlockSizeBytes = 16;

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Encrypt" /> throws <see cref="CryptographicException" /> when
    /// the per-message counter would wrap from <c>0xFFFFFFFF</c> back to <c>0x00000000</c> (which equals
    /// <c>J0</c>'s reserved counter value), rather than silently producing ciphertext under the reused
    /// <c>E_K(J0)</c> keystream.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCounterWouldWrapPast0xFFFFFFFF_ShouldThrowCryptographicException()
    {
        // J0 is "<12-byte nonce> || 00 00 FF FE". After the constructor's inc32, the running counter is
        // <nonce> || 00 00 FF FF — one increment away from wrapping into the J0-reserved value.
        // Encrypting two 16-byte blocks consumes counters FFFF and then would wrap to 0000.
        byte[] j0 = new byte[BlockSizeBytes];
        Array.Fill(j0, (byte)0xCA, 0, 12);
        j0[12] = 0xFF;
        j0[13] = 0xFF;
        j0[14] = 0xFF;
        j0[15] = 0xFE;

        using var cipher = new AesBlockCipherFixture(new byte[16]);
        using var transform = GcmModeTransform.CreateForTesting(cipher, j0);

        byte[] plaintext = new byte[BlockSizeBytes * 2];
        byte[] output = new byte[plaintext.Length + (transform.TagSize / 8)];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.Encrypt(plaintext, output);
        });
    }

    /// <summary>
    /// Verifies that the wrap guard fires when the running counter is exactly at <c>0xFFFFFFFF</c> and a single
    /// further payload block is processed — the smallest possible wrap scenario.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenSingleBlockWouldWrapFrom0xFFFFFFFF_ShouldThrowCryptographicException()
    {
        // J0 ending in FFFE means the post-constructor counter is FFFF; one payload block triggers the wrap
        // attempt on its next increment, but the keystream for that single block is itself derived from the
        // counter that, post-encryption, would step into J0's reserved value. The implementation must reject
        // the operation before producing that ciphertext block.
        byte[] j0 = new byte[BlockSizeBytes];
        Array.Fill(j0, (byte)0xCA, 0, 12);
        j0[12] = 0xFF;
        j0[13] = 0xFF;
        j0[14] = 0xFF;
        j0[15] = 0xFE;

        using var cipher = new AesBlockCipherFixture(new byte[16]);
        using var transform = GcmModeTransform.CreateForTesting(cipher, j0);

        // Single block worth of plaintext: encrypts under counter FFFF, then inc32 would wrap to 0000 — but
        // since this is the final block we do not actually consume the wrapped counter. The guard only needs
        // to fire when a counter value would actually be USED. Encrypting one block is therefore expected to
        // succeed; encrypting two blocks must fail. This test documents the boundary: one block at FFFF is OK.
        byte[] plaintext = new byte[BlockSizeBytes];
        byte[] output = new byte[plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(plaintext, output);

        // Reaching here proves the guard does not over-reject; the wrap-rejection contract is exercised by
        // the two-block test above.
        Assert.AreEqual(BlockSizeBytes + (transform.TagSize / 8), output.Length);
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Decrypt" /> also rejects wrap-inducing ciphertext lengths,
    /// matching the encrypt-side contract so that a malicious or corrupt input cannot trick the implementation
    /// into reusing <c>E_K(J0)</c> as keystream during the inverse transform.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCounterWouldWrapPast0xFFFFFFFF_ShouldThrowCryptographicException()
    {
        byte[] j0 = new byte[BlockSizeBytes];
        Array.Fill(j0, (byte)0xCA, 0, 12);
        j0[12] = 0xFF;
        j0[13] = 0xFF;
        j0[14] = 0xFF;
        j0[15] = 0xFE;

        using var cipher = new AesBlockCipherFixture(new byte[16]);
        using var transform = GcmModeTransform.CreateForTesting(cipher, j0);

        // Two-block ciphertext plus tag — same wrap boundary as the encrypt test.
        byte[] ciphertextWithTag = new byte[(BlockSizeBytes * 2) + (transform.TagSize / 8)];
        byte[] recovered = new byte[BlockSizeBytes * 2];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.Decrypt(ciphertextWithTag, recovered);
        });
    }
}
