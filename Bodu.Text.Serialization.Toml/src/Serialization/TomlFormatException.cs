// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// The exception thrown when text is not a well-formed TOML document.
/// </summary>
/// <remarks>
/// This exception reports a parse-level failure and carries the source line, column, and offset at which the error was
/// detected. Binding failures that occur after a document has parsed successfully are reported by
/// <see cref="FormatSerializationException" />.
/// </remarks>
public sealed class TomlFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class.
    /// </summary>
    public TomlFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TomlFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class with the specified message and a
    /// reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public TomlFormatException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class with the specified message and source
    /// position.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="line">The one-based source line at which the error was detected.</param>
    /// <param name="column">The one-based source column at which the error was detected.</param>
    /// <param name="offset">The zero-based source offset at which the error was detected.</param>
    public TomlFormatException(string message, int line, int column, int offset)
        : base(message)
    {
        Line = line;
        Column = column;
        Offset = offset;
    }

    /// <summary>
    /// Gets the one-based source line at which the error was detected, when available.
    /// </summary>
    /// <returns>The source line, or <see langword="null" />.</returns>
    public int? Line { get; }

    /// <summary>
    /// Gets the one-based source column at which the error was detected, when available.
    /// </summary>
    /// <returns>The source column, or <see langword="null" />.</returns>
    public int? Column { get; }

    /// <summary>
    /// Gets the zero-based source offset at which the error was detected, when available.
    /// </summary>
    /// <returns>The source offset, or <see langword="null" />.</returns>
    public int? Offset { get; }
}
