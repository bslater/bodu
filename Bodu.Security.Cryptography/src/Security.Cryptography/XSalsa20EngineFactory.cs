// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20EngineFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Builds the XSalsa20 keystream shared by the secretbox and RFC 8439 XSalsa20-Poly1305 AEAD constructions, as an
/// engine or seeded in place in a value on the stack. Both constructions derive the same Salsa20 keystream from a
/// 256-bit key and 192-bit nonce; only the Poly1305 framing applied on top differs.
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
            DeriveSubkeyAndNonce(key, nonce, subkey, salsaNonce);
            return new Salsa20StreamCipher(subkey, salsaNonce, initialCounter: 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }

    /// <summary>
    /// Seeds a Salsa20 keystream in place with the keystream <see cref="Create" /> returns an engine for: the HSalsa20
    /// subkey and the trailing 64 bits of the nonce, positioned at block counter 0.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">The 192-bit (24-byte) extended nonce.</param>
    /// <param name="keystream">The keystream to seed.</param>
    internal static void InitializeKeystream(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ref Salsa20Core.Keystream keystream)
    {
        Span<byte> subkey = stackalloc byte[Salsa20StreamCipher.KeySize256Bytes];
        Span<byte> salsaNonce = stackalloc byte[Salsa20StreamCipher.NonceSizeBytes];

        try
        {
            DeriveSubkeyAndNonce(key, nonce, subkey, salsaNonce);
            keystream.Initialize(subkey, salsaNonce, counter: 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }

    /// <summary>
    /// Derives the Salsa20 subkey via HSalsa20 over the key and the first 128 bits of the nonce, and takes the trailing
    /// 64 bits of the nonce as the Salsa20 nonce.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">The 192-bit (24-byte) extended nonce.</param>
    /// <param name="subkey">Receives the 32-byte subkey.</param>
    /// <param name="salsaNonce">Receives the 8-byte Salsa20 nonce.</param>
    private static void DeriveSubkeyAndNonce(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> subkey, Span<byte> salsaNonce)
    {
        Salsa20StreamCipher.HSalsa20(key, nonce[..Salsa20StreamCipher.HSalsaNonceSizeBytes], subkey);
        nonce.Slice(Salsa20StreamCipher.HSalsaNonceSizeBytes, Salsa20StreamCipher.NonceSizeBytes).CopyTo(salsaNonce);
    }
}
