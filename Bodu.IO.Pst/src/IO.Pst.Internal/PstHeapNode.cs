// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeapNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Parses a node's heap-on-node (<c>HN</c>) — the LTP allocator that subdivides the node's data blocks into small,
/// <c>HID</c>-addressed items — and answers item reads over it.
/// </summary>
/// <remarks>
/// An <c>HID</c> addresses an individual data block by index, so the heap is parsed over the node's ordered block
/// segments rather than its flattened payload. Every geometric fact — header extents, the page map, allocation
/// monotonicity, and item bounds — is validated at parse time at every validation level, so item reads slice
/// already-verified ranges.
/// </remarks>
internal sealed class PstHeapNode
{
    /// <summary>The heap signature byte (<c>bSig</c>) every heap-on-node declares.</summary>
    private const byte HeapSignature = 0xEC;

    /// <summary>The client signature (<c>bClientSig</c>) of a property context.</summary>
    internal const byte PropertyContextSignature = 0xBC;

    /// <summary>The client signature (<c>bClientSig</c>) of a table context.</summary>
    internal const byte TableContextSignature = 0x7C;

    /// <summary>The client signature (<c>bClientSig</c>) of a bare BTree-on-heap.</summary>
    internal const byte BTreeOnHeapSignature = 0xB5;

    /// <summary>The node's ordered data-block segments.</summary>
    private readonly List<byte[]> _segments;

    /// <summary>Each block's allocation table: entry <c>n</c> starts at <c>[n-1]</c> and ends at <c>[n]</c>.</summary>
    private readonly ushort[][] _allocations;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstHeapNode" /> class over parsed geometry.
    /// </summary>
    /// <param name="nodeId">The owning node identifier, for diagnostics.</param>
    /// <param name="clientSignature">The heap's client signature.</param>
    /// <param name="userRootHid">The client-defined root <c>HID</c>.</param>
    /// <param name="segments">The node's ordered data-block segments.</param>
    /// <param name="allocations">The validated per-block allocation tables.</param>
    private PstHeapNode(uint nodeId, byte clientSignature, uint userRootHid, List<byte[]> segments, ushort[][] allocations)
    {
        NodeId = nodeId;
        ClientSignature = clientSignature;
        UserRootHid = userRootHid;
        _segments = segments;
        _allocations = allocations;
    }

    /// <summary>
    /// Gets the owning node identifier, used in diagnostics.
    /// </summary>
    /// <value>The node identifier.</value>
    internal uint NodeId { get; }

    /// <summary>
    /// Gets the heap's client signature, which declares the structure built on the heap.
    /// </summary>
    /// <value>
    /// The <c>bClientSig</c> byte: <see cref="PropertyContextSignature" />, <see cref="TableContextSignature" />, or
    /// <see cref="BTreeOnHeapSignature" />.
    /// </value>
    internal byte ClientSignature { get; }

    /// <summary>
    /// Gets the client-defined root of the structure built on the heap.
    /// </summary>
    /// <value>The <c>hidUserRoot</c> value; zero when the structure is empty.</value>
    internal uint UserRootHid { get; }

    /// <summary>
    /// Parses the heap-on-node of the supplied node entry.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="entry">The node whose data blocks carry the heap.</param>
    /// <returns>The parsed heap.</returns>
    /// <exception cref="PstFileFormatException">The node carries no data or its heap geometry is malformed.</exception>
    internal static PstHeapNode Parse(PstSource source, PstNbtEntry entry)
    {
        List<byte[]> segments = PstDataTree.ResolveSegments(source, entry.DataBlockId);
        if (segments.Count == 0)
            throw MalformedHeap(entry.NodeId);

        // HNHDR: ibHnpm(2) bSig(1) bClientSig(1) hidUserRoot(4) rgbFillLevel(4). Later blocks carry HNPAGEHDR or
        // HNBITMAPHDR, whose only field this reader needs — ibHnpm — sits at offset 0 in all three shapes.
        byte[] first = segments[0];
        if (first.Length < 12 || first[2] != HeapSignature)
            throw MalformedHeap(entry.NodeId);

        byte clientSignature = first[3];
        uint userRootHid = BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(4));

        var allocations = new ushort[segments.Count][];
        for (int i = 0; i < segments.Count; i++)
            allocations[i] = ParsePageMap(segments[i], entry.NodeId);

        return new PstHeapNode(entry.NodeId, clientSignature, userRootHid, segments, allocations);
    }

    /// <summary>
    /// Attempts to read the heap item an <c>HID</c> addresses.
    /// </summary>
    /// <param name="hid">The heap identifier.</param>
    /// <param name="item">When this method returns <see langword="true" />, the item bytes.</param>
    /// <returns><see langword="true" /> when the identifier resolves within the heap.</returns>
    internal bool TryGetItem(uint hid, out ReadOnlyMemory<byte> item)
    {
        item = default;

        if ((hid & 0x1F) != 0)
            return false;

        int itemIndex = (int)((hid >> 5) & 0x7FF);
        int blockIndex = (int)(hid >> 16);
        if (itemIndex < 1 || blockIndex >= _segments.Count)
            return false;

        ushort[] allocations = _allocations[blockIndex];
        if (itemIndex >= allocations.Length)
            return false;

        int start = allocations[itemIndex - 1];
        item = _segments[blockIndex].AsMemory(start, allocations[itemIndex] - start);
        return true;
    }

    /// <summary>
    /// Reads the heap item an <c>HID</c> addresses.
    /// </summary>
    /// <param name="hid">The heap identifier.</param>
    /// <returns>The item bytes.</returns>
    /// <exception cref="PstFileFormatException">The identifier does not resolve within the heap.</exception>
    internal ReadOnlyMemory<byte> GetItem(uint hid)
    {
        if (!TryGetItem(hid, out ReadOnlyMemory<byte> item))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstHeapId, hid, new PstNodeId(NodeId)));
        }

        return item;
    }

    /// <summary>
    /// Parses and validates one block's <c>HNPAGEMAP</c>: the allocation-offset table addressed by the block's
    /// <c>ibHnpm</c> field.
    /// </summary>
    /// <param name="block">The block payload.</param>
    /// <param name="nodeId">The owning node identifier, for diagnostics.</param>
    /// <returns>The allocation offsets, monotonically non-decreasing and within the block.</returns>
    /// <exception cref="PstFileFormatException">The page map is out of bounds or its offsets regress.</exception>
    private static ushort[] ParsePageMap(byte[] block, uint nodeId)
    {
        if (block.Length < 2)
            throw MalformedHeap(nodeId);

        int mapOffset = BinaryPrimitives.ReadUInt16LittleEndian(block);
        if (mapOffset + 4 > block.Length)
            throw MalformedHeap(nodeId);

        int count = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(mapOffset));
        if (mapOffset + 4 + ((count + 1) * 2) > block.Length)
            throw MalformedHeap(nodeId);

        // Monotonicity and bounds are enforced here so every later item read slices a verified range.
        var allocations = new ushort[count + 1];
        ushort previous = 0;
        for (int i = 0; i <= count; i++)
        {
            ushort offset = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(mapOffset + 4 + (i * 2)));
            if (offset < previous || offset > block.Length)
                throw MalformedHeap(nodeId);

            allocations[i] = offset;
            previous = offset;
        }

        return allocations;
    }

    /// <summary>
    /// Creates the malformed-heap exception for a node identifier.
    /// </summary>
    /// <param name="nodeId">The owning node identifier.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException MalformedHeap(uint nodeId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstHeapNode, new PstNodeId(nodeId)));
}
