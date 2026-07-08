// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// The exception that is thrown when a delimited-text source contains a structural error that prevents it from being
/// parsed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="Delimited.Parse(ReadOnlySpan{char})" /> and <see cref="DelimitedReader" /> when the source
/// cannot be interpreted as valid delimited text under the configured dialect — for example, an unterminated quoted
/// field, an unexpected character following a closing quote, or a row whose field count differs from the header when
/// strict header enforcement is enabled.
/// </para>
/// <para>
/// <see cref="TextFormatException.LineNumber" /> carries the 1-based source line on which the parser detected the
/// failure when known, or <c>0</c> when the line cannot be identified.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// try
/// {
///     DelimitedDocument doc = Delimited.Parse(source);
/// }
/// catch (DelimitedFormatException ex)
/// {
///     Console.Error.WriteLine($"Line {ex.LineNumber}: {ex.Message}");
/// }
///]]>
/// </code>
/// </example>
public sealed class DelimitedFormatException
    : TextFormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with a default message and no
    /// associated line number.
    /// </summary>
    public DelimitedFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message and
    /// no associated line number.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public DelimitedFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message and
    /// inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public DelimitedFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatException" /> class with the specified message and
    /// the 1-based line number on which the error occurred.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number on which the parse error was detected.</param>
    public DelimitedFormatException(string message, int lineNumber)
        : base(message, lineNumber)
    {
    }

    private DelimitedFormatException(string message, int lineNumber, int? offset = null)
        : base(message, lineNumber, offset)
    {
    }

    private DelimitedFormatException(string message, int lineNumber, int columnNumber, int? offset)
        : base(message, lineNumber, columnNumber, offset)
    {
    }
}
