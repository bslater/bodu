// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents an error that occurs when DotEnv data is malformed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by the DotEnv reader — and therefore surfaced by <c>DotEnvSerializer</c> while deserializing — when the
/// source text cannot be interpreted as a valid DotEnv document: for example, an invalid key name, a missing
/// assignment, or an unterminated quoted value. The error is signalled through the <see cref="FormatException" />
/// hierarchy so callers can catch it alongside other parse failures.
/// </para>
/// <para>
/// Because DotEnv is a line-oriented format, the exception records its position as a <see cref="LineNumber" /> and
/// <see cref="ColumnNumber" /> in addition to the absolute byte <see cref="Offset" />. Each is <see langword="null" />
/// when the error is not associated with a specific location.
/// </para>
/// </remarks>
public sealed class DotEnvFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class.
    /// </summary>
    public DotEnvFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DotEnvFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DotEnvFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class and records the source location at
    /// which the parse error was detected.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number at which the error occurred.</param>
    /// <param name="column">The 1-based column number within the line at which the error occurred.</param>
    /// <param name="offset">The zero-based byte offset from the start of the source.</param>
    public DotEnvFormatException(string message, int line, int column, int offset)
        : base(message)
    {
        LineNumber = line;
        ColumnNumber = column;
        Offset = offset;
    }

    /// <summary>
    /// Gets the 1-based line number at which the parse error was detected, when available.
    /// </summary>
    /// <value>The line number, or <see langword="null" /> when no line is associated with the error.</value>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the 1-based column number within the line at which the parse error was detected, when available.
    /// </summary>
    /// <value>The column number, or <see langword="null" /> when no column is associated with the error.</value>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the zero-based byte offset from the start of the UTF-8 source at which the parse error was detected, when
    /// available.
    /// </summary>
    /// <value>The byte offset, or <see langword="null" /> when no position is associated with the error.</value>
    /// <remarks>
    /// The offset and <see cref="ColumnNumber" /> count UTF-8 bytes of the source document, not decoded characters; for
    /// ASCII-only documents the two coincide.
    /// </remarks>
    public int? Offset { get; }
}
