// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8Content.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads a stream from its position to its end and disposes it, as a <see cref="StreamReader" /> over it would.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The bytes from the stream's position to its end.</returns>
    internal static byte[] ReadAll(Stream stream)
    {
        throw new NotImplementedException();
    }
}
