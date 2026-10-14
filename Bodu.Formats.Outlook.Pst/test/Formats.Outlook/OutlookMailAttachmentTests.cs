// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailAttachment" />, the attachment view, over the synthetic mail store's
/// by-value and embedded-message attachments.
/// </summary>
[TestClass]
public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Retrieves the synthetic full message's attachments: the by-value attachment first, the embedded-message
    /// attachment second.
    /// </summary>
    /// <param name="store">The open synthetic session.</param>
    /// <returns>The attachment views.</returns>
    internal static IReadOnlyList<OutlookMailAttachment> GetAttachments(OutlookMailStore store) =>
        OutlookMailMessageTests.GetFullMessage(store).Attachments;

    /// <summary>
    /// Finds the identifier of a message's first attachment object, a subnode of the message.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="messageNodeId">The message node identifier.</param>
    /// <returns>The attachment object's subnode identifier.</returns>
    internal static uint FindAttachmentObjectId(byte[] file, uint messageNodeId)
    {
        using PstFile container = PstFile.Open(new MemoryStream(file, writable: false), new PstFileOptions());

        return container.GetNode(new PstNodeId(messageNodeId))
            .EnumerateSubnodes()
            .First(static subnode => subnode.NodeId.Type == PstNodeType.Attachment)
            .NodeId.Value;
    }
}
