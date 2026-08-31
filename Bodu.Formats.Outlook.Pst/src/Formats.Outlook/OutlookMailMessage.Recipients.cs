// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessage.Recipients.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailMessage
{
    /// <summary>The lazily built recipient views.</summary>
    private OutlookRecipient[]? _recipients;

    /// <summary>
    /// Gets the message's recipients, in recipient-table order.
    /// </summary>
    /// <value>The recipient views, built once on first access; empty when the message has no recipient table.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, a recipient row carries an undecodable value.
    /// </exception>
    /// <remarks>
    /// Recipients are row-resident: each recipient-table row's cells decode directly into the recipient's property
    /// collection, with code-page strings decoded under the message's encoding.
    /// </remarks>
    public IReadOnlyList<OutlookRecipient> Recipients =>
        _recipients ??= BuildRecipients();

    /// <summary>
    /// Decodes the recipient table's rows into recipient views.
    /// </summary>
    /// <returns>The recipients in table order; empty when the message carries no recipient table.</returns>
    private OutlookRecipient[] BuildRecipients()
    {
        if (!TryGetSubnodeOfType(PstNodeType.RecipientTable, out PstNode? tableNode))
            return [];

        var recipients = new List<OutlookRecipient>();
        foreach (PstTableRow row in tableNode.ReadTableContext().EnumerateRows())
            recipients.Add(new OutlookRecipient(PstMapiPropertyReader.ReadRow(row, MessageEncoding, _store.Strict)));

        return [.. recipients];
    }
}
