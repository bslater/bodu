// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Represents the decoded header of an MS-OXMSG property stream.
/// </summary>
/// <remarks>
/// The next-identifier and count fields are present for the <see cref="MsgPropertyStreamKind.Root" /> and
/// <see cref="MsgPropertyStreamKind.EmbeddedMessage" /> kinds; a recipient or attachment stream carries only reserved
/// bytes, surfaced here as zeros.
/// </remarks>
/// <param name="NextRecipientId">The identifier the writer would assign to the next recipient.</param>
/// <param name="NextAttachmentId">The identifier the writer would assign to the next attachment.</param>
/// <param name="RecipientCount">The number of recipient storages the message declares.</param>
/// <param name="AttachmentCount">The number of attachment storages the message declares.</param>
internal readonly record struct MsgPropertyStreamHeader(
    uint NextRecipientId,
    uint NextAttachmentId,
    uint RecipientCount,
    uint AttachmentCount);
