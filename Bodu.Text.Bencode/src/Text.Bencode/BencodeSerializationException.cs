// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// The exception thrown when a value cannot be bound to or from a Bencode document during serialization — for example a
/// type mismatch, a missing required member, or a value Bencode cannot represent.
/// </summary>
/// <remarks>
/// <para>
/// This exception reports a binding-level failure and is distinct from <see cref="BencodeFormatException" />, which
/// reports that the source bytes were not well-formed Bencode. When the failure can be traced to a position in the
/// source bytes, <see cref="BytesOffset" /> carries it.
/// </para>
/// <para>
/// The two causes are deliberately kept distinct; catch both exception types when the handling should not distinguish
/// malformed bytes from unmappable values.
/// </para>
/// </remarks>
public sealed class BencodeSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializationException" /> class.
    /// </summary>
    public BencodeSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BencodeSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializationException" /> class with the specified message
    /// and a reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public BencodeSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializationException" /> class with the specified message
    /// and source byte offset.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="bytesOffset">The byte offset into the source at which the error was detected.</param>
    public BencodeSerializationException(string message, int bytesOffset)
        : base(message)
    {
        BytesOffset = bytesOffset;
    }

    /// <summary>
    /// Gets the byte offset into the source at which the error was detected, when available.
    /// </summary>
    /// <returns>The byte offset, or <see langword="null" /> when the failure has no associated position.</returns>
    public int? BytesOffset { get; }
}
