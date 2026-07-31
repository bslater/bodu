// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookRecipientType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Specifies how a recipient participates in a message (the <c>PidTagRecipientType</c> values).
/// </summary>
public enum OutlookRecipientType
{
    /// <summary>
    /// The originator of the message (<c>MAPI_ORIG</c>).
    /// </summary>
    Originator = 0,

    /// <summary>
    /// A primary recipient (<c>MAPI_TO</c>).
    /// </summary>
    To = 1,

    /// <summary>
    /// A carbon-copy recipient (<c>MAPI_CC</c>).
    /// </summary>
    Cc = 2,

    /// <summary>
    /// A blind-carbon-copy recipient (<c>MAPI_BCC</c>).
    /// </summary>
    Bcc = 3,
}
