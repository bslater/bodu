// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// The exception thrown when a notable-date binary rule pack cannot be read: the input is not a pack, declares an
/// unsupported format version, is truncated or corrupted, or carries a value outside the sealed format's tables.
/// </summary>
/// <seealso cref="NotableDateBinaryResource" />
public sealed class NotableDateBinaryFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateBinaryFormatException" /> class.
    /// </summary>
    /// <param name="message">The message describing the format violation.</param>
    public NotableDateBinaryFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateBinaryFormatException" /> class with an inner exception.
    /// </summary>
    /// <param name="message">The message describing the format violation.</param>
    /// <param name="innerException">The underlying failure.</param>
    public NotableDateBinaryFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateBinaryFormatException" /> class.
    /// </summary>
    public NotableDateBinaryFormatException()
    {
    }
}
