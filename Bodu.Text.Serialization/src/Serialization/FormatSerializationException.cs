// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization;

/// <summary>
/// The exception thrown when a value cannot be bound to or from a document during serialization — for example a type
/// mismatch, a missing required member, or a value a format cannot represent.
/// </summary>
/// <remarks>
/// This exception reports a binding-level failure and is distinct from the format-specific parse exceptions, which
/// report that the source text or bytes were not well-formed. When the failure can be traced to a position in the
/// source, <see cref="Location" /> carries it.
/// </remarks>
public sealed class FormatSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSerializationException" /> class.
    /// </summary>
    public FormatSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSerializationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FormatSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSerializationException" /> class with the specified message
    /// and a reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public FormatSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSerializationException" /> class with the specified message
    /// and source location.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="location">The source location at which the error was detected.</param>
    public FormatSerializationException(string message, TextLocation location)
        : base(message)
    {
        Location = location;
    }

    /// <summary>
    /// Gets the source location at which the error was detected, when available.
    /// </summary>
    /// <returns>The source location, or <see langword="null" /> when the failure has no associated position.</returns>
    public TextLocation? Location { get; }
}
