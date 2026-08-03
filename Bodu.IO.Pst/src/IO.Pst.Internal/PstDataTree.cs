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
        if (blockId == 0)
            return Array.Empty<byte>();

        byte[] block = ReadBlock(source, blockId);
        if ((blockId & 0x2) == 0)
            return block;

        // Internal data blocks are trees: btype 0x01, cLevel 1 (XBLOCK over data blocks) or 2 (XXBLOCK over XBLOCKs).
        (byte level, int count) = ParseTreeBlock(block, blockId);
        using var payload = new MemoryStream();
        for (int i = 0; i < count; i++)
        {
            ulong childId = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)));
            byte[] child = level == 1
                ? ReadBlock(source, childId)
                : ResolveXBlock(source, childId);
            payload.Write(child);
        }

        return payload.ToArray();
    }

    /// <summary>
    /// Resolves one <c>XBLOCK</c> child of an <c>XXBLOCK</c> to its concatenated data.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The <c>XBLOCK</c> identifier.</param>
    /// <returns>The concatenated data of the block's children.</returns>
    /// <exception cref="PstFileFormatException">The block is not an <c>XBLOCK</c>.</exception>
    private static byte[] ResolveXBlock(PstSource source, ulong blockId)
    {
        byte[] block = ReadBlock(source, blockId);
        (byte level, int count) = ParseTreeBlock(block, blockId);
        if (level != 1)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId));
        }

        using var payload = new MemoryStream();
        for (int i = 0; i < count; i++)
            payload.Write(ReadBlock(source, BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(8 + (i * 8)))));

        return payload.ToArray();
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
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId));
        }

        int count = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(2));
        if (8 + (count * 8) > block.Length)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId));
        }

        return (block[1], count);
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
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstDataTree, blockId));
        }

        return source.ReadBlock(entry);
    }
}
