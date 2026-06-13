// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.Hex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class CryptographyHelper
{
    /// <summary>
    /// The lowercase hexadecimal digit alphabet used by <see cref="ToLowercaseHexString(ReadOnlySpan{byte})" />.
    /// </summary>
    private const string LowercaseHexAlphabet = "0123456789abcdef";

    /// <summary>
    /// Formats the provided bytes as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to format.</param>
    /// <returns>
    /// A lowercase hexadecimal string of length <c>2 × bytes.Length</c>, or <see cref="string.Empty" /> when
    /// <paramref name="bytes" /> is empty.
    /// </returns>
    /// <remarks>
    /// .NET 8 offers no <c>Convert.ToHexStringLower</c>; this helper writes the digits directly to avoid the double
    /// allocation of <see cref="Convert.ToHexString(ReadOnlySpan{byte})" /> followed by a lowercase copy.
    /// </remarks>
    internal static string ToLowercaseHexString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        Span<char> buffer = bytes.Length <= 256
            ? stackalloc char[bytes.Length * 2]
            : new char[bytes.Length * 2];

        for (var i = 0; i < bytes.Length; i++)
        {
            buffer[i * 2] = LowercaseHexAlphabet[bytes[i] >> 4];
            buffer[(i * 2) + 1] = LowercaseHexAlphabet[bytes[i] & 0x0F];
        }

        return new string(buffer);
    }

    /// <summary>
    /// Attempts to decode a hexadecimal character sequence into a newly allocated byte array.
    /// </summary>
    /// <param name="text">The hexadecimal text to decode. Both uppercase and lowercase digits are accepted.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the decoded bytes; otherwise an empty array.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="text" /> has even length and contains only ASCII hexadecimal digits;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Parsing is strict: no whitespace, separators, or <c>0x</c> prefixes are tolerated. An empty input decodes
    /// successfully to an empty array.
    /// </remarks>
    internal static bool TryFromHexString(ReadOnlySpan<char> text, out byte[] bytes)
    {
        bytes = [];

        if ((text.Length & 1) != 0)
            return false;

        foreach (var c in text)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }

        if (!text.IsEmpty)
            bytes = Convert.FromHexString(text);

        return true;
    }
}
