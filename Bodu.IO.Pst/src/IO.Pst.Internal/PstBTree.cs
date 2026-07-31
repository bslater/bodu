// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Walks the two on-disk B-trees — the node B-tree (NBT) and the block B-tree (BBT) — over 512-byte <c>BTPAGE</c>s:
/// in-order enumeration and keyed search.
/// </summary>
/// <remarks>
/// A <c>BTPAGE</c> holds its entries at stride <c>cbEnt</c> in the first 488 bytes; <c>cLevel</c> above zero means the
/// entries are <c>BTENTRY</c> references to child pages, and zero means leaf entries — <c>NBTENTRY</c> for the node
/// tree, <c>BBTENTRY</c> for the block tree. Search descends by the greatest key at or below the target.
/// </remarks>
internal static class PstBTree
{
    /// <summary>The NBT page type.</summary>
    internal const byte NodePageType = 0x81;

    /// <summary>The BBT page type.</summary>
    internal const byte BlockPageType = 0x80;

    /// <summary>
    /// Enumerates every leaf entry of the node B-tree in key order.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <returns>The node entries.</returns>
    internal static IEnumerable<PstNbtEntry> EnumerateNodes(PstSource source) =>
        EnumerateLeaves(source, source.Header.NbtRoot, NodePageType, ReadNbtEntry);

    /// <summary>
    /// Searches the node B-tree for a node identifier.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="nodeId">The node identifier to find.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the matching entry.</param>
    /// <returns><see langword="true" /> when the node exists.</returns>
    internal static bool TryFindNode(PstSource source, uint nodeId, out PstNbtEntry entry)
    {
        if (TryFindLeaf(source, source.Header.NbtRoot, NodePageType, nodeId, ReadNbtEntry, static e => e.NodeId, out entry))
            return true;

        entry = default;
        return false;
    }

    /// <summary>
    /// Searches the block B-tree for a block identifier.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="blockId">The block identifier to find.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the matching entry.</param>
    /// <returns><see langword="true" /> when the block exists.</returns>
    internal static bool TryFindBlock(PstSource source, ulong blockId, out PstBbtEntry entry)
    {
        if (TryFindLeaf(source, source.Header.BbtRoot, BlockPageType, blockId, ReadBbtEntry, static e => e.Bref.BlockId, out entry))
            return true;

        entry = default;
        return false;
    }

    /// <summary>
    /// Enumerates the leaf entries beneath a page in key order.
    /// </summary>
    /// <typeparam name="TEntry">The leaf entry type.</typeparam>
    /// <param name="source">The open source.</param>
    /// <param name="bref">The page reference.</param>
    /// <param name="pageType">The expected page type.</param>
    /// <param name="readEntry">Reads one leaf entry from its bytes.</param>
    /// <returns>The leaf entries.</returns>
    private static IEnumerable<TEntry> EnumerateLeaves<TEntry>(
        PstSource source,
        PstBref bref,
        byte pageType,
        Func<byte[], int, TEntry> readEntry)
    {
        byte[] page = source.ReadPage(bref, pageType);
        (int count, int stride, int level) = ReadPageHeader(page, bref);

        for (int i = 0; i < count; i++)
        {
            int offset = i * stride;
            if (level > 0)
            {
                PstBref child = ReadChildReference(page, offset);
                foreach (TEntry entry in EnumerateLeaves(source, child, pageType, readEntry))
                    yield return entry;
            }
            else
            {
                yield return readEntry(page, offset);
            }
        }
    }

    /// <summary>
    /// Searches beneath a page for a leaf entry with an exact key.
    /// </summary>
    /// <typeparam name="TEntry">The leaf entry type.</typeparam>
    /// <param name="source">The open source.</param>
    /// <param name="bref">The page reference.</param>
    /// <param name="pageType">The expected page type.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="readEntry">Reads one leaf entry from its bytes.</param>
    /// <param name="keyOf">Extracts the key of a leaf entry.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the matching entry.</param>
    /// <returns><see langword="true" /> when the key exists.</returns>
    private static bool TryFindLeaf<TEntry>(
        PstSource source,
        PstBref bref,
        byte pageType,
        ulong key,
        Func<byte[], int, TEntry> readEntry,
        Func<TEntry, ulong> keyOf,
        out TEntry entry)
        where TEntry : struct
    {
        byte[] page = source.ReadPage(bref, pageType);
        (int count, int stride, int level) = ReadPageHeader(page, bref);

        if (level > 0)
        {
            // Descend into the child whose key is the greatest at or below the target.
            PstBref child = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                int offset = i * stride;
                ulong entryKey = BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset));
                if (entryKey > key)
                    break;

                child = ReadChildReference(page, offset);
                found = true;
            }

            if (found)
                return TryFindLeaf(source, child, pageType, key, readEntry, keyOf, out entry);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                TEntry candidate = readEntry(page, i * stride);
                if (keyOf(candidate) == key)
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Reads and validates a page's entry geometry.
    /// </summary>
    /// <param name="page">The page bytes.</param>
    /// <param name="bref">The page reference, for diagnostics.</param>
    /// <returns>The entry count, entry stride, and tree level.</returns>
    /// <exception cref="PstFileFormatException">The geometry escapes the entry area.</exception>
    private static (int Count, int Stride, int Level) ReadPageHeader(byte[] page, PstBref bref)
    {
        int count = page[488];
        int stride = page[490];
        int level = page[491];
        if (stride < 16 || count * stride > 488)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPage, bref.Offset));
        }

        return (count, stride, level);
    }

    /// <summary>
    /// Reads an intermediate entry's child page reference (the <c>BREF</c> after the 8-byte key).
    /// </summary>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The child page reference.</returns>
    private static PstBref ReadChildReference(byte[] page, int offset) =>
        new(
            BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset + 8)),
            BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset + 16)));

    /// <summary>
    /// Reads a leaf <c>NBTENTRY</c>.
    /// </summary>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The node entry.</returns>
    private static PstNbtEntry ReadNbtEntry(byte[] page, int offset) =>
        new(
            (uint)BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset)),
            BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset + 8)),
            BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset + 16)),
            BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(offset + 24)));

    /// <summary>
    /// Reads a leaf <c>BBTENTRY</c>.
    /// </summary>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The block entry.</returns>
    private static PstBbtEntry ReadBbtEntry(byte[] page, int offset) =>
        new(
            new PstBref(
                BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset)),
                BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(offset + 8))),
            BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(offset + 16)),
            BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(offset + 18)));
}
