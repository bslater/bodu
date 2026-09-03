// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeOnHeap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Reads a BTree-on-heap (<c>BTH</c>) — the keyed record store the LTP builds inside a heap-on-node, used by the
/// property context (keyed by property id) and the table context's row index (keyed by row id).
/// </summary>
/// <remarks>
/// Keys are little-endian unsigned integers of the header's declared width. Descent is bounded three ways: the
/// header's index level count is capped at <see cref="MaxIndexLevels" />, an index item that names itself or an
/// ancestor as a child is rejected, and the record count is bounded by the heap size — so a crafted file can neither
/// recurse without bound nor multiply the enumeration. Under <see cref="PstValidationLevel.Strict" />, leaf keys must
/// be strictly increasing.
/// </remarks>
internal static class PstBTreeOnHeap
{
    /// <summary>
    /// The deepest index a BTree-on-heap may declare. A tree over an 8 KiB-block heap needs a handful of levels at
    /// most; the cap keeps a crafted level count from multiplying the descent.
    /// </summary>
    private const int MaxIndexLevels = 8;

    /// <summary>
    /// Reads and validates the <c>BTHHEADER</c> the supplied heap item carries.
    /// </summary>
    /// <param name="heap">The heap holding the tree.</param>
    /// <param name="hid">The header item's heap identifier.</param>
    /// <returns>The parsed header.</returns>
    /// <exception cref="PstFileFormatException">The item is not a well-formed <c>BTHHEADER</c>.</exception>
    internal static PstBthHeader ReadHeader(PstHeapNode heap, uint hid)
    {
        ReadOnlySpan<byte> item = heap.GetItem(hid).Span;
        if (item.Length < 8 || item[0] != PstHeapNode.BTreeOnHeapSignature)
            throw Malformed(heap);

        byte keySize = item[1];
        if (keySize is not (2 or 4 or 8 or 16))
            throw Malformed(heap);

        byte dataSize = item[2];
        if (dataSize == 0)
            throw Malformed(heap);

        // A BTH over an 8 KiB-block heap is at most a few levels deep; a declared depth beyond the cap is a crafted
        // multiplier for the descent below, not a real tree.
        byte indexLevels = item[3];
        if (indexLevels > MaxIndexLevels)
            throw Malformed(heap);

        return new PstBthHeader(keySize, dataSize, indexLevels, BinaryPrimitives.ReadUInt32LittleEndian(item.Slice(4)));
    }

