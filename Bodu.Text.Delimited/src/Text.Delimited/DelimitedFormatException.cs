// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Represents an error that occurs when delimited (CSV/TSV) data is malformed, such as an unterminated quoted field or
/// a record whose field count violates the configured policy.
/// </summary>
public sealed class DelimitedFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class.
    /// </summary>
    public DelimitedFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DelimitedFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DelimitedFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class and records the source location
    /// at which the parse error was detected.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number at which the error occurred.</param>
    /// <param name="offset">The zero-based byte offset from the start of the source.</param>
    public DelimitedFormatException(string message, int line, int offset)
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
