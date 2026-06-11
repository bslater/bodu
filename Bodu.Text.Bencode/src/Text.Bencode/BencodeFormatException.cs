// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents an error that occurs when bencoded data is malformed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="Utf8BencodeReader.Read" /> — and therefore surfaced by <see cref="BencodeSerializer" /> while
/// deserializing — when the input cannot be interpreted as a valid bencoded document: for example, when an unexpected
/// prefix byte is encountered, an integer is missing its terminating <c>e</c>, a string's length prefix does not fit
/// the remaining bytes, a dictionary's keys are not in lexicographic order, or the document contains trailing bytes
/// after a complete value.
/// </para>
/// <para>
/// Malformed input and binding failures are reported separately: catch <see cref="BencodeFormatException" /> (a
/// <see cref="FormatException" /> ) for bytes that are not valid Bencode, and
/// <see cref="BencodeSerializationException" /> for values or documents that cannot be mapped; catch both when the
/// handling should not distinguish the cause.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// try
/// {
///     TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(payload);
/// }
/// catch (BencodeFormatException ex)
/// {
///     Console.Error.WriteLine($"Malformed bencode at byte {ex.Offset}: {ex.Message}");
/// }
///]]>
/// </example>
public sealed class BencodeFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    public BencodeFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BencodeFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BencodeFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class and records the byte offset at
    /// which the parse error was detected.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="offset">The zero-based byte offset from the start of the source.</param>
    public BencodeFormatException(string message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the zero-based byte offset at which the parse error was detected, when available.
    /// </summary>
    /// <returns>The byte offset, or <see langword="null" /> when no position is associated with the error.</returns>
    public int? Offset { get; }
}