    /// <summary>
    /// Enumerates the tree's leaf records in stored (key) order.
    /// </summary>
    /// <param name="heap">The heap holding the tree.</param>
    /// <param name="header">The tree's parsed header.</param>
    /// <param name="validationLevel">The active validation level.</param>
    /// <returns>The leaf records as key/data byte pairs of the header's declared widths.</returns>
    /// <exception cref="PstFileFormatException">
    /// A record item's length is not a multiple of its record stride, a descent identifier does not resolve, or —
    /// under <see cref="PstValidationLevel.Strict" /> — leaf keys are not strictly increasing.
    /// </exception>
    internal static IEnumerable<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Data)> EnumerateRecords(
        PstHeapNode heap, PstBthHeader header, PstValidationLevel validationLevel)
    {
        if (header.RootHid == 0)
            yield break;

        bool enforceOrder = validationLevel == PstValidationLevel.Strict && header.KeySize <= 8;
        ulong previousKey = 0;
        bool first = true;

        // Every record occupies distinct heap bytes, so the heap size bounds the record count; an index that reaches
        // the same leaf through several entries would otherwise multiply the enumeration without bound.
        long remaining = (heap.TotalLength / (header.KeySize + header.DataSize)) + 1;
        var path = new HashSet<uint>();

        foreach ((ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> data) in EnumerateLevel(heap, header, header.RootHid, header.IndexLevels, path))
        {
            if (--remaining < 0)
                throw Malformed(heap);

            if (enforceOrder)
            {
                ulong keyValue = ReadKey(key.Span, header.KeySize);
                if (!first && keyValue <= previousKey)
                    throw Malformed(heap);

                previousKey = keyValue;
                first = false;
            }

            yield return (key, data);
        }
    }

    /// <summary>
    /// Attempts to find the leaf record with the supplied key.
    /// </summary>
    /// <param name="heap">The heap holding the tree.</param>
    /// <param name="header">The tree's parsed header.</param>
    /// <param name="key">The key to find, as the little-endian unsigned value of the header's key width.</param>
    /// <param name="data">When this method returns <see langword="true" />, the record's data bytes.</param>
    /// <returns><see langword="true" /> when a record with the key exists.</returns>
    /// <exception cref="PstFileFormatException">
    /// The tree declares 16-byte keys, which this lookup cannot compare, or a descent identifier does not resolve.
    /// </exception>
    internal static bool TryFind(PstHeapNode heap, PstBthHeader header, ulong key, out ReadOnlyMemory<byte> data)
    {
        data = default;

        if (header.KeySize > 8)
            throw Malformed(heap);

        if (header.RootHid == 0)
            return false;

        uint currentHid = header.RootHid;
        for (int level = header.IndexLevels; level > 0; level--)
        {
            ReadOnlyMemory<byte> item = heap.GetItem(currentHid);
            int stride = header.KeySize + 4;
            int count = RecordCount(heap, item.Length, stride);

            // Follow the last child whose first key does not exceed the target; a target below every key is absent.
            uint nextHid = 0;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> record = item.Span.Slice(i * stride, stride);
                if (ReadKey(record, header.KeySize) > key)
                    break;

                nextHid = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(header.KeySize));
                found = true;
            }

            if (!found)
                return false;

            currentHid = nextHid;
        }

        ReadOnlyMemory<byte> leaf = heap.GetItem(currentHid);
        int leafStride = header.KeySize + header.DataSize;
        int leafCount = RecordCount(heap, leaf.Length, leafStride);
        for (int i = 0; i < leafCount; i++)
        {
            ulong recordKey = ReadKey(leaf.Span.Slice(i * leafStride, header.KeySize), header.KeySize);
            if (recordKey == key)
            {
                data = leaf.Slice((i * leafStride) + header.KeySize, header.DataSize);
                return true;
            }

            if (recordKey > key)
                break;
        }

        return false;
    }

    /// <summary>
    /// Enumerates the records beneath one item, recursing through index levels.
    /// </summary>
    /// <param name="heap">The heap holding the tree.</param>
    /// <param name="header">The tree's parsed header.</param>
    /// <param name="hid">The item's heap identifier.</param>
    /// <param name="level">The item's level: zero for a leaf, otherwise an index.</param>
    /// <param name="path">The identifiers of the items on the current descent, which a child may not repeat.</param>
    /// <returns>The leaf records beneath the item, in order.</returns>
    /// <exception cref="PstFileFormatException">An index item names itself or an ancestor as a child.</exception>
    private static IEnumerable<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Data)> EnumerateLevel(
        PstHeapNode heap, PstBthHeader header, uint hid, int level, HashSet<uint> path)
    {
        ReadOnlyMemory<byte> item = heap.GetItem(hid);

        if (level > 0)
        {
            if (!path.Add(hid))
                throw Malformed(heap);

            int stride = header.KeySize + 4;
            int count = RecordCount(heap, item.Length, stride);
            for (int i = 0; i < count; i++)
            {
                uint childHid = BinaryPrimitives.ReadUInt32LittleEndian(item.Span.Slice((i * stride) + header.KeySize, 4));
                foreach ((ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Data) record in EnumerateLevel(heap, header, childHid, level - 1, path))
                    yield return record;
            }

            path.Remove(hid);
            yield break;
        }

        int leafStride = header.KeySize + header.DataSize;
        int leafCount = RecordCount(heap, item.Length, leafStride);
        for (int i = 0; i < leafCount; i++)
        {
            yield return (
                item.Slice(i * leafStride, header.KeySize),
                item.Slice((i * leafStride) + header.KeySize, header.DataSize));
        }
    }

    /// <summary>
    /// Computes an item's record count, rejecting an item whose length is not a whole number of records.
    /// </summary>
    /// <param name="heap">The heap, for diagnostics.</param>
    /// <param name="itemLength">The item length in bytes.</param>
    /// <param name="stride">The record stride.</param>
    /// <returns>The record count.</returns>
    /// <exception cref="PstFileFormatException">The length is not a multiple of the stride.</exception>
    private static int RecordCount(PstHeapNode heap, int itemLength, int stride)
    {
        if (itemLength % stride != 0)
            throw Malformed(heap);

        return itemLength / stride;
    }

    /// <summary>
    /// Reads a key of up to eight bytes as its little-endian unsigned value.
    /// </summary>
    /// <param name="record">The bytes beginning at the key.</param>
    /// <param name="keySize">The key width: 2, 4, or 8.</param>
    /// <returns>The key value.</returns>
    private static ulong ReadKey(ReadOnlySpan<byte> record, byte keySize) => keySize switch
    {
        2 => BinaryPrimitives.ReadUInt16LittleEndian(record),
        4 => BinaryPrimitives.ReadUInt32LittleEndian(record),
        _ => BinaryPrimitives.ReadUInt64LittleEndian(record),
    };

    /// <summary>
    /// Creates the malformed-tree exception for a heap.
    /// </summary>
    /// <param name="heap">The heap holding the tree.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException Malformed(PstHeapNode heap) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBTreeOnHeap, new PstNodeId(heap.NodeId)), PstFileError.InvalidHeap);
}
