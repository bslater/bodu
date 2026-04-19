// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.ProcessAssociatedData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    // ── ProcessAssociatedData — lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="InvalidOperationException" /> when called after
    /// <see cref="OcbModeTransform.Encrypt" /> has been invoked on the same instance.
    /// </summary>
    /// <remarks>
    /// <see cref="OcbModeTransform.Encrypt" /> calls <c>EnsureAadProcessed</c> internally,
    /// which marks the associated-data slot as consumed by setting <c>aadProcessed = true</c>.
    /// Any subsequent call to <c>ProcessAssociatedData</c> is therefore rejected, enforcing
    /// the single-pass contract: each transform instance is intended for exactly one
    /// encrypt or decrypt operation.
    /// </remarks>
    [TestMethod]
    public void ProcessAssociatedData_AfterEncrypt_ShouldThrowInvalidOperationException()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var transform = new OcbModeTransform(cipher, new byte[ExpectedBlockSize]);
        var pt = new byte[ExpectedBlockSize];
        transform.Encrypt(pt, new byte[pt.Length + transform.TagSize]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            transform.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03 }),
            "ProcessAssociatedData must throw after Encrypt has been called on the same instance.");
    }

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="InvalidOperationException" /> when called after
    /// <see cref="OcbModeTransform.Decrypt" /> has been invoked on the same instance.
    /// </summary>
    /// <remarks>
    /// Like <c>Encrypt</c>, <c>Decrypt</c> calls <c>EnsureAadProcessed</c> at the start of
    /// its execution, which permanently sets the consumed flag. A caller that attempts to
    /// supply associated data after decryption has already completed must receive
    /// <see cref="InvalidOperationException" /> to prevent silent misuse.
    /// </remarks>
    [TestMethod]
    public void ProcessAssociatedData_AfterDecrypt_ShouldThrowInvalidOperationException()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var pt = new byte[ExpectedBlockSize];

        // Produce valid ciphertext so Decrypt can complete successfully.
        var enc = new OcbModeTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[pt.Length + enc.TagSize];
        enc.Encrypt(pt, ct);

        var dec = new OcbModeTransform(cipher, (byte[])iv.Clone());
        dec.Decrypt(ct, new byte[pt.Length]);   // sets aadProcessed = true

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            dec.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03 }),
            "ProcessAssociatedData must throw after Decrypt has been called on the same instance.");
    }

    // ── ProcessAssociatedData — empty-span equivalence ────────────────────────────────────────

    /// <summary>
    /// Verifies that explicitly passing an empty span to
    /// <see cref="OcbModeTransform.ProcessAssociatedData" /> produces the same ciphertext
    /// and tag as not calling it at all.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §4 defines <c>HASH(K, A)</c> of the empty string as <c>zeros(128)</c>.
    /// Whether the caller explicitly passes an empty span (causing <c>this.aad</c> to be
    /// set to an empty array) or omits the call entirely (causing <c>EnsureAadProcessed</c>
    /// to default <c>this.aad</c> to <c>Array.Empty&lt;byte&gt;()</c>), the HASH result is
    /// the same 128-bit zero block, so the authentication tag is identical in both cases.
    /// </remarks>
    [TestMethod]
    public void ProcessAssociatedData_WithEmptySpan_ShouldProduceSameOutputAsOmittingCall()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(new byte[16]);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize * 2 + 5];

        // Instance 1: explicit empty AAD.
        var withEmpty = new OcbModeTransform(cipher1, (byte[])iv.Clone());
        withEmpty.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        var ct1 = new byte[plaintext.Length + withEmpty.TagSize];
        withEmpty.Encrypt(plaintext, ct1);

        // Instance 2: no ProcessAssociatedData call.
        var withNone = new OcbModeTransform(cipher2, (byte[])iv.Clone());
        var ct2 = new byte[plaintext.Length + withNone.TagSize];
        withNone.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "ProcessAssociatedData with an empty span must produce the same output as " +
            "omitting the call (RFC 7253 §4: HASH of empty A = zeros(128)).");
    }
}
