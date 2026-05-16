// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// The exception that is thrown when a delimited-text source contains a structural error that prevents it from
/// being parsed.
/// </summary>
public sealed class DelimitedFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with a default message and
    /// no associated line number.
    /// </summary>
    public DelimitedFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message
    /// and no associated line number.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public DelimitedFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message
    /// and inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public DelimitedFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message
    /// and the 1-based line number on which the error occurred.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number on which the parse error was detected.</param>
    public DelimitedFormatException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the 1-based line number at which the parse error was detected, or <c>0</c> when the error is not
    /// associated with a specific line.
    /// </summary>
    /// <returns>
    /// A positive integer identifying the source line, or <c>0</c> when the line is unknown or not applicable.
    /// </returns>
    public int LineNumber { get; }
}
