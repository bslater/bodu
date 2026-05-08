// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── Decrypt argument validation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="InvalidOperationException" />
    /// when <see cref="AsconAead128.ProcessAssociatedData" /> has not been called.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAadNotProcessed_ShouldThrowInvalidOperationException()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        byte[] fakeCtWithTag = new byte[AsconAead128.TagBytes];

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Decrypt(fakeCtWithTag, Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ArgumentException" />
    /// when the input is shorter than <see cref="AsconAead128.TagBytes" /> bytes.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        using AsconAead128 sut = MakeInstance();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Decrypt(new byte[AsconAead128.TagBytes - 1], Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ArgumentException" />
    /// when the output buffer is too small to hold the recovered plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputBufferTooSmall_ShouldThrowArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length - 1]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ObjectDisposedException" />
    /// when the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Decrypt(new byte[AsconAead128.TagBytes], Array.Empty<byte>());
        });
    }

    // ── Tag-mismatch detection ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when one bit of the ciphertext body is flipped.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCiphertextBodyTampered_ShouldThrowCryptographicException()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                             0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        ciphertext[0] ^= 0x01;

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when one bit of the authentication tag is flipped.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagTampered_ShouldThrowCryptographicException()
    {
        byte[] plaintext = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        ciphertext[plaintext.Length] ^= 0x01; // first byte of tag

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when the associated data used during decryption differs from the one used during encryption.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAadMismatch_ShouldThrowCryptographicException()
    {
        byte[] aad1     = [0x01, 0x02];
        byte[] aad2     = [0xFF, 0xFF];
        byte[] plaintext = [0x10, 0x20, 0x30, 0x40];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];

        using AsconAead128 enc = new AsconAead128(ValidKey, ValidNonce);
        enc.ProcessAssociatedData(aad1);
        enc.Encrypt(plaintext, ciphertext);

        using AsconAead128 dec = new AsconAead128(ValidKey, ValidNonce);
        dec.ProcessAssociatedData(aad2);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> returns the number of plaintext bytes
    /// recovered, which is <c>ciphertextWithTag.Length - <see cref="AsconAead128.TagBytes" /></c>.
    /// </summary>
    [TestMethod]
    public void Decrypt_ShouldReturnCorrectByteCount()
    {
        byte[] plaintext  = new byte[20];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        byte[] recovered = new byte[plaintext.Length];
        using AsconAead128 dec = MakeInstance();
        int written = dec.Decrypt(ciphertext, recovered);

        Assert.AreEqual(plaintext.Length, written,
            "Decrypt must return plaintext.Length bytes written.");
    }

    // ── Single-use lifecycle ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> cannot be called more than once after associated data has been processed.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalledTwiceAfterAssociatedDataProcessed_ShouldThrowInvalidOperationException()
    {
        byte[] plaintext = [0x10, 0x20, 0x30, 0x40];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];

        using (AsconAead128 enc = MakeInstance())
        {
            _ = enc.Encrypt(plaintext, ciphertext);
        }

        byte[] recovered1 = new byte[plaintext.Length];
        byte[] recovered2 = new byte[plaintext.Length];

        using AsconAead128 sut = MakeInstance();

        _ = sut.Decrypt(ciphertext, recovered1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Decrypt(ciphertext, recovered2);
        });
    }

    /// <summary>
    /// Verifies that a failed authentication attempt still consumes the instance.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAuthenticationTagInvalid_ShouldMarkInstanceCompleted()
    {
        byte[] plaintext = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];

        using (AsconAead128 enc = MakeInstance())
        {
            _ = enc.Encrypt(plaintext, ciphertext);
        }

        ciphertext[^1] ^= 0x01;

        byte[] recovered = new byte[plaintext.Length];

        using AsconAead128 sut = MakeInstance();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = sut.Decrypt(ciphertext, recovered);
        });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Decrypt(ciphertext, recovered);
        });
    }


    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> with the input and output buffers
    /// referencing overlapping memory still recovers the plaintext when the tag is valid.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputAndOutputAlias_ShouldRecoverPlaintext()
    {
        byte[] plaintext = Enumerable.Range(0, 21).Select(i => (byte)(i * 3 + 1)).ToArray();

        byte[] sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        using (AsconAead128 enc = MakeInstance())
            enc.Encrypt(plaintext, sealed_);

        // Decrypt in-place over the same buffer — output starts at the head of `sealed_`.
        using AsconAead128 dec = MakeInstance();
        int written = dec.Decrypt(sealed_, sealed_);

        Assert.AreEqual(plaintext.Length, written);
        CollectionAssert.AreEqual(plaintext, sealed_.AsSpan(0, plaintext.Length).ToArray());
    }

    /// <summary>
    /// Verifies that when <see cref="AsconAead128.Decrypt" /> raises
    /// <see cref="CryptographicException" /> on tag mismatch the candidate plaintext is zeroed in
    /// the destination buffer to avoid leaking unauthenticated material.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagMismatch_ShouldZeroDestinationBuffer()
    {
        byte[] plaintext = Enumerable.Range(0, 16).Select(i => (byte)(i + 1)).ToArray();

        using AsconAead128 enc = MakeInstance();
        byte[] sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        sealed_[sealed_.Length - 1] ^= 0x01;

        using AsconAead128 dec = MakeInstance();
        byte[] recovered = new byte[plaintext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(sealed_, recovered);
        });

        foreach (byte b in recovered)
            Assert.AreEqual(0, b, "Decrypt must zero the destination buffer on tag mismatch.");
    }

}
