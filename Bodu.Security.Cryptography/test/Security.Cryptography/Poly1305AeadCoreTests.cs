// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Anchors the shared <see cref="Poly1305AeadCore" /> RFC 8439 framing against the gold-standard
/// ChaCha20-Poly1305 known-answer vector from RFC 8439 Section 2.8.2. Because the extended-nonce constructions reuse
/// this exact framing on top of an HChaCha20 / HSalsa20 subkey, locking the framing here verifies the pad16 + length
/// block, the counter-0 Poly1305 key derivation, and the encrypt-from-counter-1 behaviour independently of the
/// subkey-derivation layer.
/// </summary>
[TestClass]
public class Poly1305AeadCoreTests
{
    // RFC 8439 Section 2.8.2 — AEAD_CHACHA20_POLY1305 example and test vector.
    private static readonly byte[] s_key =
        Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");

    private static readonly byte[] s_nonce =
        Convert.FromHexString("070000004041424344454647");

    private static readonly byte[] s_associatedData =
        Convert.FromHexString("50515253c0c1c2c3c4c5c6c7");

    private static readonly byte[] s_plaintext =
        Convert.FromHexString(
            "4c616469657320616e642047656e746c656d656e206f662074686520636c617373206f66202739393a2049662049" +
            "20636f756c64206f6666657220796f75206f6e6c79206f6e652074697020666f7220746865206675747572652c20" +
            "73756e73637265656e20776f756c642062652069742e");

    private static readonly byte[] s_ciphertext =
        Convert.FromHexString(
            "d31a8d34648e60db7b86afbc53ef7ec2a4aded51296e08fea9e2b5a736ee62d63dbea45e8ca967128" +
            "2fafb69da92728b1a71de0a9e060b2905d6a5b67ecd3b3692ddbd7f2d778b8c9803aee328091b58fa" +
            "b324e4fad675945585808b4831d7bc3ff4def08e4b7a9de576d26586cec64b6116");

    private static readonly byte[] s_tag =
        Convert.FromHexString("1ae10b594f09e26a7e902ecbd0600691");

    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.SealRfc8439" /> driven by a raw ChaCha20 engine reproduces the
    /// ciphertext and tag mandated by RFC 8439 Section 2.8.2.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenGivenRfc8439Vector_ShouldProduceExpectedCiphertextAndTag()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);
        byte[] output = new byte[s_plaintext.Length + Poly1305AeadCore.TagBytes];

        int written = Poly1305AeadCore.SealRfc8439(engine, s_associatedData, s_plaintext, output);

        Assert.AreEqual(output.Length, written);
        CollectionAssert.AreEqual(s_ciphertext, output.AsSpan(0, s_plaintext.Length).ToArray());
        CollectionAssert.AreEqual(s_tag, output.AsSpan(s_plaintext.Length).ToArray());
    }

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
    /// Verifies that sealing under the RFC 8439 framing with an engine that produces keystream in bulk matches sealing
    /// with the same engine one block at a time, for every message length from empty to past the widest kernel's run.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenEngineHasNoBulkPath_ShouldMatchTheBulkEngine()
    {
        var random = new Random(0x5EA1_0001);
        byte[] associatedData = new byte[13];
        random.NextBytes(associatedData);

        foreach (int length in Enumerable.Range(0, 300).Concat([1024, 1029, 4099]))
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var single = new SingleBlockStreamCipher(new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0));
            using var bulk = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealRfc8439(single, associatedData, plaintext, expected);
            _ = Poly1305AeadCore.SealRfc8439(bulk, associatedData, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
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
    /// Verifies that sealing under the secretbox framing with an engine that produces keystream in bulk matches sealing
    /// with the same engine one block at a time, for every message length from empty to past the widest kernel's run,
    /// including those that end within the counter-0 block's trailing 32 bytes.
    /// </summary>
    [TestMethod]
    public void SealSecretbox_WhenEngineHasNoBulkPath_ShouldMatchTheBulkEngine()
    {
        var random = new Random(0x5EA1_0003);
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        random.NextBytes(key);
        random.NextBytes(nonce);

        foreach (int length in Enumerable.Range(0, 300).Concat([1024, 1029, 4099]))
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var single = new SingleBlockStreamCipher(new Salsa20StreamCipher(key, nonce, initialCounter: 0));
            using var bulk = new Salsa20StreamCipher(key, nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealSecretbox(single, plaintext, expected);
            _ = Poly1305AeadCore.SealSecretbox(bulk, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }
}
