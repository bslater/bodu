// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// The exception thrown when a byte sequence is not a well-formed Bencode document.
/// </summary>
/// <remarks>
/// This exception reports a parse-level failure. Binding failures that occur after a document has parsed successfully
/// are reported by <see cref="FormatSerializationException" />.
/// </remarks>
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
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BencodeFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class with the specified message and a
    /// reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public BencodeFormatException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class with the specified message and
    /// byte offset.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The zero-based byte offset at which the error was detected.</param>
    public BencodeFormatException(string message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the zero-based byte offset at which the error was detected, when available.
    /// </summary>
    /// <returns>The byte offset, or <see langword="null" /> when no position is associated with the error.</returns>
    public int? Offset { get; }
}
