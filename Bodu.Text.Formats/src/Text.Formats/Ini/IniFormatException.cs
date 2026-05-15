// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents an error that occurs when INI data is malformed or violates the configured parsing policy.
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
    /// Initializes a new instance of the <see cref="IniFormatException" /> class with an associated source line.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The 1-based line number at which the error occurred, or <c>0</c> if the error is not associated with a specific line.</param>
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
