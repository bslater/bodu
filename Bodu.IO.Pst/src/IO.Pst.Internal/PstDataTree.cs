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
    /// Resolves a data-block identifier to the node's complete payload.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The data-block identifier from the node entry; <c>0</c> yields an empty payload.</param>
    /// <returns>The assembled payload bytes.</returns>
    /// <exception cref="PstFileFormatException">A referenced block is missing or a tree block is malformed.</exception>
    internal static byte[] Resolve(PstSource source, ulong blockId)
    {
        List<byte[]> segments = ResolveSegments(source, blockId);
        if (segments.Count == 0)
            return Array.Empty<byte>();

        // A single segment is returned as a copy: the block array may be held by the session's decoded-block cache,
        // and this payload can escape to public callers (PstNode.ReadAllBytes) as a mutable byte[].
        if (segments.Count == 1)
            return (byte[])segments[0].Clone();

        using var payload = new MemoryStream();
        foreach (byte[] segment in segments)
            payload.Write(segment);

        return payload.ToArray();
    }

    /// <summary>
    /// Resolves a data-block identifier to the payload's leaf data blocks, in order.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The data-block identifier from the node entry; <c>0</c> yields an empty list.</param>
    /// <returns>The ordered leaf data blocks whose concatenation is the payload.</returns>
    /// <exception cref="PstFileFormatException">A referenced block is missing or a tree block is malformed.</exception>
    /// <remarks>
    /// The LTP heap-on-node addresses individual data blocks by index, so the segment boundaries are significant to
    /// its readers; <see cref="Resolve" /> flattens the same segments for callers that only need the payload bytes.
    /// </remarks>
    internal static List<byte[]> ResolveSegments(PstSource source, ulong blockId)
    {
        var segments = new List<byte[]>();
        if (blockId == 0)
            return segments;

        byte[] block = ReadBlock(source, blockId);
        if ((blockId & 0x2) == 0)
        {
            segments.Add(block);
            return segments;
        }

        // Internal data blocks are trees: btype 0x01, cLevel 1 (XBLOCK over data blocks) or 2 (XXBLOCK over XBLOCKs).
        (byte level, int count) = ParseTreeBlock(block, blockId);
        for (int i = 0; i < count; i++)
        {
            ulong childId = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)));
            if (level == 1)
                segments.Add(ReadBlock(source, childId));
            else
                AppendXBlockSegments(source, childId, segments);
        }

        return segments;
    }

    /// <summary>
    /// Appends one <c>XBLOCK</c> child of an <c>XXBLOCK</c> as its ordered leaf data blocks.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The <c>XBLOCK</c> identifier.</param>
    /// <param name="segments">The segment list to append to.</param>
    /// <exception cref="PstFileFormatException">The block is not an <c>XBLOCK</c>.</exception>
    private static void AppendXBlockSegments(PstSource source, ulong blockId, List<byte[]> segments)
    {
        byte[] block = ReadBlock(source, blockId);
        (byte level, int count) = ParseTreeBlock(block, blockId);
        if (level != 1)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId), PstFileError.InvalidDataTree);
        }

        for (int i = 0; i < count; i++)
            segments.Add(ReadBlock(source, BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)))));
    }

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
        for (int i = 0; i < count; i++)
        {
            ulong childId = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)));
            if (level == 1)
            {
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
