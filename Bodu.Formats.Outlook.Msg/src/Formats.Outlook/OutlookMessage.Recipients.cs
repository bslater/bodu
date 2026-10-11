// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessage.Recipients.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMessage
{
    /// <summary>The lazily materialized recipient list.</summary>
    private OutlookRecipient[]? _recipients;

    /// <summary>
    /// Gets the recipients of the message, in storage-index order.
    /// </summary>
    /// <value>The recipient list; empty when the message declares no recipients.</value>
    /// <exception cref="ObjectDisposedException">The message has been disposed.</exception>
    /// <exception cref="OutlookMsgFormatException">
    /// Thrown under strict validation when the recipient storages are inconsistent with the declared count, or a
    /// recipient storage has no property stream.
    /// </exception>
    /// <remarks>
    /// Under the tolerant validation levels a recipient storage with no <c>__properties_version1.0</c> stream is
    /// omitted from the list.
    /// </remarks>
    public IReadOnlyList<OutlookRecipient> Recipients
    {
        get
        {
            ThrowIfDisposed();

            return _recipients ??= BuildRecipients();
        }
    }

    /// <summary>
    /// Materializes the recipient list from the indexed child storages.
    /// </summary>
    /// <returns>The decoded recipients in index order.</returns>
    private OutlookRecipient[] BuildRecipients()
    {
        List<CompoundStorage> storages = MsgStorageWalker.EnumerateIndexed(
            _storage, MsgStreamNames.RecipientStoragePrefix, _header.RecipientCount, _options.ValidationLevel);

        var recipients = new OutlookRecipient[storages.Count];
        for (int i = 0; i < storages.Count; i++)
        {
            MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
                storages[i], MsgPropertyStreamKind.RecipientOrAttachment, _options.ValidationLevel, _stringEncoding, out _);
            recipients[i] = new OutlookRecipient(properties);
        }

        return recipients;
    }
}
