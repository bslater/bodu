// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Represents an error that occurs when INI data is malformed or violates the configured parsing policy.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="Ini.Parse(ReadOnlySpan{char})" /> and the overloads that accept an
/// <see cref="IniParseOptions" /> when the source text cannot be interpreted as valid INI under the configured policy —
/// for example, an unterminated section header, a duplicate key in strict mode, or an entry outside any named section
/// when the global section is disabled. The error is signalled through the <see cref="FormatException" /> hierarchy so
/// callers can catch it alongside other parse failures.
/// </para>
/// <para>
/// <see cref="TextFormatException.LineNumber" /> carries the 1-based source line that triggered the failure when the
/// parser was able to pinpoint it, or <c>0</c> when the error is not associated with a specific line (for example, when
/// the source is completely empty under a policy that requires at least one section).
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// try
/// {
///     IniDocument doc = Ini.Parse(source);
/// }
/// catch (IniFormatException ex)
/// {
///     Console.Error.WriteLine($"Line {ex.LineNumber}: {ex.Message}");
/// }
///]]>
/// </code>
/// </example>
public sealed class IniFormatException
    : TextFormatException
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
    /// Initializes a new instance of the <see cref="IniFormatException" /> class with an associated source line.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">
    /// The 1-based line number at which the error occurred, or <c>0</c> if the error is not associated with a specific
    /// line.
    /// </param>
    public IniFormatException(string message, int lineNumber)
        : base(message, lineNumber)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class with the specified message, line
    /// number, and source offset.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The 1-based line number at which the error occurred, or <c>0</c> when unknown.</param>
    /// <param name="offset">
    /// The 0-based offset from the start of the source at which the parse error was detected, or
    /// <see langword="null" /> when unknown.
    /// </param>
    private IniFormatException(string message, int lineNumber, int? offset = null)
        : base(message, lineNumber, offset)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFormatException" /> class with the specified message, line
    /// number, column number, and source offset.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The 1-based line number at which the error occurred, or <c>0</c> when unknown.</param>
    /// <param name="columnNumber">
    /// The 1-based column at which the parse error was detected, or <c>0</c> when unknown.
    /// </param>
    /// <param name="offset">
    /// The 0-based offset from the start of the source at which the parse error was detected, or
    /// <see langword="null" /> when unknown.
    /// </param>
    private IniFormatException(string message, int lineNumber, int columnNumber, int? offset)
        : base(message, lineNumber, columnNumber, offset)
    {
    }
}
