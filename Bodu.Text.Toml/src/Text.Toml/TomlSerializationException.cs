// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// The exception thrown when a value cannot be bound to or from a TOML document during serialization — for example a
/// type mismatch, a missing required member, or a value TOML cannot represent.
/// </summary>
/// <remarks>
/// This exception reports a binding-level failure and is distinct from <see cref="TomlFormatException" />, which
/// reports that the source text was not well-formed TOML. When the failure can be traced to a position in the source,
/// the <see cref="Offset" /> carries it.
/// </remarks>
public sealed class TomlSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class.
    /// </summary>
    public TomlSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TomlSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message and
    /// a reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public TomlSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message and
    /// source offset.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The byte offset into the source at which the error was detected.</param>
    public TomlSerializationException(string message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the byte offset into the source at which the error was detected, when available.
    /// </summary>
    /// <returns>The byte offset, or <see langword="null" /> when the failure has no associated position.</returns>
    public int? Offset { get; }
}
