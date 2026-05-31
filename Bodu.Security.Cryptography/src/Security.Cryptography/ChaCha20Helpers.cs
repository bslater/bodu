// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Helpers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides shared validation guards for the ChaCha20 / XChaCha20 stream-cipher family.
/// </summary>
internal static class ChaCha20Helpers
{
    /// <summary>
    /// Throws if the supplied nonce is <see langword="null" /> or does not have the exact required byte length.
    /// </summary>
    /// <param name="nonce">The nonce supplied as the algorithm IV.</param>
    /// <param name="expectedBytes">The required nonce length, in bytes.</param>
    /// <param name="paramName">The captured argument expression for the offending parameter.</param>
    /// <exception cref="CryptographicException">
    /// <paramref name="nonce" /> is <see langword="null" /> or its length is not <paramref name="expectedBytes" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidNonceSize(byte[]? nonce, int expectedBytes, [CallerArgumentExpression(nameof(nonce))] string? paramName = null)
    {
        int actualBits = (nonce?.Length ?? 0) * 8;
        if (nonce is null || nonce.Length != expectedBytes)
        {
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_IVSize, actualBits, expectedBytes * 8),
                paramName);
        }
    }
}
