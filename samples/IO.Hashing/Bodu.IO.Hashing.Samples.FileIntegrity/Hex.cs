// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Samples.FileIntegrity;

/// <summary>
/// Provides the hexadecimal helpers the manifest scenarios share.
/// </summary>
public static class Hex
{
    /// <summary>
    /// Decodes a manifest's hexadecimal digest text, returning <see langword="null" /> when the text is malformed.
    /// </summary>
    /// <param name="text">The digest as hexadecimal text.</param>
    /// <returns>The decoded bytes, or <see langword="null" /> when <paramref name="text" /> is not valid hex.</returns>
    public static byte[]? TryDecode(string text)
    {
        try
        {
            return Convert.FromHexString(text);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
