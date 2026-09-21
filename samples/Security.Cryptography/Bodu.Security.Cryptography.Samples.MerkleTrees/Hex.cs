// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.MerkleTrees;

/// <summary>
/// Provides the lowercase-hex formatting helpers shared by the scenarios. Roots and proof steps are 32-byte
/// SHA-256 digests, so most lines print an abbreviated prefix to stay readable while still making tampering
/// visible; the published vectors print in full so they can be compared against the RFC by eye.
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
    /// Formats the first eight hexadecimal characters of a hash, which is enough to distinguish the values used here.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>An eight-character lowercase prefix followed by an ellipsis.</returns>
    public static string ToShortHex(ReadOnlySpan<byte> bytes) =>
        $"{ToHex(bytes)[..8]}...";
}
