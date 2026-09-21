// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.CipherModesAndPadding;

/// <summary>
/// Provides the byte-formatting helpers shared by the scenarios.
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
    /// Returns an array of <paramref name="length" /> bytes all set to <paramref name="value" />.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <param name="value">The byte to repeat.</param>
    /// <returns>The filled array.</returns>
    public static byte[] Fill(int length, byte value) =>
        Enumerable.Repeat(value, length).ToArray();

    /// <summary>
    /// Formats a byte sequence as lowercase hex grouped into <paramref name="blockSize" />-byte blocks, so a
    /// block-oriented mode's structure is visible at a glance.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="blockSize">The block size, in bytes.</param>
    /// <returns>The grouped lowercase hex representation.</returns>
    public static string ToBlocks(ReadOnlySpan<byte> bytes, int blockSize)
    {
        var hex = ToHex(bytes);
        var groups = new List<string>();
        for (var offset = 0; offset < hex.Length; offset += blockSize * 2)
            groups.Add(hex.Substring(offset, Math.Min(blockSize * 2, hex.Length - offset)));

        return string.Join(" ", groups);
    }
}
