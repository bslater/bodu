// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Represents an error that occurs when INI data is malformed, such as an unterminated section header, an empty section
/// name, or an entry with no assignment.
/// </summary>
public sealed class IniFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class.
    /// </summary>
    public IniFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IniFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public IniFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class and records the source location at
    /// which the parse error was detected.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number at which the error occurred.</param>
    /// <param name="offset">The zero-based byte offset from the start of the source.</param>
    public IniFormatException(string message, int line, int offset)
        : base(message)
    {
        LineNumber = line;
        Offset = offset;
    }

    /// <summary>
    /// Gets the 1-based line number at which the parse error was detected, when available.
    /// </summary>
    /// <value>The line number, or <see langword="null" /> when no line is associated with the error.</value>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the zero-based byte offset at which the parse error was detected, when available.
    /// </summary>
    /// <value>The byte offset, or <see langword="null" /> when no position is associated with the error.</value>
    public int? Offset { get; }
}
