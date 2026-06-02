// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20EngineFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Builds the XSalsa20 keystream engine shared by the secretbox and RFC 8439 XSalsa20-Poly1305 AEAD constructions. Both
/// derive the same Salsa20 engine from a 256-bit key and 192-bit nonce; only the Poly1305 framing applied on top
/// differs.
/// </summary>
internal static class XSalsa20EngineFactory
{
    /// <summary>
    /// Derives the 256-bit Salsa20 subkey from the key and the first 128 bits of the nonce via HSalsa20, then returns a
    /// Salsa20 engine under that subkey with the trailing 64 bits of the nonce and a block counter starting at 0.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">The 192-bit (24-byte) extended nonce.</param>
    /// <returns>A Salsa20 keystream engine positioned at block counter 0.</returns>
    internal static IStreamCipher Create(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        Span<byte> subkey = stackalloc byte[Salsa20StreamCipher.KeySize256Bytes];
        Span<byte> salsaNonce = stackalloc byte[Salsa20StreamCipher.NonceSizeBytes];

        try
        {
            Salsa20StreamCipher.HSalsa20(key, nonce[..Salsa20StreamCipher.HSalsaNonceSizeBytes], subkey);
            nonce.Slice(Salsa20StreamCipher.HSalsaNonceSizeBytes, Salsa20StreamCipher.NonceSizeBytes).CopyTo(salsaNonce);

            return new Salsa20StreamCipher(subkey, salsaNonce, initialCounter: 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }
}
