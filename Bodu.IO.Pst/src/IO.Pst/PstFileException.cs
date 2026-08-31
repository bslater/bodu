// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Represents an error raised while reading a PST file. Serves as the base of the library's exception hierarchy.
/// </summary>
public class PstFileException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileException" /> class.
    /// </summary>
    public PstFileException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PstFileException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileException" /> class with a message and an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public PstFileException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileException" /> class with a message and an error category.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="error">The error category.</param>
    public PstFileException(string? message, PstFileError error)
        : base(message)
    {
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFileException" /> class with a message, an inner exception,
    /// and an error category.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    /// <param name="error">The error category.</param>
    public PstFileException(string? message, Exception? innerException, PstFileError error)
        : base(message, innerException)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the category of the failure, so callers can distinguish a missing object from structural corruption
    /// without parsing messages.
    /// </summary>
    /// <value>The error category; <see cref="PstFileError.None" /> when the throw site recorded none.</value>
    public PstFileError Error { get; }
}
