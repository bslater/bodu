// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents one attachment of an <see cref="OutlookMailMessage" />: a typed view over the attachment object's
/// decoded properties, with access to the by-value payload or the nested attached message.
/// </summary>
/// <remarks>
/// The conveniences return <see langword="null" /> when the underlying property is absent; every attachment property
/// remains reachable through <see cref="Properties" />. Content access is method-specific:
/// <see cref="OpenContentStream" /> serves a by-value payload and <see cref="OpenMessage" /> serves an embedded
/// message — each throws <see cref="NotSupportedException" /> for the other method kinds.
/// </remarks>
public sealed class OutlookMailAttachment
{
    /// <summary>The owning session.</summary>
    private readonly OutlookMailStore _store;

    /// <summary>The message that owns this attachment.</summary>
    private readonly OutlookMailMessage _owner;

    /// <summary>The attachment object subnode.</summary>
    private readonly PstNode _node;

    /// <summary>The lazily decoded attachment properties.</summary>
    private MapiPropertyCollection? _properties;

    /// <summary>The encoding the attachment's code-page strings decoded with; set when <see cref="Properties" /> decodes.</summary>
    private Encoding? _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailAttachment" /> class.
    /// </summary>
    /// <param name="store">The owning session.</param>
    /// <param name="owner">The owning message view.</param>
    /// <param name="node">The attachment object subnode.</param>
    internal OutlookMailAttachment(OutlookMailStore store, OutlookMailMessage owner, PstNode node)
    {
        _store = store;
        _owner = owner;
        _node = node;
    }

    /// <summary>
    /// Gets every decoded property of the attachment.
    /// </summary>
    /// <value>The tag-addressed property collection, decoded once on first access.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    public MapiPropertyCollection Properties
    {
        get
        {
            _store.ThrowIfDisposed();

            if (_properties is null)
            {
                _properties = PstMapiPropertyReader.Read(
                    _node.ReadPropertyContext(), _owner.MessageEncoding, _store.Strict, out Encoding encoding);
                _encoding = encoding;
            }

            return _properties;
        }
    }

    /// <summary>
    /// Gets how the attachment's content is stored.
    /// </summary>
    /// <value>
    /// The <c>PidTagAttachMethod</c> value when present and defined. When the property is absent — or, under the
    /// tolerant validation levels, carries an undefined value — an attachment that carries a by-value payload reports
    /// <see cref="OutlookAttachmentMethod.ByValue" /> (real-world writers frequently omit the method); otherwise
    /// <see cref="OutlookAttachmentMethod.None" />.
    /// </value>
    /// <exception cref="OutlookPstFormatException">
    /// The declared method is not a defined value and the session validates strictly.
    /// </exception>
    public OutlookAttachmentMethod Method
    {
        get
        {
            if (Properties.GetInt32(MapiPropertyIds.AttachMethod) is int value)
            {
                if (value is >= (int)OutlookAttachmentMethod.None and <= (int)OutlookAttachmentMethod.Ole)
                    return (OutlookAttachmentMethod)value;

                if (_store.Strict)
                {
                    throw new OutlookPstFormatException(string.Format(
                        CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstAttachmentMethod, value));
                }
            }

            return Properties.GetBinary(MapiPropertyIds.AttachData) is not null
                ? OutlookAttachmentMethod.ByValue
                : OutlookAttachmentMethod.None;
        }
    }

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
    /// <see cref="OutlookAttachmentMethod.Ole" /> — the payload is an object, not a byte stream.
    /// </exception>
    /// <exception cref="OutlookPstFormatException">The by-value content payload is missing.</exception>
    /// <remarks>
    /// The payload is the attachment object's <c>PidTagAttachData</c> value, buffered when the properties decode; the
    /// returned stream reads from that buffer without copying it.
    /// </remarks>
    public Stream OpenContentStream()
    {
        if (Method is OutlookAttachmentMethod.EmbeddedMessage or OutlookAttachmentMethod.Ole)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookPstResourceStrings.Op_NotSupported_PstAttachmentContent, Method));
        }

        if (Properties.GetBinary(MapiPropertyIds.AttachData) is not ReadOnlyMemory<byte> content)
            throw new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_PstAttachmentContent);

        // The decoded payload is an owned array the collection never mutates; wrap it read-only rather than copying
        // a payload that can run to tens of megabytes.
        if (!MemoryMarshal.TryGetArray(content, out ArraySegment<byte> segment))
            segment = new ArraySegment<byte>(content.ToArray());

        return new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
    }

    /// <summary>
    /// Opens the nested attached message.
    /// </summary>
    /// <returns>
    /// The nested message view. It is bound to the owning session and becomes unusable when the session is disposed.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The attachment's <see cref="Method" /> is not <see cref="OutlookAttachmentMethod.EmbeddedMessage" />.
    /// </exception>
    /// <exception cref="OutlookPstFormatException">
    /// The attachment carries no nested message subnode, or the nested message would sit deeper than
    /// <see cref="OutlookMailStoreReaderOptions.MaxEmbeddedMessageDepth" />.
    /// </exception>
    /// <remarks>
    /// The nested message is the message-typed subnode of the attachment object (MS-PST §2.4.6.3); its code-page
    /// strings inherit the attachment's encoding.
    /// </remarks>
    public OutlookMailMessage OpenMessage()
    {
        if (Method != OutlookAttachmentMethod.EmbeddedMessage)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture, OutlookPstResourceStrings.Op_NotSupported_PstEmbeddedMessage, Method));
        }

        if (_owner.EmbeddedDepth >= _store.MaxEmbeddedMessageDepth)
        {
            throw new OutlookPstFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstEmbeddedMessageDepth, _store.MaxEmbeddedMessageDepth));
        }

        if (_node.TryGetSubnodeOfType(PstNodeType.NormalMessage, out PstNode? messageNode))
            return new OutlookMailMessage(_store, messageNode, AttachmentEncoding, _owner.EmbeddedDepth + 1);

        throw new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_PstEmbeddedMessage);
    }

    /// <summary>
    /// Returns a textual form of the attachment for diagnostics.
    /// </summary>
    /// <returns>The method and file name.</returns>
    public override string ToString() =>
        $"{Method}: {FileName ?? "(unnamed)"}";

    /// <summary>
    /// Gets the encoding the attachment's code-page strings decoded with, forcing the properties to decode first.
    /// </summary>
    /// <value>The attachment-level encoding a nested message inherits.</value>
    private Encoding AttachmentEncoding
    {
        get
        {
            _ = Properties;

            return _encoding!;
        }
    }
}
