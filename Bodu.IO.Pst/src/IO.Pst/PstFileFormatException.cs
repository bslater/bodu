// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Represents an error raised when a PST file's structure is malformed or fails validation.
/// </summary>
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
}
