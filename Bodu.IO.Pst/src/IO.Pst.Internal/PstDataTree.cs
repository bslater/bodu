// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Resolves a node's data-block identifier to its complete payload, flattening the multi-block data trees the format
/// uses for payloads beyond one block: an <c>XBLOCK</c> (a list of data-block identifiers) or an <c>XXBLOCK</c> (a list
/// of <c>XBLOCK</c> identifiers).
/// </summary>
internal static class PstDataTree
{
    /// <summary>The tree-block type byte shared by <c>XBLOCK</c> and <c>XXBLOCK</c>.</summary>
    private const byte DataTreeBlockType = 0x01;

    /// <summary>
    /// Resolves a data-block identifier to the node's complete payload, in memory.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The data-block identifier from the node entry; <c>0</c> yields an empty payload.</param>
    /// <returns>The payload bytes, owned by the caller.</returns>
    /// <exception cref="PstFileFormatException">
    /// A referenced block is missing, a tree block is malformed, or the payload exceeds the session's
    /// <see cref="PstSource.MaxNodeDataLength" /> or <see cref="PstSource.MaxDataTreeLeaves" /> limit
    /// (<see cref="PstFileError.LimitExceeded" />).
    /// </exception>
    /// <remarks>
    /// The leaf entries are resolved first — reading only tree blocks — so the declared total is checked against the
    /// materialization limit before any leaf payload is loaded, and the result is assembled into one exactly sized
    /// array rather than copied through a growable buffer.
    /// </remarks>
    internal static byte[] Resolve(PstSource source, ulong blockId)
    {
        List<PstBbtEntry> leaves = ResolveLeafEntries(source, blockId);
        long total = EnsureMaterializable(source, blockId, leaves);
        if (leaves.Count == 0)
            return [];

        // A single segment is returned as a copy: the block array may be held by the session's decoded-block cache,
        // and this payload can escape to public callers (PstNode.ReadAllBytes) as a mutable byte[].
        if (leaves.Count == 1)
            return (byte[])source.ReadBlock(leaves[0]).Clone();

        var payload = new byte[total];
        int offset = 0;
        foreach (PstBbtEntry leaf in leaves)
        {
            byte[] block = source.ReadBlock(leaf);
            block.CopyTo(payload, offset);
            offset += block.Length;
        }

        return payload;
    }

    /// <summary>
    /// Resolves a data-block identifier to the payload's leaf data blocks, in order.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The data-block identifier from the node entry; <c>0</c> yields an empty list.</param>
    /// <returns>The ordered leaf data blocks whose concatenation is the payload.</returns>
    /// <exception cref="PstFileFormatException">
    /// A referenced block is missing, a tree block is malformed, or the payload exceeds the session's limits
    /// (<see cref="PstFileError.LimitExceeded" />).
    /// </exception>
    /// <remarks>
    /// The LTP heap-on-node addresses individual data blocks by index, so the segment boundaries are significant to
    /// its readers; <see cref="Resolve" /> flattens the same segments for callers that only need the payload bytes.
    /// Like <see cref="Resolve" />, the declared total is checked against the materialization limit before any leaf
    /// payload is loaded.
    /// </remarks>
    internal static List<byte[]> ResolveSegments(PstSource source, ulong blockId)
    {
        List<PstBbtEntry> leaves = ResolveLeafEntries(source, blockId);
        _ = EnsureMaterializable(source, blockId, leaves);

        var segments = new List<byte[]>(leaves.Count);
        foreach (PstBbtEntry leaf in leaves)
            segments.Add(source.ReadBlock(leaf));

        return segments;
    }

    /// <summary>
    /// Sums the leaf lengths of a resolved tree and refuses a total above the session's materialization limit.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The tree's root block identifier, for diagnostics.</param>
    /// <param name="leaves">The resolved leaf entries.</param>
    /// <returns>The declared payload length.</returns>
    /// <exception cref="PstFileFormatException">The total exceeds <see cref="PstSource.MaxNodeDataLength" />.</exception>
    private static long EnsureMaterializable(PstSource source, ulong blockId, List<PstBbtEntry> leaves)
    {
        long total = 0;
        foreach (PstBbtEntry leaf in leaves)
            total += leaf.Length;

        if (total > source.MaxNodeDataLength)
            throw LimitExceeded(blockId);

        return total;
    }

