// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSubnodeTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Parses a node's subnode tree — the private namespace of child nodes an <c>SLBLOCK</c> holds directly or an
/// <c>SIBLOCK</c> spreads over multiple <c>SLBLOCK</c>s.
/// </summary>
internal static class PstSubnodeTree
{
    /// <summary>The subnode-block type byte shared by <c>SLBLOCK</c> and <c>SIBLOCK</c>.</summary>
    private const byte SubnodeBlockType = 0x02;

    /// <summary>
    /// Reads the subnode entries beneath a subnode-tree block identifier, in stored order.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The subnode-tree block identifier; <c>0</c> yields no entries.</param>
    /// <returns>
    /// The subnode entries: node identifier, data-block identifier, and nested subnode-tree identifier.
    /// </returns>
    /// <exception cref="PstFileFormatException">A subnode block is malformed.</exception>
    internal static List<PstNbtEntry> Read(PstSource source, ulong blockId)
    {
        var entries = new List<PstNbtEntry>();
        if (blockId == 0)
            return entries;

        AppendBlock(source, blockId, entries, allowIndex: true);
        return entries;
    }

    /// <summary>
    /// Appends the entries of one subnode block, recursing through an index block's children.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The subnode block identifier.</param>
    /// <param name="entries">The list receiving the entries.</param>
    /// <param name="allowIndex">Whether an <c>SIBLOCK</c> is permitted at this position.</param>
    /// <exception cref="PstFileFormatException">The block is not a well-formed subnode block.</exception>
    private static void AppendBlock(PstSource source, ulong blockId, List<PstNbtEntry> entries, bool allowIndex)
    {
        byte[] block = PstDataTree.ReadBlock(source, blockId);
        if (block.Length < 8 || block[0] != SubnodeBlockType)
            throw Malformed(blockId);

        byte level = block[1];
        int count = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(2));
        ReadOnlySpan<byte> body = block.AsSpan(8);

        if (level == 0)
        {
            // SLBLOCK: 24-byte SLENTRY rows — node id, data block id, nested subnode block id.
            if (count * 24 > body.Length)
                throw Malformed(blockId);

            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> row = body.Slice(i * 24, 24);
                entries.Add(new PstNbtEntry(
                    (uint)BinaryPrimitives.ReadUInt64LittleEndian(row),
                    BinaryPrimitives.ReadUInt64LittleEndian(row.Slice(8)),
                    BinaryPrimitives.ReadUInt64LittleEndian(row.Slice(16)),
                    ParentNodeId: 0));
            }
        }
        else if (level == 1 && allowIndex)
        {
            // SIBLOCK: 16-byte SIENTRY rows — node id, SLBLOCK id.
            if (count * 16 > body.Length)
                throw Malformed(blockId);

            for (int i = 0; i < count; i++)
                AppendBlock(source, BinaryPrimitives.ReadUInt64LittleEndian(body.Slice((i * 16) + 8)), entries, allowIndex: false);
        }
        else
        {
            throw Malformed(blockId);
        }
    }

    /// <summary>
    /// Creates the malformed-subnode-tree exception for a block identifier.
    /// </summary>
    /// <param name="blockId">The offending block identifier.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException Malformed(ulong blockId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstSubnodeTree, blockId));
}
