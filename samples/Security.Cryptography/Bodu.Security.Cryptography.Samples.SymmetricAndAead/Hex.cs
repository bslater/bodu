// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead;

/// <summary>
/// Provides a single lowercase-hex formatting helper and a fixed key-material generator shared by the
/// scenarios, so every ciphertext, key, nonce, and tag prints in the same deterministic form.
/// </summary>
public static class Hex
{
    /// <summary>
    /// Formats a byte sequence as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hex representation.</returns>
    public static string ToHex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();

    /// <summary>
    /// Creates a deterministic byte array of the given length: byte <c>i</c> is <c>start + i</c>. Used to
    /// build fixed keys, IVs, and nonces so the sample reproduces on every run.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <param name="start">The value of the first byte.</param>
    /// <returns>A newly allocated, deterministically filled array.</returns>
    public static byte[] Fill(int length, byte start)
    {
        var array = new byte[length];
        for (var i = 0; i < length; i++)
            array[i] = unchecked((byte)(start + i));

        return array;
    }
}
