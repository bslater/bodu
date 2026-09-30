// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Poly1305AeadCore" />, grouped into member-named partial files. The RFC 8439 framing is anchored
/// to the gold-standard ChaCha20-Poly1305 known-answer vector from RFC 8439 Section 2.8.2. Because the extended-nonce
/// constructions reuse this exact framing on top of an HChaCha20 / HSalsa20 subkey, locking the framing here verifies
/// the pad16 + length block, the counter-0 Poly1305 key derivation, and the encrypt-from-counter-1 behaviour
/// independently of the subkey-derivation layer. Each framing is also held, over every message length to past the
/// widest kernel's run, to the same framing drawn from an engine one block at a time and from a keystream value.
/// </summary>
[TestClass]
public partial class Poly1305AeadCoreTests
{
    // RFC 8439 Section 2.8.2 - AEAD_CHACHA20_POLY1305 example and test vector.
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
    /// Gets the message lengths the differential tests sweep: every length from empty to 1,299 bytes, which crosses the
    /// block, secretbox-offset and 4-, 8- and 16-block run boundaries, the longest message each framing can draw in one
    /// pass through a buffer (960 bytes under RFC 8439, 992 under secretbox), and every count of bytes, from one to a
    /// whole group, after the whole groups of a longer message; and 4,099 bytes, whose last block is partial.
    /// </summary>
    /// <value>The lengths, in bytes.</value>
    private static IEnumerable<int> MessageLengths =>
        Enumerable.Range(0, 1300).Append(4099);

    /// <summary>
    /// Returns the RFC 8439 Section 2.8.2 ciphertext followed by its tag.
    /// </summary>
    /// <returns>The sealed message.</returns>
    private static byte[] Rfc8439CiphertextWithTag()
    {
        byte[] ciphertextWithTag = new byte[s_ciphertext.Length + s_tag.Length];
        s_ciphertext.CopyTo(ciphertextWithTag, 0);
        s_tag.CopyTo(ciphertextWithTag, s_ciphertext.Length);
        return ciphertextWithTag;
    }

    /// <summary>
    /// Returns a new array of seeded random bytes.
    /// </summary>
    /// <param name="random">The source of the bytes.</param>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    private static byte[] NextBytes(Random random, int length)
    {
        byte[] bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }
}
