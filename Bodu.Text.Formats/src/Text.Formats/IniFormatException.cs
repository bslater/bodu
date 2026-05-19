// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

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
/// <see cref="LineNumber" /> carries the 1-based source line that triggered the failure when the parser was able to
/// pinpoint it, or <c>0</c> when the error is not associated with a specific line (for example, when the source is
/// completely empty under a policy that requires at least one section).
/// </para>
/// </remarks>
/// <example>
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
/// </example>
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
    /// Initializes a new instance of the <see cref="IniFormatException" /> class with an associated source line.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">
    /// The 1-based line number at which the error occurred, or <c>0</c> if the error is not associated with a specific
    /// line.
    /// </param>
    public IniFormatException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the 1-based line number at which the parse error occurred.
    /// </summary>
    /// <returns>
    /// The 1-based line number, or <c>0</c> when the exception is not associated with a specific source line.
    /// </returns>
    public int LineNumber { get; }
}
