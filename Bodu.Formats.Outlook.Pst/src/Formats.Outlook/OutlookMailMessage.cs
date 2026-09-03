// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents one message of a mail store: its decoded properties with typed conveniences over the well-known
/// scalars.
/// </summary>
/// <remarks>
/// Message views are bound to their owning <see cref="OutlookMailStore" /> session and decode their property context
/// once, on first access. Every property remains reachable through <see cref="Properties" />; the conveniences return
/// <see langword="null" /> when the underlying property is absent.
/// </remarks>
public sealed partial class OutlookMailMessage
{
    /// <summary>The owning session.</summary>
    private readonly OutlookMailStore _store;

    /// <summary>The message node.</summary>
    private readonly PstNode _node;

    /// <summary>The encoding inherited from the owning object for a nested message, or <see langword="null" />.</summary>
    private readonly Encoding? _inheritedEncoding;

    /// <summary>The embedded-message nesting depth: zero for a folder-level message.</summary>
    private readonly int _depth;

    /// <summary>The lazily decoded message properties.</summary>
    private MapiPropertyCollection? _properties;

    /// <summary>The encoding the message's code-page strings decoded with; set when <see cref="Properties" /> decodes.</summary>
    private Encoding? _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailMessage" /> class.
    /// </summary>
    /// <param name="store">The owning session.</param>
    /// <param name="node">The message node.</param>
    /// <param name="inheritedEncoding">
    /// The encoding a nested message inherits from its owning attachment; <see langword="null" /> for a folder-level
    /// message, which inherits the store encoding.
    /// </param>
    /// <param name="depth">The embedded-message nesting depth; zero for a folder-level message.</param>
    internal OutlookMailMessage(OutlookMailStore store, PstNode node, Encoding? inheritedEncoding = null, int depth = 0)
    {
        _store = store;
        _node = node;
        _inheritedEncoding = inheritedEncoding;
        _depth = depth;
    }

    /// <summary>
    /// Gets the embedded-message nesting depth of this view: zero for a message enumerated from a folder, one more
    /// for each level opened through <see cref="OutlookMailAttachment.OpenMessage" />.
    /// </summary>
    /// <value>The nesting depth.</value>
    public int EmbeddedDepth =>
        _depth;

    /// <summary>
    /// Gets every decoded property of the message.
    /// </summary>
    /// <value>The tag-addressed property collection, decoded once on first access.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    public MapiPropertyCollection Properties
    {
        get
        {
            if (_properties is null)
            {
                _properties = PstMapiPropertyReader.Read(
                    _node.ReadPropertyContext(), _inheritedEncoding ?? _store.StoreEncoding, _store.Strict, out Encoding encoding);
                _encoding = encoding;
            }

            return _properties;
        }
    }

    /// <summary>
    /// Gets the message subject, with the MS-PST subject-prefix marker removed.
    /// </summary>
    /// <value>The normalized <c>PidTagSubject</c> value, or <see langword="null" /> when absent.</value>
    public string? Subject =>
        PstStoreLayout.NormalizeSubject(Properties.GetString(MapiPropertyIds.Subject));

    /// <summary>
    /// Gets the sender display name.
    /// </summary>
    /// <value>The <c>PidTagSenderName</c> value, or <see langword="null" /> when absent.</value>
    public string? SenderName =>
        Properties.GetString(MapiPropertyIds.SenderName);

    /// <summary>
    /// Gets the sender email address.
    /// </summary>
    /// <value>The <c>PidTagSenderEmailAddress</c> value, or <see langword="null" /> when absent.</value>
    public string? SenderEmailAddress =>
        Properties.GetString(MapiPropertyIds.SenderEmailAddress);

    /// <summary>
    /// Gets the message class (for example, <c>IPM.Note</c>).
    /// </summary>
    /// <value>The <c>PidTagMessageClass</c> value, or <see langword="null" /> when absent.</value>
    public string? MessageClass =>
        Properties.GetString(MapiPropertyIds.MessageClass);

    /// <summary>
    /// Gets the internet message identifier.
    /// </summary>
    /// <value>The <c>PidTagInternetMessageId</c> value, or <see langword="null" /> when absent.</value>
    public string? InternetMessageId =>
        Properties.GetString(MapiPropertyIds.InternetMessageId);

    /// <summary>
    /// Gets the transport message headers.
    /// </summary>
    /// <value>The <c>PidTagTransportMessageHeaders</c> value, or <see langword="null" /> when absent.</value>
    public string? TransportMessageHeaders =>
        Properties.GetString(MapiPropertyIds.TransportMessageHeaders);

    /// <summary>
    /// Gets the client submit time.
    /// </summary>
    /// <value>The <c>PidTagClientSubmitTime</c> value, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? SentTime =>
        Properties.GetDateTime(MapiPropertyIds.ClientSubmitTime);

    /// <summary>
    /// Gets the message delivery time.
    /// </summary>
    /// <value>The <c>PidTagMessageDeliveryTime</c> value, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? ReceivedTime =>
        Properties.GetDateTime(MapiPropertyIds.MessageDeliveryTime);

    /// <summary>
    /// Returns a textual form of the message for diagnostics.
    /// </summary>
    /// <returns>The subject, or the node identifier when absent.</returns>
    public override string ToString() =>
        Subject ?? _node.Id.ToString();

    /// <summary>
    /// Gets the message node, for the recipient, attachment, and body partials.
    /// </summary>
    /// <returns>The message node.</returns>
    internal PstNode Node =>
        _node;

    /// <summary>
    /// Gets the encoding the message's code-page strings decoded with, forcing the properties to decode first.
    /// </summary>
    /// <value>The message-level encoding child objects inherit.</value>
    internal Encoding MessageEncoding
    {
        get
        {
            _ = Properties;

            return _encoding!;
        }
    }

    /// <summary>
    /// Attempts to retrieve the first subnode of a given type from the message's subnode tree.
    /// </summary>
    /// <param name="type">The node type sought.</param>
    /// <param name="subnode">When this method returns <see langword="true" />, the subnode.</param>
    /// <returns><see langword="true" /> when the message carries a subnode of the type.</returns>
    /// <exception cref="PstFileException">The subnode tree is malformed.</exception>
    private bool TryGetSubnodeOfType(PstNodeType type, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstNode subnode)
    {
        foreach (PstNodeInfo info in _node.EnumerateSubnodes())
        {
            if (info.NodeId.Type == type)
                return _node.TryGetSubnode(info.NodeId, out subnode);
        }

        subnode = null;
        return false;
    }
}
