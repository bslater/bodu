// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

/// <summary>
/// Base class for parse-failure exceptions raised by the text-format parsers in <c>Bodu.Text.Formats</c>. Provides the
/// common diagnostic surface — line, column, and offset — so callers can write a single handler that works across
/// Bencode, Delimited, DotEnv, and INI sources.
/// </summary>
/// <remarks>
/// <para>
/// The location properties are advisory: each derived parser populates the fields it can identify at the point of
/// failure. <see cref="LineNumber" /> uses <c>0</c> to mean "unknown" for backwards compatibility with the existing
/// derived-type contracts, while <see cref="ColumnNumber" /> and <see cref="Offset" /> use <see langword="null" /> for
/// the same purpose because they are new diagnostics.
/// </para>
/// </remarks>
public abstract class TextFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextFormatException" /> class.
    /// </summary>
    protected TextFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFormatException" /> class with the specified error message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    protected TextFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFormatException" /> class with the specified error message and
    /// inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    protected TextFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFormatException" /> class with the specified error message and
    /// source location.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">
    /// The 1-based line number on which the parse error was detected, or <c>0</c> when unknown.
    /// </param>
    /// <param name="columnNumber">
    /// The 1-based column number on which the parse error was detected, or <see langword="null" /> when unknown.
    /// </param>
    /// <param name="offset">
    /// The 0-based offset from the start of the source at which the parse error was detected, or
    /// <see langword="null" /> when unknown.
    /// </param>
    protected TextFormatException(string message, int lineNumber, int? columnNumber = null, int? offset = null)
        : base(message)
    {
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        Offset = offset;
    }

    /// <summary>
    /// Gets the 1-based column number at which the parse error was detected, or <see langword="null" /> when the parser
    /// cannot identify a specific column.
    /// </summary>
    /// <returns>
    /// A positive integer identifying the source column, or <see langword="null" /> when the column is unknown or not
    /// applicable to the format.
    /// </returns>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the 1-based line number at which the parse error was detected, or <c>0</c> when the error is not associated
    /// with a specific line.
    /// </summary>
    /// <returns>
    /// A positive integer identifying the source line, or <c>0</c> when the line is unknown or not applicable.
    /// </returns>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the 0-based offset from the start of the source at which the parse error was detected, or
    /// <see langword="null" /> when the parser cannot identify a specific offset.
    /// </summary>
    /// <returns>
    /// A non-negative integer identifying the source offset, or <see langword="null" /> when the offset is unknown or
    /// not applicable to the format.
    /// </returns>
    public int? Offset { get; }
}
