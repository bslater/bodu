// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305IetfTests.Tamper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

public partial class XSalsa20Poly1305IetfTests
{
    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws <see cref="CryptographicException" />
    /// when the authentication tag has been altered.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(new byte[] { 1, 2, 3, 4 }, new byte[] { 0xaa });
        sealed_[^1] ^= 0x80;

        using var dec = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce);
        dec.ProcessAssociatedData(new byte[] { 0xaa });

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(sealed_, new byte[sealed_.Length - 16]);
        });
    }

    /// <summary>
    /// Verifies that decrypting with associated data different from that used at encryption time throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAssociatedDataDiffers_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(new byte[] { 1, 2, 3, 4 }, new byte[] { 0xaa });

        using var dec = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce);
        dec.ProcessAssociatedData(new byte[] { 0xbb });

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(sealed_, new byte[sealed_.Length - 16]);
        });
    }

    /// <summary>
    /// Verifies that a failed authentication leaves the output buffer untouched — no candidate plaintext is released.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAuthenticationFails_ShouldNotWriteOutput()
    {
        byte[] sealed_ = Seal(new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }, ReadOnlySpan<byte>.Empty);
        sealed_[^1] ^= 0xff;

        var output = new byte[sealed_.Length - 16];
        using var dec = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce);
        dec.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(sealed_, output);
        });

        CollectionAssert.AreEqual(new byte[output.Length], output);
    }

    private static byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        using var enc = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce);
        return enc.Encrypt(plaintext, associatedData);
    }
}
