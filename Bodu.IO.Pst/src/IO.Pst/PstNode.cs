// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents one node of the node database: raw access to its data payload and its subnode tree.
/// </summary>
/// <remarks>
/// A node's payload is assembled from its data tree on demand and materialized in memory; the multi-block trees the
/// format uses for large payloads are flattened transparently. Subnodes form the node's private namespace — a message
/// node, for example, keeps its recipient and attachment tables there.
/// </remarks>
public sealed class PstNode
{
    /// <summary>The owning session.</summary>
    private readonly PstFile _file;

    /// <summary>The node's directory entry.</summary>
    private readonly PstNbtEntry _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNode" /> class.
    /// </summary>
    /// <param name="file">The owning session.</param>
    /// <param name="entry">The node's directory entry.</param>
    internal PstNode(PstFile file, PstNbtEntry entry)
    {
        _file = file;
        _entry = entry;
    }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    /// <value>The identifier.</value>
    public PstNodeId Id =>
        new(_entry.NodeId);

    /// <summary>
    /// Gets the parent node identifier the directory records.
    /// </summary>
    /// <value>The parent identifier, or the zero identifier when none is recorded.</value>
    public PstNodeId ParentId =>
        new(_entry.ParentNodeId);

    /// <summary>
    /// Gets a value indicating whether the node carries a subnode tree.
    /// </summary>
    /// <value><see langword="true" /> when subnodes are present.</value>
    public bool HasSubnodes =>
        _entry.SubnodeBlockId != 0;

    /// <summary>
    /// Reads the node's complete data payload.
    /// </summary>
    /// <returns>The payload bytes; empty when the node carries no data.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The data tree is malformed or fails validation.</exception>
    public byte[] ReadAllBytes() =>
        PstDataTree.Resolve(_file.GetSource(), _entry.DataBlockId);

    /// <summary>
    /// Opens the node's data payload as a read-only stream.
    /// </summary>
    /// <returns>A seekable stream over the assembled payload.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The data tree is malformed or fails validation.</exception>
    public Stream OpenDataStream() =>
        new MemoryStream(ReadAllBytes(), writable: false);

    /// <summary>
    /// Enumerates the node's subnodes, in stored order.
    /// </summary>
    /// <returns>The subnode directory facts; empty when the node carries no subnode tree.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The subnode tree is malformed or fails validation.</exception>
    public IEnumerable<PstNodeInfo> EnumerateSubnodes()
    {
        foreach (PstNbtEntry entry in PstSubnodeTree.Read(_file.GetSource(), _entry.SubnodeBlockId))
            yield return _file.ToInfo(entry with { ParentNodeId = _entry.NodeId });
    }

    /// <summary>
    /// Attempts to retrieve a subnode by identifier.
    /// </summary>
    /// <param name="id">The subnode identifier.</param>
    /// <param name="subnode">When this method returns <see langword="true" />, the subnode.</param>
    /// <returns><see langword="true" /> when the subnode exists.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The subnode tree is malformed or fails validation.</exception>
    public bool TryGetSubnode(PstNodeId id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstNode subnode)
    {
        foreach (PstNbtEntry entry in PstSubnodeTree.Read(_file.GetSource(), _entry.SubnodeBlockId))
        {
            if (entry.NodeId == id.Value)
            {
                subnode = new PstNode(_file, entry with { ParentNodeId = _entry.NodeId });
                return true;
            }
        }

        subnode = null;
        return false;
    }

    /// <summary>
    /// Returns a textual form of the node for diagnostics.
    /// </summary>
    /// <returns>The identifier and its type.</returns>
    public override string ToString() =>
        $"{Id} ({Id.Type})";
}