    /// <summary>
    /// Creates the resource-limit exception for a tree.
    /// </summary>
    /// <param name="blockId">The tree's root block identifier.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException LimitExceeded(ulong blockId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstLimitExceeded, blockId), PstFileError.LimitExceeded);

    /// <summary>
    /// Parses and validates a tree block's header; the child identifiers follow at offset 8.
    /// </summary>
    /// <param name="block">The block payload.</param>
    /// <param name="blockId">The block identifier, for diagnostics.</param>
    /// <returns>The tree level and the child count.</returns>
    /// <exception cref="PstFileFormatException">The block is not a well-formed tree block.</exception>
    private static (byte Level, int Count) ParseTreeBlock(byte[] block, ulong blockId)
    {
        if (block.Length < 8 || block[0] != DataTreeBlockType || block[1] is not(1 or 2))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId), PstFileError.InvalidDataTree);
        }

        int count = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(2));
        if (8 + (count * 8) > block.Length)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId), PstFileError.InvalidDataTree);
        }

        return (block[1], count);
    }

    /// <summary>
    /// Resolves a data-block identifier to the payload's ordered leaf block-tree entries without reading any leaf
    /// payload.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The data-block identifier from the node entry; <c>0</c> yields an empty list.</param>
    /// <returns>The ordered leaf entries whose payloads concatenate to the node's data.</returns>
    /// <exception cref="PstFileFormatException">A referenced block is missing or a tree block is malformed.</exception>
    /// <remarks>
    /// Only the internal tree blocks (<c>XBLOCK</c> / <c>XXBLOCK</c>) are read; leaf data blocks are looked up in the
    /// block B-tree but never loaded, so a caller can stream an arbitrarily large logical payload block by block.
    /// </remarks>
    internal static List<PstBbtEntry> ResolveLeafEntries(PstSource source, ulong blockId)
    {
        var leaves = new List<PstBbtEntry>();
        if (blockId == 0)
            return leaves;

        if ((blockId & 0x2) == 0)
        {
            leaves.Add(FindEntry(source, blockId));
            return leaves;
        }

        byte[] block = ReadBlock(source, blockId);
        (byte level, int count) = ParseTreeBlock(block, blockId);
        int limit = source.MaxDataTreeLeaves;
        for (int i = 0; i < count; i++)
        {
            ulong childId = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)));
            if (level == 1)
            {
                // The fan-out is checked as it accumulates, before the leaf is even looked up: a crafted tree can
                // name the same block a million times, and the leaf list itself is the allocation being bounded.
                if (leaves.Count >= limit)
                    throw LimitExceeded(blockId);

                leaves.Add(FindEntry(source, childId));
            }
            else
            {
                byte[] child = ReadBlock(source, childId);
                (byte childLevel, int childCount) = ParseTreeBlock(child, childId);
                if (childLevel != 1)
                {
                    throw new PstFileFormatException(string.Format(
                        CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, childId), PstFileError.InvalidDataTree);
                }

                if (leaves.Count + childCount > limit)
                    throw LimitExceeded(blockId);

                for (int j = 0; j < childCount; j++)
                    leaves.Add(FindEntry(source, BinaryPrimitives.ReadUInt64LittleEndian(child.AsSpan(8 + (j * 8)))));
            }
        }

        return leaves;
    }

    /// <summary>
    /// Looks a block up in the block B-tree.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The block identifier.</param>
    /// <returns>The block's B-tree entry.</returns>
    /// <exception cref="PstFileFormatException">The identifier is not in the block B-tree.</exception>
    private static PstBbtEntry FindEntry(PstSource source, ulong blockId)
    {
        if (!PstBTree.TryFindBlock(source, blockId, out PstBbtEntry entry))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId), PstFileError.InvalidDataTree);
        }

        return entry;
    }

    /// <summary>
    /// Looks a block up in the block B-tree and reads it.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The block identifier.</param>
    /// <returns>The block payload.</returns>
    /// <exception cref="PstFileFormatException">The identifier is not in the block B-tree.</exception>
    internal static byte[] ReadBlock(PstSource source, ulong blockId)
    {
        if (!PstBTree.TryFindBlock(source, blockId, out PstBbtEntry entry))
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId), PstFileError.InvalidDataTree);
        }

        return source.ReadBlock(entry);
    }
}
