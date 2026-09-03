// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessage.Attachments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailMessage
{
    /// <summary>The lazily built attachment views.</summary>
    private OutlookMailAttachment[]? _attachments;

    /// <summary>
    /// Gets the message's attachments, in attachment-table order.
    /// </summary>
    /// <value>The attachment views, built once on first access; empty when the message has no attachment table.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, an attachment-table row does not reference a valid attachment object subnode.
    /// </exception>
    /// <remarks>
    /// Each attachment-table row's identifier is the attachment object's subnode identifier; a row that references no
    /// attachment subnode is skipped under the tolerant levels.
    /// </remarks>
    public IReadOnlyList<OutlookMailAttachment> Attachments
    {
        get
        {
            _store.ThrowIfDisposed();

            return _attachments ??= BuildAttachments();
        }
    }

    /// <summary>
    /// Resolves the attachment table's rows into attachment views over their object subnodes.
    /// </summary>
    /// <returns>The attachments in table order; empty when the message carries no attachment table.</returns>
    private OutlookMailAttachment[] BuildAttachments()
    {
        if (!TryGetSubnodeOfType(PstNodeType.AttachmentTable, out PstNode? tableNode))
            return [];

        var attachments = new List<OutlookMailAttachment>();
        foreach (PstTableRow row in tableNode.ReadTableContext().EnumerateRows())
        {
            var attachmentId = new PstNodeId(row.RowId);
            if (attachmentId.Type != PstNodeType.Attachment || !_node.TryGetSubnode(attachmentId, out PstNode? attachmentNode))
            {
                if (_store.Strict)
                {
                    throw new OutlookPstFormatException(string.Format(
                        CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstAttachmentObject, attachmentId, _node.Id));
                }

                continue;
            }

            attachments.Add(new OutlookMailAttachment(_store, this, attachmentNode));
        }

        return [.. attachments];
    }
}
