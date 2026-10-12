// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Walks the two on-disk B-trees - the node B-tree (NBT) and the block B-tree (BBT) - over 512-byte <c>BTPAGE</c>s:
/// in-order enumeration and keyed search.
/// </summary>
/// <remarks>
/// <para>
/// A <c>BTPAGE</c> holds its entries at stride <c>cbEnt</c> in the first 488 bytes; <c>cLevel</c> above zero means the
/// entries are <c>BTENTRY</c> references to child pages, and zero means leaf entries - <c>NBTENTRY</c> for the node
/// tree, <c>BBTENTRY</c> for the block tree. Search descends by the greatest key at or below the target.
/// </para>
/// <para>
/// Every key on a child page lies in the range its parent entry gives it: at or above the entry's key (MS-PST
/// 2.2.2.7.7.2), and below the key of the entry that follows it in the parent or, for a parent's last entry, in an
/// ancestor. Enumeration and search both hold each page they read to that range, so a page two entries reference is
/// rejected rather than read twice.
/// </para>
/// </remarks>
internal static class PstBTree
{
    /// <summary>The deepest node or block B-tree the reader descends. Real stores are a handful of levels deep; the cap turns a page that references itself or an ancestor into a format error instead of unbounded recursion.</summary>
    private const int MaxDepth = 16;

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
        Func<PstLayout, byte[], int, TEntry> readEntry) =>
        EnumerateLeaves(source, bref, pageType, readEntry, expectedLevel: -1, depth: 0, lowKey: 0, highKey: null);

    /// <summary>
    /// Enumerates the leaf entries beneath a page in key order, checking that every child sits exactly one level below
    /// its parent, that its keys lie in the range its parent entry gives it, and that the descent stays within
    /// <see cref="MaxDepth" />.
    /// </summary>
    /// <typeparam name="TEntry">The leaf entry type.</typeparam>
    /// <param name="source">The open source.</param>
    /// <param name="bref">The page reference.</param>
    /// <param name="pageType">The expected page type.</param>
    /// <param name="readEntry">Reads one leaf entry from its bytes.</param>
    /// <param name="expectedLevel">The level the page must declare, or <c>-1</c> for the root.</param>
    /// <param name="depth">The number of pages above this one.</param>
    /// <param name="lowKey">The smallest key the page may hold: its parent entry's key, or zero for the root.</param>
    /// <param name="highKey">The key every key on the page must be below, or <see langword="null" /> for none.</param>
    /// <returns>The leaf entries.</returns>
    /// <exception cref="PstFileFormatException">
    /// The page's level is not the one its parent implies, a key lies outside the range its parent entry gives the
    /// page, or the tree is deeper than the format allows - a crafted page referencing itself, an ancestor, or a page
    /// another entry references can neither recurse without bound nor be read twice.
    /// </exception>
    private static IEnumerable<TEntry> EnumerateLeaves<TEntry>(
        PstSource source,
        PstBref bref,
        byte pageType,
        Func<PstLayout, byte[], int, TEntry> readEntry,
        int expectedLevel,
        int depth,
        ulong lowKey,
        ulong? highKey)
    {
        PstLayout layout = source.Layout;
        byte[] page = source.ReadPage(bref, pageType);
        (int count, int stride, int level) = ReadPageHeader(layout, page, bref, expectedLevel, depth);
        CheckKeyRange(layout, page, bref, count, stride, lowKey, highKey);

        for (int i = 0; i < count; i++)
        {
            int offset = i * stride;
            if (level > 0)
            {
                // A child holds the keys from its entry's key up to the next entry's key; the last child inherits this
                // page's upper bound.
                PstBref child = ReadChildReference(layout, page, offset);
                ulong childLowKey = layout.ReadId(page.AsSpan(offset));
                ulong? childHighKey = i + 1 < count ? layout.ReadId(page.AsSpan(offset + stride)) : highKey;
                foreach (TEntry entry in EnumerateLeaves(source, child, pageType, readEntry, level - 1, depth + 1, childLowKey, childHighKey))
                    yield return entry;
            }
            else
            {
                yield return readEntry(layout, page, offset);
            }
        }
    }

    /// <summary>
    /// Searches beneath a page for a leaf entry with an exact key, descending iteratively with the same level and depth
    /// checks as enumeration.
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
    /// <exception cref="PstFileFormatException">
    /// A page's level is not the one its parent implies, a key lies outside the range its parent entry gives the page,
    /// or the tree is deeper than the format allows.
    /// </exception>
    private static bool TryFindLeaf<TEntry>(
        PstSource source,
        PstBref bref,
        byte pageType,
        ulong key,
        Func<PstLayout, byte[], int, TEntry> readEntry,
        Func<TEntry, ulong> keyOf,
        out TEntry entry)
        where TEntry : struct
    {
        PstLayout layout = source.Layout;
        PstBref current = bref;
        int expectedLevel = -1;
        ulong lowKey = 0;
        ulong? highKey = null;
        for (int depth = 0; ; depth++)
        {
            byte[] page = source.ReadPage(current, pageType);
            (int count, int stride, int level) = ReadPageHeader(layout, page, current, expectedLevel, depth);
            CheckKeyRange(layout, page, current, count, stride, lowKey, highKey);

            if (level == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    TEntry candidate = readEntry(layout, page, i * stride);
                    if (keyOf(candidate) == key)
                    {
                        entry = candidate;
                        return true;
                    }
                }

                entry = default;
                return false;
            }

            // Descend into the child whose key is the greatest at or below the target; the next entry's key, or this
            // page's upper bound after its last entry, bounds the keys the child may hold.
            int chosen = -1;
            for (int i = 0; i < count && layout.ReadId(page.AsSpan(i * stride)) <= key; i++)
                chosen = i;

            if (chosen < 0)
            {
                entry = default;
                return false;
            }

            current = ReadChildReference(layout, page, chosen * stride);
            lowKey = layout.ReadId(page.AsSpan(chosen * stride));
            highKey = chosen + 1 < count ? layout.ReadId(page.AsSpan((chosen + 1) * stride)) : highKey;
            expectedLevel = level - 1;
        }
    }

    /// <summary>
    /// Reads and validates a page's entry geometry and its place in the tree.
    /// </summary>
    /// <param name="layout">The file layout.</param>
    /// <param name="page">The page bytes.</param>
    /// <param name="bref">The page reference, for diagnostics.</param>
    /// <param name="expectedLevel">The level the page must declare, or <c>-1</c> when it is the root.</param>
    /// <param name="depth">The number of pages above this one.</param>
    /// <returns>The entry count, entry stride, and tree level.</returns>
    /// <exception cref="PstFileFormatException">
    /// The geometry escapes the entry area, the level differs from the one the parent implies, or the depth exceeds
    /// <see cref="MaxDepth" />.
    /// </exception>
    private static (int Count, int Stride, int Level) ReadPageHeader(PstLayout layout, byte[] page, PstBref bref, int expectedLevel, int depth)
    {
        int count = page[layout.PageEntryCountOffset];
        int stride = page[layout.PageEntryStrideOffset];
        int level = page[layout.PageLevelOffset];
        if (stride < layout.BbtLeafStride || count * stride > layout.PageEntryArea || depth > MaxDepth || (expectedLevel >= 0 && level != expectedLevel))
        {
            throw new PstFileFormatException(
                string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPage, bref.Offset),
                PstFileError.InvalidPage);
        }

        return (count, stride, level);
    }

    /// <summary>
    /// Checks that every key on a page lies in the range its parent entry gives it.
    /// </summary>
    /// <param name="layout">The file layout.</param>
    /// <param name="page">The page bytes.</param>
    /// <param name="bref">The page reference, for diagnostics.</param>
    /// <param name="count">The entry count.</param>
    /// <param name="stride">The entry stride.</param>
    /// <param name="lowKey">The smallest key the page may hold.</param>
    /// <param name="highKey">The key every key on the page must be below, or <see langword="null" /> for none.</param>
    /// <exception cref="PstFileFormatException">A key lies outside the range.</exception>
    /// <remarks>
    /// Every entry begins with its key: a <c>BTENTRY</c>'s <c>btkey</c>, an <c>NBTENTRY</c>'s node identifier, and a
    /// <c>BBTENTRY</c>'s block identifier.
    /// </remarks>
    private static void CheckKeyRange(PstLayout layout, byte[] page, PstBref bref, int count, int stride, ulong lowKey, ulong? highKey)
    {
        for (int i = 0; i < count; i++)
        {
            ulong key = layout.ReadId(page.AsSpan(i * stride));
            if (key < lowKey || key >= highKey)
            {
                throw new PstFileFormatException(
                    string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPageKeyRange, bref.Offset),
                    PstFileError.InvalidPage);
            }
        }
    }

    /// <summary>
    /// Reads the child page reference of an intermediate entry: the <c>BREF</c> that follows the entry's key.
    /// </summary>
    /// <param name="layout">The file layout.</param>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The child page reference.</returns>
    private static PstBref ReadChildReference(PstLayout layout, byte[] page, int offset) =>
        layout.ReadBref(page.AsSpan(offset + layout.IdWidth));

    /// <summary>
    /// Reads a node B-tree leaf entry (<c>NBTENTRY</c>).
    /// </summary>
    /// <param name="layout">The file layout.</param>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The node entry.</returns>
    /// <exception cref="PstFileFormatException">
    /// A Unicode entry's 64-bit <c>nid</c> field exceeds the 32-bit identifier space.
    /// </exception>
    private static PstNbtEntry ReadNbtEntry(PstLayout layout, byte[] page, int offset)
    {
        // In the Unicode layout the nid field is eight bytes wide but a node identifier is 32 bits; a set high dword
        // is not a truncation to tolerate but a page that does not hold what its type claims. The ANSI field is 32 bits.
        ulong nodeId = layout.ReadId(page.AsSpan(offset));
        if (nodeId > uint.MaxValue)
        {
            throw new PstFileFormatException(
                string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstNodeIdentifier, nodeId),
                PstFileError.InvalidPage);
        }

        int idWidth = layout.IdWidth;
        return new PstNbtEntry(
            (uint)nodeId,
            layout.ReadId(page.AsSpan(offset + idWidth)),
            layout.ReadId(page.AsSpan(offset + (idWidth * 2))),
            BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(offset + (idWidth * 3))));
    }

    /// <summary>
    /// Reads a block B-tree leaf entry (<c>BBTENTRY</c>).
    /// </summary>
    /// <param name="layout">The file layout.</param>
    /// <param name="page">The page bytes.</param>
    /// <param name="offset">The entry offset.</param>
    /// <returns>The block entry.</returns>
    private static PstBbtEntry ReadBbtEntry(PstLayout layout, byte[] page, int offset) =>
        new(
            layout.ReadBref(page.AsSpan(offset)),
            BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(offset + layout.BrefSize)),
            BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(offset + layout.BrefSize + 2)));
}
