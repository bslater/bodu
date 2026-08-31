// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstUnsupportedFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Represents an error raised when a PST file is recognized but uses a format variant or content encoding the library
/// does not read yet (the ANSI format, the 4&#160;KiB-page OST variant, or Windows Information Protection encryption).
/// </summary>
public sealed class PstUnsupportedFormatException
    : PstFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PstUnsupportedFormatException" /> class.
    /// </summary>
    public PstUnsupportedFormatException()
        : base(null, PstFileError.UnsupportedFormat)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstUnsupportedFormatException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PstUnsupportedFormatException(string? message)
        : base(message, PstFileError.UnsupportedFormat)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstUnsupportedFormatException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public PstUnsupportedFormatException(string? message, Exception? innerException)
        : base(message, innerException, PstFileError.UnsupportedFormat)
    {
    }
}
