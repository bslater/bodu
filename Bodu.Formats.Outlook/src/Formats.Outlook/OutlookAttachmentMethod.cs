// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentMethod.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Specifies how an attachment's content is stored or referenced (the <c>PidTagAttachMethod</c> values).
/// </summary>
public enum OutlookAttachmentMethod
{
    /// <summary>
    /// The attachment carries no content (<c>afNone</c>).
    /// </summary>
    None = 0,

    /// <summary>
    /// The content is stored by value as a byte payload (<c>afByValue</c>).
    /// </summary>
    ByValue = 1,

    /// <summary>
    /// The content is referenced by a path resolved by the recipient (<c>afByReference</c>).
    /// </summary>
    ByReference = 2,

    /// <summary>
    /// The content is referenced by a path resolved at send time (<c>afByReferenceResolve</c>).
    /// </summary>
    ByReferenceResolve = 3,

    /// <summary>
    /// The content is referenced by a path and never sent (<c>afByReferenceOnly</c>).
    /// </summary>
    ByReferenceOnly = 4,

    /// <summary>
    /// The content is an embedded message (<c>afEmbeddedMessage</c>).
    /// </summary>
    EmbeddedMessage = 5,

    /// <summary>
    /// The content is an embedded OLE object (<c>afStorage</c>).
    /// </summary>
    Ole = 6,
}
