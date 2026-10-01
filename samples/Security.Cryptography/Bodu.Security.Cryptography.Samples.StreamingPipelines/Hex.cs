// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines;

/// <summary>
/// Provides the lowercase-hex formatting helpers shared by the scenarios. The payloads here are hundreds of
/// kilobytes, so ciphertext prints as an abbreviated prefix that is still enough to tell two outputs apart.
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
    /// Formats the first eight bytes of a sequence as lowercase hex followed by an ellipsis.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A sixteen-character lowercase prefix followed by an ellipsis.</returns>
    public static string ToShortHex(ReadOnlySpan<byte> bytes) =>
        $"{ToHex(bytes[..Math.Min(8, bytes.Length)])}...";
}
