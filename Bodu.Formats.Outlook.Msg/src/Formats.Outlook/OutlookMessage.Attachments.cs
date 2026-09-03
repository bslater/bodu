// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessage.Attachments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMessage
{
    /// <summary>The lazily materialized attachment list.</summary>
    private OutlookAttachment[]? _attachments;

    /// <summary>
    /// Gets the attachments of the message, in storage-index order.
    /// </summary>
    /// <value>The attachment list; empty when the message declares no attachments.</value>
    /// <exception cref="ObjectDisposedException">The message has been disposed.</exception>
    /// <exception cref="OutlookMsgFormatException">
    /// Thrown under strict validation when the attachment storages are inconsistent with the declared count.
    /// </exception>
    public IReadOnlyList<OutlookAttachment> Attachments
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _attachments ??= BuildAttachments();
        }
    }

    /// <summary>
    /// Materializes the attachment list from the indexed child storages.
    /// </summary>
    /// <returns>The decoded attachments in index order.</returns>
    private OutlookAttachment[] BuildAttachments()
    {
        List<CompoundStorage> storages = MsgStorageWalker.EnumerateIndexed(
            _storage, MsgStreamNames.AttachmentStoragePrefix, _header.AttachmentCount, _options.ValidationLevel);

        var attachments = new OutlookAttachment[storages.Count];
        for (int i = 0; i < storages.Count; i++)
        {
            MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
                storages[i], MsgPropertyStreamKind.RecipientOrAttachment, _options.ValidationLevel, _stringEncoding, out _);
            attachments[i] = new OutlookAttachment(this, storages[i], properties);
        }

        return attachments;
    }

    /// <summary>
    /// Opens an embedded-message storage as a nested, non-owning message session.
    /// </summary>
    /// <param name="messageStorage">The embedded-message storage inside an attachment.</param>
    /// <param name="attachmentProperties">The owning attachment's properties, for encoding inheritance.</param>
    /// <returns>The nested message session, sharing this session's container and named-property mapping.</returns>
    /// <exception cref="OutlookMsgFormatException">
    /// The nested message would sit deeper than <see cref="OutlookMessageReaderOptions.MaxEmbeddedMessageDepth" />.
    /// </exception>
    internal OutlookMessage OpenNestedMessage(CompoundStorage messageStorage, MapiPropertyCollection attachmentProperties)
    {
        if (_depth >= _options.MaxEmbeddedMessageDepth)
        {
            throw new OutlookMsgFormatException(string.Format(
                System.Globalization.CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgEmbeddedMessageDepth, _options.MaxEmbeddedMessageDepth));
        }

        System.Text.Encoding inherited = MapiEncodingResolver.Resolve(attachmentProperties, _stringEncoding);
        MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
            messageStorage, MsgPropertyStreamKind.EmbeddedMessage, _options.ValidationLevel, inherited, out MsgPropertyStreamHeader header);

        System.Text.Encoding encoding = MapiEncodingResolver.Resolve(properties, inherited);
        return new OutlookMessage(_compound, messageStorage, _options, properties, header, ownsContainer: false, encoding, _root ?? this, _depth + 1);
    }
}
