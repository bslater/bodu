// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;
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
/// — each throws <see cref="NotSupportedException" /> for every other method kind.
/// </remarks>
public sealed class OutlookAttachment
{
    /// <summary>The message that owns this attachment.</summary>
    private readonly OutlookMessage _owner;

    /// <summary>The attachment storage.</summary>
    private readonly CompoundStorage _storage;

    /// <summary>The length of the by-value content stream, or <see langword="null" /> when the storage carries none.</summary>
    private readonly long? _contentLength;

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
        _contentLength = MsgContainer.TryGetStreamLength(storage, ContentStreamName, out long length) ? length : null;
    }

    /// <summary>
    /// Gets the tag of the by-value content property (<c>PidTagAttachDataBinary</c>).
    /// </summary>
    internal static MapiPropertyTag ContentTag { get; } = new(MapiPropertyIds.AttachData, MapiPropertyType.Binary);

    /// <summary>
    /// Gets the name of the by-value content stream (<c>PidTagAttachDataBinary</c>).
    /// </summary>
    private static string ContentStreamName { get; } =
        MsgStreamNames.GetSubstgStreamName(ContentTag.Value);

    /// <summary>
    /// Gets every decoded property of the attachment.
    /// </summary>
    /// <value>The tag-addressed property collection.</value>
    /// <remarks>
    /// A by-value payload (<c>PidTagAttachDataBinary</c>) larger than
    /// <see cref="OutlookMessageReaderOptions.MaxInlineAttachmentBytes" /> is not decoded: the property is present
    /// with a <see langword="null" /> value and the content is served by <see cref="OpenContentStream" /> directly
    /// from the container.
    /// </remarks>
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
            : _contentLength is not null ? OutlookAttachmentMethod.ByValue : OutlookAttachmentMethod.None;

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
    /// Gets the attachment MIME type.
    /// </summary>
    /// <value>The <c>PidTagAttachMimeTag</c> value, or <see langword="null" /> when absent.</value>
    public string? MimeTag =>
        Properties.GetString(MapiPropertyIds.AttachMimeTag);

    /// <summary>
    /// Gets the attachment size.
    /// </summary>
    /// <value>
    /// The <c>PidTagAttachSize</c> value the writer recorded; when absent, the length of the by-value content stream;
    /// <see langword="null" /> when neither is available.
    /// </value>
    /// <remarks>
    /// The stream length comes from the container directory, so a deferred payload is never read to measure it.
    /// </remarks>
    public long? Size =>
        Properties.GetInt32(MapiPropertyIds.AttachSize) ?? _contentLength;

    /// <summary>
    /// Opens the by-value content payload as a read-only stream.
    /// </summary>
    /// <returns>The content stream; dispose it when reading is complete.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="NotSupportedException">
    /// The attachment's <see cref="Method" /> is not <see cref="OutlookAttachmentMethod.ByValue" /> — a referenced,
    /// embedded, or OLE attachment carries no byte payload in the message.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">
    /// The by-value content stream is missing, or the container is malformed.
    /// </exception>
    /// <remarks>
    /// A payload decoded inline (at or below <see cref="OutlookMessageReaderOptions.MaxInlineAttachmentBytes" />) is
    /// served from the decoded bytes without copying. A larger payload is opened from the container: under
    /// <see cref="CompoundReadStrategy.Streaming" /> it is read sector by sector on demand, and a container fault
    /// during a read surfaces as <see cref="OutlookMsgFormatException" />.
    /// </remarks>
    public Stream OpenContentStream()
    {
        _owner.ThrowIfDisposed();

        if (Method != OutlookAttachmentMethod.ByValue)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Op_NotSupported_MsgAttachmentContent, Method));
        }

        // An inline payload is served from the decoded bytes; a deferred one is opened from the container and read on
        // demand, with container faults translated as they surface.
        if (Properties.GetBinary(MapiPropertyIds.AttachData) is ReadOnlyMemory<byte> inline)
        {
            return MemoryMarshal.TryGetArray(inline, out ArraySegment<byte> segment)
                ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
                : new MemoryStream(inline.ToArray(), writable: false);
        }

        if (!MsgContainer.TryOpenStream(_storage, ContentStreamName, out CompoundStream? content))
        {
            throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.IO_KeyNotFound_MsgAttachmentContent, ContentStreamName));
        }

        return new MsgContentStream(content);
    }

    /// <summary>
    /// Opens the nested attached message.
    /// </summary>
    /// <returns>
    /// The nested message session. It shares the root session's container and named-property mapping; disposing it is
    /// a no-op, and it becomes unusable when the root session is disposed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="NotSupportedException">
    /// The attachment's <see cref="Method" /> is not <see cref="OutlookAttachmentMethod.EmbeddedMessage" />.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">
    /// The embedded-message storage is missing or malformed, or the nested message would sit deeper than
    /// <see cref="OutlookMessageReaderOptions.MaxEmbeddedMessageDepth" />.
    /// </exception>
    public OutlookMessage OpenMessage()
    {
        _owner.ThrowIfDisposed();

        if (Method != OutlookAttachmentMethod.EmbeddedMessage)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Op_NotSupported_MsgEmbeddedMessage, Method));
        }

        if (!MsgContainer.TryOpenStorage(_storage, MsgStreamNames.EmbeddedMessageStorageName, out CompoundStorage? messageStorage))
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
