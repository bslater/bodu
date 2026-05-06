// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
        byte[] plaintext = new byte[8];

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
        byte[] plaintext = new byte[37]; // non-multiple of block to exercise partial-block path
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        using AsconAead128 sut = MakeInstance();
        byte[] output = new byte[plaintext.Length + AsconAead128.TagBytes];

        int written = sut.Encrypt(plaintext, output);

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
        byte[] output = new byte[AsconAead128.TagBytes];

        int written = sut.Encrypt(ReadOnlySpan<byte>.Empty, output);

        Assert.AreEqual(AsconAead128.TagBytes, written);

        // The tag must not be all-zero.
        bool allZero = true;
        foreach (byte b in output) { if (b != 0) { allZero = false; break; } }
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

        byte[] output1 = new byte[plaintext.Length + AsconAead128.TagBytes];
        byte[] output2 = new byte[plaintext.Length + AsconAead128.TagBytes];

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
        byte[] output1 = new byte[plaintext.Length + AsconAead128.TagBytes];
        byte[] output2 = new byte[plaintext.Length + AsconAead128.TagBytes];

        using AsconAead128 sut = MakeInstance();

        _ = sut.Encrypt(plaintext, output1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Encrypt(plaintext, output2);
        });
    }

}
