// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.OpenRfc8439.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.OpenRfc8439" /> recovers the RFC 8439 Section 2.8.2 plaintext from the
    /// reference ciphertext and tag.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenGivenRfc8439Vector_ShouldRecoverPlaintext()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

        byte[] ciphertextWithTag = new byte[s_ciphertext.Length + s_tag.Length];
        s_ciphertext.CopyTo(ciphertextWithTag, 0);
        s_tag.CopyTo(ciphertextWithTag, s_ciphertext.Length);

        byte[] output = new byte[s_ciphertext.Length];
        int written = Poly1305AeadCore.OpenRfc8439(engine, s_associatedData, ciphertextWithTag, output);

        Assert.AreEqual(s_plaintext.Length, written);
        CollectionAssert.AreEqual(s_plaintext, output);
    }

    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.OpenRfc8439" /> throws <see cref="CryptographicException" /> when the
    /// authentication tag has been altered.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

        byte[] ciphertextWithTag = new byte[s_ciphertext.Length + s_tag.Length];
        s_ciphertext.CopyTo(ciphertextWithTag, 0);
        s_tag.CopyTo(ciphertextWithTag, s_ciphertext.Length);
        ciphertextWithTag[^1] ^= 0xff;

        byte[] output = new byte[s_ciphertext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = Poly1305AeadCore.OpenRfc8439(engine, s_associatedData, ciphertextWithTag, output);
        });
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with an engine that produces keystream in bulk recovers every
    /// plaintext, of every length from empty to past the widest kernel's run, sealed one keystream block at a time.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenSealedOneBlockAtATime_ShouldRecoverPlaintext()
    {
        var random = new Random(0x5EA1_0002);

        foreach (int length in Enumerable.Range(0, 300).Concat([1024, 1029, 4099]))
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] sealedMessage = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] recovered = new byte[length];
            using var single = new SingleBlockStreamCipher(new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0));
            using var bulk = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealRfc8439(single, s_associatedData, plaintext, sealedMessage);
            _ = Poly1305AeadCore.OpenRfc8439(bulk, s_associatedData, sealedMessage, recovered);

            CollectionAssert.AreEqual(plaintext, recovered, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value recovers the RFC 8439 Section 2.8.2 plaintext.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenKeystreamIsAValue_ShouldRecoverPlaintext()
    {
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(s_key, s_nonce, counter: 0);
        byte[] output = new byte[s_plaintext.Length];

        int written = Poly1305AeadCore.OpenRfc8439(ref keystream, s_associatedData, Rfc8439CiphertextWithTag(), output);

        Assert.AreEqual(s_plaintext.Length, written);
        CollectionAssert.AreEqual(s_plaintext, output);
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value throws <see cref="CryptographicException" /> for an altered tag
    /// before writing any plaintext.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenKeystreamIsAValueAndTagIsTampered_ShouldThrowWithoutWritingOutput()
    {
        byte[] ciphertextWithTag = Rfc8439CiphertextWithTag();
        ciphertextWithTag[^1] ^= 0x01;
        byte[] output = new byte[s_plaintext.Length];
        Array.Fill(output, (byte)0xCC);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(s_key, s_nonce, counter: 0);
            _ = Poly1305AeadCore.OpenRfc8439(ref keystream, s_associatedData, ciphertextWithTag, output);
        });

        Assert.IsTrue(output.All(value => value == 0xCC), "The output was written before the tag was verified.");
    }
}
