// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Identifies which MS-OXMSG storage a property stream belongs to, which determines the size and content of its header.
/// </summary>
internal enum MsgPropertyStreamKind
{
    /// <summary>
    /// The top-level message storage; the header is 32 bytes and carries the next-identifier and count fields.
    /// </summary>
    Root = 0,

    /// <summary>
    /// An embedded-message storage; the header is 24 bytes and carries the next-identifier and count fields.
    /// </summary>
    EmbeddedMessage = 1,

    /// <summary>
    /// A recipient or attachment storage; the header is 8 reserved bytes with no fields.
    /// </summary>
    RecipientOrAttachment = 2,
}
