// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.HybridEncryption;

/// <summary>
/// Provides the lowercase-hex helpers shared by the scenarios.
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
    /// Decodes a hexadecimal string into bytes.
    /// </summary>
    /// <param name="hex">The hexadecimal text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] FromHex(string hex) =>
        Convert.FromHexString(hex);
}
