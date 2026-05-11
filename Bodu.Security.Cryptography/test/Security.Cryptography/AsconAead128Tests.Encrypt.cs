// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── Encrypt argument validation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> throws <see cref="InvalidOperationException" />
    /// when <see cref="AsconAead128.ProcessAssociatedData" /> has not been called.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenAadNotProcessed_ShouldThrowInvalidOperationException()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Encrypt(Array.Empty<byte>(), new byte[AsconAead128.TagBytes]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> throws <see cref="ArgumentException" />
    /// when the output buffer is too small to hold the ciphertext plus the authentication tag.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputBufferTooSmall_ShouldThrowArgumentException()
    {
        using AsconAead128 sut = MakeInstance();
        var plaintext = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Encrypt(plaintext, new byte[plaintext.Length]); // missing tag bytes
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> throws <see cref="ObjectDisposedException" />
    /// when the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Encrypt(Array.Empty<byte>(), new byte[AsconAead128.TagBytes]);
        });
    }

    // ── Encrypt output-size ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> returns exactly
    /// <c>plaintext.Length + <see cref="AsconAead128.TagBytes" /></c>.
    /// </summary>
    [TestMethod]
    public void Encrypt_ShouldReturnPlaintextLengthPlusTagBytes()
    {
        var plaintext = new byte[37]; // non-multiple of block to exercise partial-block path
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        using AsconAead128 sut = MakeInstance();
        var output = new byte[plaintext.Length + AsconAead128.TagBytes];

        var written = sut.Encrypt(plaintext, output);

        Assert.AreEqual(plaintext.Length + AsconAead128.TagBytes, written);
    }

    /// <summary>
    /// Verifies that encrypting an empty plaintext produces exactly <see cref="AsconAead128.TagBytes" />
    /// bytes (tag only, no ciphertext).
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenPlaintextEmpty_ShouldProduceTagOnly()
    {
        using AsconAead128 sut = MakeInstance();
        var output = new byte[AsconAead128.TagBytes];

        var written = sut.Encrypt(ReadOnlySpan<byte>.Empty, output);

        Assert.AreEqual(AsconAead128.TagBytes, written);

        // The tag must not be all-zero.
        var allZero = true;
        foreach (var b in output) { if (b != 0) { allZero = false; break; } }
        Assert.IsFalse(allZero, "The authentication tag for an empty plaintext must not be all zero.");
    }

    /// <summary>
    /// Verifies that encrypting the same plaintext twice with the same key and nonce produces
    /// identical ciphertext and tag (determinism).
    /// </summary>
    [TestMethod]
    public void Encrypt_SamePlaintextSameKeyNonce_ShouldProduceIdenticalOutput()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var output1 = new byte[plaintext.Length + AsconAead128.TagBytes];
        var output2 = new byte[plaintext.Length + AsconAead128.TagBytes];

        using AsconAead128 enc1 = MakeInstance();
        enc1.Encrypt(plaintext, output1);

        using AsconAead128 enc2 = MakeInstance();
        enc2.Encrypt(plaintext, output2);

        CollectionAssert.AreEqual(output1, output2, "Encrypt must be deterministic for the same key, nonce, and plaintext.");
    }

    // ── Single-use lifecycle ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> cannot be called more than once after associated data has been processed.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalledTwiceAfterAssociatedDataProcessed_ShouldThrowInvalidOperationException()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04];
        var output1 = new byte[plaintext.Length + AsconAead128.TagBytes];
        var output2 = new byte[plaintext.Length + AsconAead128.TagBytes];

        using AsconAead128 sut = MakeInstance();

        _ = sut.Encrypt(plaintext, output1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Encrypt(plaintext, output2);
        });
    }

    /// <summary>
    /// Verifies that after <see cref="AsconAead128.Decrypt" /> raises a
    /// <see cref="CryptographicException" /> on tag mismatch, subsequent <see cref="AsconAead128.Encrypt" />
    /// calls throw <see cref="InvalidOperationException" /> — the instance is poisoned and
    /// must not be reusable for an alternate operation.
    /// </summary>
    [TestMethod]
    public void Encrypt_AfterDecryptTagMismatch_ShouldThrowExactly()
    {
        var plaintext = new byte[8];

        using AsconAead128 enc = MakeInstance();
        var sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        // Flip a bit in the tag to force a mismatch on Decrypt.
        sealed_[sealed_.Length - 1] ^= 0xFF;

        using AsconAead128 dec = MakeInstance();
        var recovered = new byte[plaintext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(sealed_, recovered);
        });

        // Now the instance has been completed via the finally block — Encrypt must reject.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dec.Encrypt(plaintext, sealed_);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> regardless of whether AAD has been processed.
    /// Covers the disposed-then-pre-AAD path that the existing
    /// <c>Encrypt_WhenAadNotProcessed_ShouldThrowInvalidOperationException</c> does not exercise.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenDisposedBeforeAadProcessed_ShouldThrowExactly()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Encrypt(Array.Empty<byte>(), new byte[AsconAead128.TagBytes]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> with the input and output buffers
    /// referencing overlapping memory still produces the same result as a non-aliased pair —
    /// confirming the implementation reads each block fully before writing.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenInputAndOutputAlias_ShouldProduceSameResult()
    {
        var plaintext = Enumerable.Range(0, 37).Select(i => (byte)i).ToArray();

        // Reference encryption with disjoint buffers.
        var reference = new byte[plaintext.Length + AsconAead128.TagBytes];
        using (AsconAead128 enc = MakeInstance())
            enc.Encrypt(plaintext, reference);

        // In-place encryption with aliased buffer.
        var aliased = new byte[plaintext.Length + AsconAead128.TagBytes];
        Buffer.BlockCopy(plaintext, 0, aliased, 0, plaintext.Length);
        using (AsconAead128 inplace = MakeInstance())
            inplace.Encrypt(aliased.AsSpan(0, plaintext.Length), aliased);

        CollectionAssert.AreEqual(reference, aliased,
            "Aliased Encrypt must match the disjoint result for the same input.");
    }

}
