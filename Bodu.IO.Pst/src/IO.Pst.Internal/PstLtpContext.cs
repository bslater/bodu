// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstLtpContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Resolves the <c>HNID</c> value references a property or table context carries: a heap identifier reads from the
/// context's own heap, and a node identifier reads from the owning node's subnode tree.
/// </summary>
/// <remarks>
/// The subnode tree is loaded on first use and its entries cached by identifier, so a context with many
/// subnode-resident values walks the tree once.
/// </remarks>
internal sealed class PstLtpContext
{
    /// <summary>The open source.</summary>
    private readonly PstSource _source;

    /// <summary>The owning node's subnode-tree block identifier; zero when the node has none.</summary>
    private readonly ulong _subnodeBlockId;

    /// <summary>The subnode entries keyed by node identifier, loaded on first use.</summary>
    private Dictionary<uint, PstNbtEntry>? _subnodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstLtpContext" /> class for a node.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="entry">The owning node's directory entry.</param>
    internal PstLtpContext(PstSource source, PstNbtEntry entry)
    {
        _source = source;
        _subnodeBlockId = entry.SubnodeBlockId;
        NodeId = entry.NodeId;
    }

    /// <summary>
    /// Gets the owning node identifier, used in diagnostics.
    /// </summary>
    /// <value>The node identifier.</value>
    internal uint NodeId { get; }

    /// <summary>
    /// Gets the open source the context reads from.
    /// </summary>
    /// <value>The source.</value>
    internal PstSource Source => _source;

    /// <summary>
    /// Resolves an <c>HNID</c> to its value payload.
    /// </summary>
    /// <param name="heap">The context's heap, which serves heap-resident values.</param>
    /// <param name="hnid">The value reference; the null <c>HNID</c> yields an empty payload.</param>
    /// <returns>The payload bytes.</returns>
    /// <exception cref="PstFileFormatException">
    /// The reference does not resolve — a heap identifier outside the heap, or a node identifier absent from the
    /// owning node's subnode tree.
    /// </exception>
    internal byte[] ResolveHnidPayload(PstHeapNode heap, uint hnid)
    {
        if (PstHnid.IsNull(hnid))
            return [];

        if (PstHnid.IsHeapId(hnid))
            return heap.GetItem(hnid).ToArray();

        if (!TryGetSubnodeEntry(hnid, out PstNbtEntry entry))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstSubnodeTree, new PstNodeId(NodeId)), PstFileError.InvalidSubnodeTree);
        }

        return PstDataTree.Resolve(_source, entry.DataBlockId);
    }

    /// <summary>
    /// Attempts to resolve a subnode's ordered data-block segments, for structures that consume block boundaries
    /// directly.
    /// </summary>
    /// <param name="nid">The subnode identifier.</param>
    /// <param name="segments">When this method returns <see langword="true" />, the subnode's ordered data blocks.</param>
    /// <returns><see langword="true" /> when the subnode exists.</returns>
    internal bool TryGetSubnodeSegments(uint nid, out List<byte[]> segments)
    {
        if (!TryGetSubnodeEntry(nid, out PstNbtEntry entry))
        {
            segments = [];
            return false;
        }

        segments = PstDataTree.ResolveSegments(_source, entry.DataBlockId);
        return true;
    }

    /// <summary>
    /// Determines the length of the payload an HNID references without reading any leaf data.
    /// </summary>
    /// <param name="heap">The heap the HNID belongs to.</param>
    /// <param name="hnid">The heap identifier or subnode identifier.</param>
    /// <returns>The payload length in bytes; zero for a null HNID.</returns>
    /// <exception cref="PstFileFormatException">
    /// The HNID names a heap item or subnode that does not exist, or the subnode's data tree is malformed.
    /// </exception>
    /// <remarks>
    /// A subnode-resident value's length is the sum of its leaf block lengths, which the data tree's index blocks
    /// carry; the leaves themselves are not read and no materialization limit applies.
    /// </remarks>
    internal long GetHnidLength(PstHeapNode heap, uint hnid)
    {
        if (PstHnid.IsNull(hnid))
            return 0;

        if (PstHnid.IsHeapId(hnid))
            return heap.GetItem(hnid).Length;

        long total = 0;
        foreach (PstBbtEntry leaf in GetSubnodeLeaves(hnid))
            total += leaf.Length;

        return total;
    }

    /// <summary>
    /// Opens the payload an HNID references as a read-only, seekable stream.
    /// </summary>
    /// <param name="heap">The heap the HNID belongs to.</param>
    /// <param name="hnid">The heap identifier or subnode identifier.</param>
    /// <returns>
    /// A stream over the heap item for a heap-resident value, or a <see cref="PstDataStream" /> over the subnode's
    /// leaf blocks for a subnode-resident value; an empty stream for a null HNID.
    /// </returns>
    /// <exception cref="PstFileFormatException">
    /// The HNID names a heap item or subnode that does not exist, or the subnode's data tree is malformed.
    /// </exception>
    /// <remarks>
    /// A heap-resident value is served from the heap's already-decoded bytes without copying; a subnode-resident
    /// value is read block by block on demand, so <see cref="PstSource.MaxNodeDataLength" /> does not apply.
    /// </remarks>
    internal Stream OpenHnidStream(PstHeapNode heap, uint hnid)
    {
        if (PstHnid.IsNull(hnid))
            return new MemoryStream([], writable: false);

        if (PstHnid.IsHeapId(hnid))
            return OpenMemoryStream(heap.GetItem(hnid));

        return new PstDataStream(_source, GetSubnodeLeaves(hnid));
    }

    /// <summary>
    /// Opens a read-only stream over already-decoded bytes without copying them when the memory is array-backed.
    /// </summary>
    /// <param name="bytes">The bytes to expose.</param>
    /// <returns>The read-only stream.</returns>
    internal static Stream OpenMemoryStream(ReadOnlyMemory<byte> bytes) =>
        MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(bytes.ToArray(), writable: false);

    /// <summary>
    /// Resolves the leaf data blocks of a subnode's data tree, reading index blocks only.
    /// </summary>
    /// <param name="nid">The subnode identifier.</param>
    /// <returns>The ordered leaf entries.</returns>
    /// <exception cref="PstFileFormatException">The subnode does not exist or its data tree is malformed.</exception>
    private List<PstBbtEntry> GetSubnodeLeaves(uint nid)
    {
        if (!TryGetSubnodeEntry(nid, out PstNbtEntry entry))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstSubnodeTree, new PstNodeId(NodeId)), PstFileError.InvalidSubnodeTree);
        }

        return PstDataTree.ResolveLeafEntries(_source, entry.DataBlockId);
    }

    /// <summary>
    /// Attempts to find a subnode entry, loading the tree on first use.
    /// </summary>
    /// <param name="nid">The subnode identifier.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the subnode's entry.</param>
    /// <returns><see langword="true" /> when the subnode exists.</returns>
    private bool TryGetSubnodeEntry(uint nid, out PstNbtEntry entry)
    {
        if (_subnodes is null)
        {
            _subnodes = new Dictionary<uint, PstNbtEntry>();
            foreach (PstNbtEntry subnode in PstSubnodeTree.Read(_source, _subnodeBlockId))
                _subnodes[subnode.NodeId] = subnode;
        }

        return _subnodes.TryGetValue(nid, out entry);
    }
}
