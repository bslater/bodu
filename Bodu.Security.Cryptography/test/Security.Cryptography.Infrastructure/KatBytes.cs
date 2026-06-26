// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides terse byte-decoding helpers for declaring known-answer test vectors. Intended to be imported with
/// <c>using static</c> so vector literals read as <c>Digest = Hex("a3817f04…")</c>.
/// </summary>
public static class KatBytes
{
    /// <summary>
    /// Decodes a hex-encoded string into a byte array, treating the empty string as a zero-length array.
    /// </summary>
    /// <param name="hex">The hex-encoded value. Must contain an even number of hex characters.</param>
    /// <returns>The decoded bytes, or an empty array when <paramref name="hex" /> is empty.</returns>
    public static byte[] Hex(string hex) =>
        hex.Length == 0 ? [] : Convert.FromHexString(hex);
}
