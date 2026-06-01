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
        var ciphertext = (byte[])s_tamperBaseCiphertext.Clone();
        ciphertext[0] ^= 0x01;
        var ctWithTag = ConcatTag(ciphertext, s_tamperBaseTag);
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
        var tag = (byte[])s_tamperBaseTag.Clone();
        tag[0] ^= 0x01;
        var ctWithTag = ConcatTag(s_tamperBaseCiphertext, tag);
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
        var ctWithTag = ConcatTag(s_tamperBaseCiphertext, s_tamperBaseTag);
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
        var nonce = (byte[])s_tamperReferenceNonce.Clone();
        nonce[0] ^= 0x01;
        var ctWithTag = ConcatTag(s_tamperBaseCiphertext, s_tamperBaseTag);
        using AsconAead128 aead = new(s_tamperReferenceKey, nonce);
        aead.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(ctWithTag, new byte[s_tamperBasePlaintext.Length]);
        });
    }

    private static byte[] ConcatTag(byte[] ciphertext, byte[] tag)
    {
        var result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        return result;
    }
}
