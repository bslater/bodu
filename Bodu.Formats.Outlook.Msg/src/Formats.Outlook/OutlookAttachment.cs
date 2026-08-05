// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents one attachment of an <see cref="OutlookMessage" />: a typed view over the attachment storage's decoded
/// properties, with access to the by-value payload or the nested attached message.
/// </summary>
/// <remarks>
/// The conveniences return <see langword="null" /> when the underlying property is absent; every attachment property
/// remains reachable through <see cref="Properties" />. Content access is method-specific:
/// <see cref="OpenContentStream" /> serves a by-value payload and <see cref="OpenMessage" /> serves an embedded message
/// — each throws <see cref="NotSupportedException" /> for the other method kinds.
/// </remarks>
public sealed class OutlookAttachment
{
    /// <summary>The message that owns this attachment.</summary>
    private readonly OutlookMessage _owner;

    /// <summary>The attachment storage.</summary>
    private readonly CompoundStorage _storage;

    /// <summary>Whether the storage carries a by-value content stream.</summary>
    private readonly bool _hasContentStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookAttachment" /> class.
    /// </summary>
    /// <param name="owner">The owning message session.</param>
    /// <param name="storage">The attachment storage.</param>
    /// <param name="properties">The attachment storage's decoded properties.</param>
    internal OutlookAttachment(OutlookMessage owner, CompoundStorage storage, MapiPropertyCollection properties)
    {
        _owner = owner;
        _storage = storage;
        Properties = properties;
        _hasContentStream = storage.TryOpenStream(ContentStreamName, out CompoundStream? content);
        content?.Dispose();
    }

    /// <summary>
    /// Gets the name of the by-value content stream (<c>PidTagAttachDataBinary</c>).
    /// </summary>
    private static string ContentStreamName =>
        MsgStreamNames.GetSubstgStreamName(((uint)MapiPropertyIds.AttachData << 16) | (ushort)MapiPropertyType.Binary);

    /// <summary>
    /// Gets every decoded property of the attachment.
    /// </summary>
    /// <value>The tag-addressed property collection.</value>
    public MapiPropertyCollection Properties { get; }

    /// <summary>
    /// Gets how the attachment's content is stored.
    /// </summary>
    /// <value>
    /// The <c>PidTagAttachMethod</c> value when present and defined. When the property is absent, an attachment that
    /// carries a by-value content stream reports <see cref="OutlookAttachmentMethod.ByValue" /> (real-world writers
    /// frequently omit the method); otherwise <see cref="OutlookAttachmentMethod.None" />.
    /// </value>
    public OutlookAttachmentMethod Method =>
        Properties.GetInt32(MapiPropertyIds.AttachMethod) is int value
            && value is >= (int)OutlookAttachmentMethod.None and <= (int)OutlookAttachmentMethod.Ole
            ? (OutlookAttachmentMethod)value
            : _hasContentStream ? OutlookAttachmentMethod.ByValue : OutlookAttachmentMethod.None;

    /// <summary>
    /// Gets the attachment file name, preferring the long form.
    /// </summary>
    /// <value>
    /// The <c>PidTagAttachLongFilename</c> value, falling back to <c>PidTagAttachFilename</c>; <see langword="null" />
    /// when neither is present.
    /// </value>
    public string? FileName =>
        Properties.GetString(MapiPropertyIds.AttachLongFilename) ?? Properties.GetString(MapiPropertyIds.AttachFilename);

    /// <summary>
    /// Gets the content identifier used to reference the attachment from an HTML body.
    /// </summary>
    /// <value>The <c>PidTagAttachContentId</c> value, or <see langword="null" /> when absent.</value>
    public string? ContentId =>
        Properties.GetString(MapiPropertyIds.AttachContentId);

    /// <summary>
    /// Gets the attachment size the writer recorded.
    /// </summary>
    /// <value>The <c>PidTagAttachSize</c> value, or <see langword="null" /> when absent.</value>
    public long? Size =>
        Properties.GetInt32(MapiPropertyIds.AttachSize);

    /// <summary>
    /// Opens the by-value content payload as a read-only stream.
    /// </summary>
    /// <returns>The content stream; dispose it when reading is complete.</returns>
    /// <exception cref="NotSupportedException">
    /// The attachment's <see cref="Method" /> is <see cref="OutlookAttachmentMethod.EmbeddedMessage" /> or
    /// <see cref="OutlookAttachmentMethod.Ole" /> — the payload is a storage, not a stream.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">The by-value content stream is missing.</exception>
    public Stream OpenContentStream()
    {
        if (Method is OutlookAttachmentMethod.EmbeddedMessage or OutlookAttachmentMethod.Ole)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Op_NotSupported_MsgAttachmentContent, Method));
        }

        if (!_storage.TryOpenStream(ContentStreamName, out CompoundStream? content))
        {
            throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.IO_KeyNotFound_MsgAttachmentContent, ContentStreamName));
        }

        return content;
    }

    /// <summary>
    /// Opens the nested attached message.
    /// </summary>
    /// <returns>
    /// The nested message session. It shares the root session's container and named-property mapping; disposing it is
    /// a no-op, and it becomes unusable when the root session is disposed.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The attachment's <see cref="Method" /> is not <see cref="OutlookAttachmentMethod.EmbeddedMessage" />.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">The embedded-message storage is missing or malformed.</exception>
    public OutlookMessage OpenMessage()
    {
        if (Method != OutlookAttachmentMethod.EmbeddedMessage)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Op_NotSupported_MsgEmbeddedMessage, Method));
        }

        if (!_storage.TryOpenStorage(MsgStreamNames.EmbeddedMessageStorageName, out CompoundStorage? messageStorage))
        {
            throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.IO_KeyNotFound_MsgAttachmentContent, MsgStreamNames.EmbeddedMessageStorageName));
        }

        return _owner.OpenNestedMessage(messageStorage, Properties);
    }

    /// <summary>
    /// Returns a textual form of the attachment for diagnostics.
    /// </summary>
    /// <returns>The method and file name.</returns>
    public override string ToString() =>
        $"{Method}: {FileName ?? "(unnamed)"}";
}
