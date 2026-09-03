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

    /// <summary>The lazily resolved ordered leaf entries of the node's data tree.</summary>
    private List<PstBbtEntry>? _dataLeaves;

    /// <summary>The lazily materialized subnode directory, in stored order.</summary>
    private List<PstNbtEntry>? _subnodeEntries;

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
    /// Gets the logical length of the node's data payload, in bytes.
    /// </summary>
    /// <value>The payload length; <c>0</c> when the node carries no data.</value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The data tree is malformed or fails validation.</exception>
    /// <remarks>
    /// Resolving the length reads only the data tree's internal blocks — never the leaf payloads — so it is cheap
    /// even for very large nodes.
    /// </remarks>
    public long DataLength
    {
        get
        {
            long total = 0;
            foreach (PstBbtEntry leaf in ResolveDataLeaves())
                total += leaf.Length;

            return total;
        }
    }

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
    /// <returns>A seekable stream over the payload.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The data tree is malformed or fails validation.</exception>
    /// <remarks>
    /// The stream reads one leaf block at a time through the session's decoded-block cache, so an arbitrarily large
    /// payload is never materialized; use <see cref="ReadAllBytes" /> for the buffered convenience.
    /// </remarks>
    public Stream OpenDataStream() =>
        new PstDataStream(_file.GetSource(), ResolveDataLeaves());

    /// <summary>
    /// Enumerates the node's subnodes, in stored order.
    /// </summary>
    /// <returns>The subnode directory facts; empty when the node carries no subnode tree.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The subnode tree is malformed or fails validation.</exception>
    public IEnumerable<PstNodeInfo> EnumerateSubnodes()
    {
        foreach (PstNbtEntry entry in ReadSubnodeEntries())
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
        foreach (PstNbtEntry entry in ReadSubnodeEntries())
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
    /// Attempts to retrieve the first subnode of a given type, in stored order, in a single pass over the subnode
    /// directory.
    /// </summary>
    /// <param name="type">The node type sought.</param>
    /// <param name="subnode">When this method returns <see langword="true" />, the subnode.</param>
    /// <returns><see langword="true" /> when the node carries a subnode of the type.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">The subnode tree is malformed or fails validation.</exception>
    internal bool TryGetSubnodeOfType(PstNodeType type, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstNode subnode)
    {
        foreach (PstNbtEntry entry in ReadSubnodeEntries())
        {
            if (new PstNodeId(entry.NodeId).Type == type)
            {
                subnode = new PstNode(_file, entry with { ParentNodeId = _entry.NodeId });
                return true;
            }
        }

        subnode = null;
        return false;
    }

    /// <summary>
    /// Reads the node's LTP property context: the property bag of 16-bit property identifiers with wire-typed values.
    /// </summary>
    /// <returns>The property context.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">
    /// The node's heap does not carry a property context, or the context is malformed.
    /// </exception>
    /// <remarks>
    /// Each call re-reads the context from the source; retain the returned instance to read many properties. Value
    /// payloads resolve on access, so large subnode-resident values cost their read only when retrieved.
    /// </remarks>
    public PstPropertyContext ReadPropertyContext()
    {
        (PstHeapNode heap, List<PstPcEntry> entries) = PstPropertyContextReader.Read(_file.GetSource(), _entry);

        return new PstPropertyContext(heap, new PstLtpContext(_file.GetSource(), _entry), entries);
    }

    /// <summary>
    /// Reads the node's LTP table context: the table of typed columns over identifier-keyed rows.
    /// </summary>
    /// <returns>The table context.</returns>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">
    /// The node's heap does not carry a table context, or the context is malformed.
    /// </exception>
    /// <remarks>
    /// Each call re-reads the context from the source; retain the returned instance to read many rows. Row
    /// enumeration streams the row matrix one block at a time.
    /// </remarks>
    public PstTableContext ReadTableContext()
    {
        (PstHeapNode heap, PstTcInfo info, PstBthHeader rowIndex) = PstTableContextReader.Read(_file.GetSource(), _entry);

        return new PstTableContext(heap, new PstLtpContext(_file.GetSource(), _entry), info, rowIndex, _file.GetSource().ValidationLevel);
    }

    /// <summary>
    /// Returns a textual form of the node for diagnostics.
    /// </summary>
    /// <returns>The identifier and its type.</returns>
    public override string ToString() =>
        $"{Id} ({Id.Type})";

    /// <summary>
    /// Resolves the node's ordered data-tree leaf entries, caching them for the instance's lifetime.
    /// </summary>
    /// <returns>The leaf entries; empty when the node carries no data.</returns>
    private List<PstBbtEntry> ResolveDataLeaves() =>
        _dataLeaves ??= PstDataTree.ResolveLeafEntries(_file.GetSource(), _entry.DataBlockId);

    /// <summary>
    /// Reads the node's subnode directory, caching the parsed rows for the instance's lifetime so repeated table and
    /// attachment lookups do not re-walk the subnode tree.
    /// </summary>
    /// <returns>The subnode entries, in stored order; empty when the node carries no subnode tree.</returns>
    private List<PstNbtEntry> ReadSubnodeEntries() =>
        _subnodeEntries ??= [.. PstSubnodeTree.Read(_file.GetSource(), _entry.SubnodeBlockId)];
}
