// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailFolder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents one folder of a mail store: its decoded properties and streaming access to its subfolders and messages.
/// </summary>
/// <remarks>
/// <para>
/// Folder views are bound to their owning <see cref="OutlookMailStore" /> session. Enumerations stream the folder's
/// hierarchy and contents tables one row block at a time — nothing is materialized ahead of iteration — and a folder
/// whose table node is absent enumerates empty, matching real-world stores that omit empty tables.
/// </para>
/// <para>
/// Under strict validation a table row that does not reference a valid node of the expected kind throws
/// <see cref="OutlookPstFormatException" />; under the tolerant levels the row is skipped.
/// </para>
/// </remarks>
public sealed class OutlookMailFolder
{
    /// <summary>The owning session.</summary>
    private readonly OutlookMailStore _store;

    /// <summary>The folder node.</summary>
    private readonly PstNode _node;

    /// <summary>The lazily decoded folder properties.</summary>
    private MapiPropertyCollection? _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailFolder" /> class.
    /// </summary>
    /// <param name="store">The owning session.</param>
    /// <param name="node">The folder node.</param>
    internal OutlookMailFolder(OutlookMailStore store, PstNode node)
    {
        _store = store;
        _node = node;
    }

    /// <summary>
    /// Gets every decoded property of the folder.
    /// </summary>
    /// <value>The tag-addressed property collection, decoded once on first access.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    public MapiPropertyCollection Properties
    {
        get
        {
            _store.ThrowIfDisposed();

            return _properties ??= PstMapiPropertyReader.Read(
                _node.ReadPropertyContext(), _store.StoreEncoding, _store.Strict, out _);
        }
    }

    /// <summary>
    /// Gets the folder display name.
    /// </summary>
    /// <value>The <c>PidTagDisplayName</c> value, or <see langword="null" /> when absent.</value>
    public string? DisplayName =>
        Properties.GetString(MapiPropertyIds.DisplayName);

    /// <summary>
    /// Gets the folder container class (for example, <c>IPF.Note</c>).
    /// </summary>
    /// <value>The <c>PidTagContainerClass</c> value, or <see langword="null" /> when absent.</value>
    public string? ContainerClass =>
        Properties.GetString(MapiPropertyIds.ContainerClass);

    /// <summary>
    /// Gets the number of messages the folder declares.
    /// </summary>
    /// <value>The <c>PidTagContentCount</c> value, or <see langword="null" /> when absent.</value>
    public int? MessageCount =>
        Properties.GetInt32(MapiPropertyIds.ContentCount);

    /// <summary>
    /// Gets the number of unread messages the folder declares.
    /// </summary>
    /// <value>The <c>PidTagContentUnreadCount</c> value, or <see langword="null" /> when absent.</value>
    public int? UnreadCount =>
        Properties.GetInt32(MapiPropertyIds.ContentUnreadCount);

    /// <summary>
    /// Gets a value indicating whether the folder declares subfolders.
    /// </summary>
    /// <value>The <c>PidTagSubfolders</c> value; <see langword="false" /> when absent.</value>
    public bool HasSubfolders =>
        Properties.GetBoolean(MapiPropertyIds.Subfolders) ?? false;

    /// <summary>
    /// Enumerates the folder's immediate subfolders, in hierarchy-table order.
    /// </summary>
    /// <returns>The subfolder views; empty when the folder has no hierarchy table.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, a hierarchy row does not reference a valid folder node.
    /// </exception>
    /// <remarks>
    /// Search folders are Outlook runtime state, not archive content, and are excluded from the enumeration at every
    /// validation level — the standard search root hangs off the hierarchy of real stores.
    /// </remarks>
    public IEnumerable<OutlookMailFolder> EnumerateSubfolders()
    {
        foreach (PstNode node in EnumerateTableNodes(
            PstStoreLayout.HierarchyTableOf(_node.Id), PstNodeType.NormalFolder, silentlySkipped: PstNodeType.SearchFolder))
        {
            yield return new OutlookMailFolder(_store, node);
        }
    }

    /// <summary>
    /// Enumerates the folder's messages, in contents-table order.
    /// </summary>
    /// <returns>The message views; empty when the folder has no contents table.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, a contents row does not reference a valid message node.
    /// </exception>
    public IEnumerable<OutlookMailMessage> EnumerateMessages()
    {
        foreach (PstNode node in EnumerateTableNodes(PstStoreLayout.ContentsTableOf(_node.Id), PstNodeType.NormalMessage))
            yield return new OutlookMailMessage(_store, node);
    }

    /// <summary>
    /// Enumerates the folder's associated (folder-associated-information) messages, in table order.
    /// </summary>
    /// <returns>The associated-message views; empty when the folder has no associated-contents table.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, an associated-contents row does not reference a valid message node.
    /// </exception>
    public IEnumerable<OutlookMailMessage> EnumerateAssociatedMessages()
    {
        foreach (PstNode node in EnumerateTableNodes(PstStoreLayout.AssociatedContentsTableOf(_node.Id), PstNodeType.AssociatedMessage))
            yield return new OutlookMailMessage(_store, node);
    }

    /// <summary>
    /// Returns a textual form of the folder for diagnostics.
    /// </summary>
    /// <returns>The display name, or the node identifier when unnamed.</returns>
    public override string ToString() =>
        DisplayName ?? _node.Id.ToString();

    /// <summary>
    /// Streams the object nodes a table's rows reference: each row identifier is the referenced node's identifier.
    /// </summary>
    /// <param name="tableId">The composed table node identifier.</param>
    /// <param name="expectedType">The node type a row must reference.</param>
    /// <param name="silentlySkipped">
    /// A node type that is legitimate structure but deliberately excluded (for example, search folders), skipped
    /// without error at every validation level.
    /// </param>
    /// <returns>The referenced nodes, in row order; empty when the table node is absent.</returns>
    private IEnumerable<PstNode> EnumerateTableNodes(PstNodeId tableId, PstNodeType expectedType, PstNodeType? silentlySkipped = null)
    {
        if (!_store.TryGetNode(tableId, out PstNode? tableNode))
            yield break;

        foreach (PstTableRow row in tableNode.ReadTableContext().EnumerateRows())
        {
            var rowNodeId = new PstNodeId(row.RowId);
            if (rowNodeId.Type == silentlySkipped)
                continue;

            if (!_store.TryGetNode(rowNodeId, out PstNode? node) || node.Id.Type != expectedType)
            {
                if (_store.Strict)
                {
                    throw new OutlookPstFormatException(string.Format(
                        CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstTableRowNode, rowNodeId, tableId));
                }

                continue;
            }

            yield return node;
        }
    }
}
