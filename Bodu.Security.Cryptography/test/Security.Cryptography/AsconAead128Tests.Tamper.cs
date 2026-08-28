// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Tamper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    private static readonly byte[] s_tamperReferenceKey = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");

    private static readonly byte[] s_tamperReferenceNonce = Convert.FromHexString("101112131415161718191A1B1C1D1E1F");

    private static readonly byte[] s_tamperBasePlaintext = [0x20];

    private static readonly byte[] s_tamperBaseCiphertext = [0xE8];

    private static readonly byte[] s_tamperBaseTag = Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588");

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> rejects a tampered ciphertext byte by throwing
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCiphertextByteTampered_ShouldThrowExactly()
    {
        byte[] ciphertext = (byte[])s_tamperBaseCiphertext.Clone();
        ciphertext[0] ^= 0x01;
        byte[] ctWithTag = ConcatTag(ciphertext, s_tamperBaseTag);
        using AsconAead128 aead = new(s_tamperReferenceKey, s_tamperReferenceNonce);
        aead.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> rejects a tampered authentication-tag byte
    /// by throwing <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagByteTampered_ShouldThrowExactly()
    {
        byte[] tag = (byte[])s_tamperBaseTag.Clone();
        tag[0] ^= 0x01;
        byte[] ctWithTag = ConcatTag(s_tamperBaseCiphertext, tag);
        using AsconAead128 aead = new(s_tamperReferenceKey, s_tamperReferenceNonce);
        aead.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> rejects an introduced associated-data byte
    /// (the encrypted vector had no AD) by throwing <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAssociatedDataIntroduced_ShouldThrowExactly()
    {
        byte[] ctWithTag = ConcatTag(s_tamperBaseCiphertext, s_tamperBaseTag);
        using AsconAead128 aead = new(s_tamperReferenceKey, s_tamperReferenceNonce);
        aead.ProcessAssociatedData([0x01]);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> rejects a tampered nonce byte by throwing
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenNonceByteTampered_ShouldThrowExactly()
    {
        byte[] nonce = (byte[])s_tamperReferenceNonce.Clone();
        nonce[0] ^= 0x01;
        byte[] ctWithTag = ConcatTag(s_tamperBaseCiphertext, s_tamperBaseTag);
        using AsconAead128 aead = new(s_tamperReferenceKey, nonce);
        aead.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> clears the keyed sponge state when authentication fails, so
    /// a rejected message does not leave the full 320-bit permutation state — key- and plaintext-derived — live in the
    /// transform instance until disposal.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAuthenticationFails_ShouldClearSpongeState()
    {
        byte[] ciphertext = (byte[])s_tamperBaseCiphertext.Clone();
        ciphertext[0] ^= 0x01;
        byte[] ctWithTag = ConcatTag(ciphertext, s_tamperBaseTag);
        using AsconAead128 aead = new(s_tamperReferenceKey, s_tamperReferenceNonce);
        aead.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });

        System.Reflection.FieldInfo stateField = typeof(AsconAead128).GetField(
            "_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var state = (AsconState)stateField.GetValue(aead)!;

        Assert.AreEqual(default, state,
            "AsconAead128 must reset the sponge state on authentication failure so key-derived permutation " +
            "state does not outlive the rejected message.");
    }

    private static byte[] ConcatTag(byte[] ciphertext, byte[] tag)
    {
        byte[] result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        return result;
    }
}
