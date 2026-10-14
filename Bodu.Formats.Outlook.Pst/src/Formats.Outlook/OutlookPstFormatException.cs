// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookPstFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents an error raised when a mail store (a <c>.pst</c> file) is structurally invalid or violates the Outlook
/// personal-folders messaging conventions (MS-PST).
/// </summary>
/// <remarks>
/// <para>
/// This exception reports messaging-level problems - a folder or message object that violates the format's structural
/// conventions, a table row that does not reference a valid node, a malformed name-to-id map, a tripped limit - and
/// wraps container-level ones. A source that is not a PST file, a variant the reader does not support, and corruption
/// in the container's pages, blocks, B-trees, heaps, or tables surface as this exception too, with the
/// <c>Bodu.IO.Pst</c> <c>PstFileException</c> (a <c>PstFileFormatException</c>, <c>PstUnsupportedFormatException</c>,
/// or <c>PstNodeNotFoundException</c>, whose <c>Error</c> names the category) as the inner exception.
/// </para>
/// <para>
/// A single handler for <see cref="OutlookFormatException" /> therefore catches every failure to read either Outlook
/// format, as <see cref="OutlookFormatException" /> documents.
/// </para>
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
