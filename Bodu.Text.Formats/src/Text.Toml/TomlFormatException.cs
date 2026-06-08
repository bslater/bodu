// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents an error that occurs when TOML data is malformed or violates the TOML v1.1.0 specification.
/// </summary>
/// <remarks>
/// <para>
/// Raised by the TOML parser when the source text cannot be interpreted as a valid TOML document — for example, an
/// unterminated string, a duplicate key, a redefinition of a table, or an out-of-range integer. The error is signalled
/// through the <see cref="FormatException" /> hierarchy so callers can catch it alongside other parse failures.
/// </para>
/// <para>
/// <see cref="TextFormatException.LineNumber" /> carries the 1-based source line that triggered the failure when the
/// parser was able to pinpoint it, or <c>0</c> when the error is not associated with a specific line.
/// </para>
/// </remarks>
public sealed class TomlFormatException
    : TextFormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class.
    /// </summary>
    public TomlFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TomlFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TomlFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFormatException" /> class with an associated source line.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">
    /// The 1-based line number at which the error occurred, or <c>0</c> if the error is not associated with a specific
    /// line.
    /// </param>
    public TomlFormatException(string message, int lineNumber)
        : base(message, lineNumber)
    {
    }
}
