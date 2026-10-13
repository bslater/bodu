// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8Content.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Unicode;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Hands a downloaded text response to a UTF-8 parser as the bytes a <see cref="StreamReader" /> would decode it to,
/// without copying a response that is already plain UTF-8.
/// </summary>
/// <remarks>
/// This type is compiled into each provider that parses a delimited text response.
/// </remarks>
internal static class Utf8Content
{
    /// <summary>
    /// Returns the UTF-8 text of a response: the response itself, less a leading UTF-8 byte-order mark, when it is
    /// valid UTF-8; otherwise the UTF-8 bytes of the text a <see cref="StreamReader" /> decodes it to, honoring a
    /// UTF-16 or UTF-32 byte-order mark and replacing each invalid sequence.
    /// </summary>
    /// <param name="content">The response bytes.</param>
    /// <returns>
    /// The UTF-8 text of the response, which is a slice of <paramref name="content" /> when the response is valid
    /// UTF-8.
    /// </returns>
    internal static ReadOnlySpan<byte> Normalize(ReadOnlySpan<byte> content)
    {
        // Bytes 0xFE and 0xFF never occur in UTF-8, so a response led by a UTF-16 or UTF-32 byte-order mark is never
        // valid UTF-8 and is decoded the way StreamReader decodes it.
        ReadOnlySpan<byte> text = content.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? content[3..] : content;
        if (Utf8.IsValid(text))
            return text;

        using StreamReader reader = new(new MemoryStream(content.ToArray(), writable: false));
        return Encoding.UTF8.GetBytes(reader.ReadToEnd());
    }

    /// <summary>
    /// Reads a stream from its position to its end and disposes it, as a <see cref="StreamReader" /> over it would.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The bytes from the stream's position to its end.</returns>
    internal static byte[] ReadAll(Stream stream)
    {
        using (stream)
        {
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
