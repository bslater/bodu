// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookPstFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents a violation of the Outlook personal-folders messaging conventions (MS-PST) while reading a mail store.
/// </summary>
/// <remarks>
/// This exception reports messaging-level problems — a folder or message object that violates the format's structural
/// conventions, a table row that does not reference a valid node, a malformed name-to-id map. Container-level
/// corruption (blocks, B-trees, heaps) propagates as the <c>Bodu.IO.Pst</c> <c>PstFileException</c> family instead.
/// </remarks>
public sealed class OutlookPstFormatException
    : OutlookFormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookPstFormatException" /> class.
    /// </summary>
    public OutlookPstFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookPstFormatException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OutlookPstFormatException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookPstFormatException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public OutlookPstFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
