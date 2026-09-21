// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Samples.SpecializedStructures;

/// <summary>
/// Formats hash values for display. Merkle roots and proof steps are 32-byte SHA-256 digests, so the scenarios print
/// an abbreviated prefix to keep the console output readable while still making tampering visible.
/// </summary>
public static class Hex
{
    /// <summary>
    /// Returns the full lowercase hexadecimal encoding of a hash.
    /// </summary>
    /// <param name="value">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal encoding.</returns>
    public static string Full(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(value).ToLowerInvariant();

    /// <summary>
    /// Returns the first eight hexadecimal characters of a hash, which is enough to distinguish the values used here.
    /// </summary>
    /// <param name="value">The bytes to encode.</param>
    /// <returns>An eight-character lowercase prefix followed by an ellipsis.</returns>
    public static string Short(ReadOnlySpan<byte> value) =>
        $"{Full(value)[..8]}...";
}
