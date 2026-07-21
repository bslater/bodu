// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys;

/// <summary>
/// Provides lowercase-hex encode / decode helpers shared by the scenarios, so imported key material and
/// derived values print in a single deterministic form.
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
    /// Parses a hexadecimal string into a byte array.
    /// </summary>
    /// <param name="hex">The hex string to decode.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] FromHex(string hex) =>
        Convert.FromHexString(hex);
}
