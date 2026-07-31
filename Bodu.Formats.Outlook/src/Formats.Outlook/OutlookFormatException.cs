// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents an error raised when Outlook-format content is structurally invalid or cannot be decoded.
/// </summary>
/// <remarks>
/// This is the base exception of the Outlook format readers; format-specific packages derive from it (for example, the
/// <c>.msg</c> reader's <c>OutlookMsgFormatException</c>) so consumers can catch the family with a single handler.
/// </remarks>
public class OutlookFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookFormatException" /> class.
    /// </summary>
    public OutlookFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookFormatException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OutlookFormatException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookFormatException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public OutlookFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
