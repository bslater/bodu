// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.ToUtf8Bytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> into a freshly allocated UTF-8 byte array.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <returns>A new byte array containing the UTF-8 representation of <paramref name="text" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    ///<![CDATA[
    /// // Convert a configuration value to UTF-8 bytes for hashing.
    /// byte[] payload = "client-secret".ToUtf8Bytes();
    /// byte[] hash    = SHA256.HashData(payload);
    ///]]>
    /// </example>
    public static byte[] ToUtf8Bytes(this string text)
    {
        ThrowHelper.ThrowIfNull(text);

        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
