// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Represents an error raised when a PST file's structure is malformed or fails validation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "Provides the standard constructors and both error-code overloads; the base (message, innerException, error) ordering exists for the fixed-code subclasses, and mirroring it would add a second public overload with the same meaning.")]
public sealed class PstFileFormatException
    : PstFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileFormatException" /> class.
    /// </summary>
    public PstFileFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileFormatException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PstFileFormatException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileFormatException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public PstFileFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileFormatException" /> class with a message and an error
    /// category.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="error">The error category.</param>
    public PstFileFormatException(string? message, PstFileError error)
        : base(message, error)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileFormatException" /> class with a message, an error category,
    /// and the exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="error">The category of container defect the exception reports.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PstFileFormatException(string? message, PstFileError error, Exception? innerException)
        : base(message, error, innerException)
    {
    }
}
