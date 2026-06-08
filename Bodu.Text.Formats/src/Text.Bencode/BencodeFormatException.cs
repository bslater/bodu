// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents an error that occurs when bencoded data is malformed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="Bencode.Parse(ReadOnlySpan{byte})" /> when the input cannot be interpreted as a valid bencoded
/// document — for example, when an unexpected prefix byte is encountered, an integer is missing its terminating
/// <c>e</c>, a string's length prefix does not fit the remaining bytes, a dictionary's keys are not in lexicographic
/// order, or the document contains trailing bytes after a complete value. The error is signalled through the
/// <see cref="TextFormatException" /> hierarchy so callers can catch it alongside other parse failures.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// try
/// {
///     BencodedValue root = Bencode.Parse(payload);
/// }
/// catch (BencodeFormatException ex)
/// {
///     Console.Error.WriteLine($"Malformed bencode at byte {ex.Offset}: {ex.Message}");
/// }
///]]>
/// </example>
public sealed class BencodeFormatException
    : TextFormatException
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
    /// <param name="offset">The 0-based byte offset from the start of the source.</param>
    public BencodeFormatException(string message, int offset)
        : base(message, lineNumber: 0, offset: offset)
    {
    }
}
