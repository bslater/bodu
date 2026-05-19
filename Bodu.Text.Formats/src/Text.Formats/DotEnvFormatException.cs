// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// The exception that is thrown when a DotEnv source document contains a structural error that prevents it from being
/// parsed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> when the source cannot be interpreted as valid DotEnv text
/// — for example, when a line is not a comment and does not match <c>KEY=VALUE</c>, when a key contains characters
/// outside the <c>[A-Za-z_][A-Za-z0-9_]*</c> pattern, when a quoted value is left unterminated, or when the parser
/// encounters a duplicate key under <see cref="DotEnvDuplicateKeyBehavior.Disallowed" />.
/// </para>
/// <para>
/// <see cref="LineNumber" /> carries the 1-based source line on which the parser detected the failure when known, or
/// <c>0</c> when the line cannot be identified.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// try
/// {
///     DotEnvDocument doc = DotEnv.Parse(source);
/// }
/// catch (DotEnvFormatException ex)
/// {
///     Console.Error.WriteLine($"Line {ex.LineNumber}: {ex.Message}");
/// }
///]]>
/// </example>
public sealed class DotEnvFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class with a default message and no
    /// associated line number.
    /// </summary>
    public DotEnvFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class with the specified message and no
    /// associated line number.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public DotEnvFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class with the specified message and
    /// inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public DotEnvFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvFormatException" /> class with the specified message and the
    /// 1-based line number on which the error occurred.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number on which the parse error was detected.</param>
    public DotEnvFormatException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the 1-based line number at which the parse error was detected, or <c>0</c> when the error is not associated
    /// with a specific line.
    /// </summary>
    /// <returns>
    /// A positive integer identifying the source line, or <c>0</c> when the line is unknown or not applicable.
    /// </returns>
    public int LineNumber { get; }
}
