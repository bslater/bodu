// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMsgFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents an error raised when a <c>.msg</c> file is structurally invalid or violates the MS-OXMSG format.
/// </summary>
public sealed class OutlookMsgFormatException
    : OutlookFormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMsgFormatException" /> class.
    /// </summary>
    public OutlookMsgFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMsgFormatException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OutlookMsgFormatException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMsgFormatException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public OutlookMsgFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
