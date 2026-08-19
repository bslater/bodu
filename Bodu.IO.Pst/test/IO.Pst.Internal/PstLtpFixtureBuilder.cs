// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstLtpFixtureBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Authors LTP payload bytes — heap-on-node blocks with their page maps, and the BTree-on-heap, property-context, and
/// table-context structures built over them. Composed with <see cref="PstFixtureBuilder" />, which wraps the produced
/// blocks in a structurally valid container, so each test controls the exact heap shape it asserts.
/// </summary>
internal sealed class PstLtpFixtureBuilder
{
    /// <summary>The items of each heap block, in allocation order.</summary>
    private readonly List<List<byte[]>> _blocks = [[]];

    /// <summary>
    /// Gets or sets the client signature (<c>bClientSig</c>) the heap header declares.
    /// </summary>
    /// <value>The signature byte; a property context (<c>0xBC</c>) by default.</value>
    internal byte ClientSignature { get; set; } = 0xBC;

    /// <summary>
    /// Gets or sets the client-defined root <c>HID</c> the heap header records.
    /// </summary>
    /// <value>The <c>hidUserRoot</c> value; zero by default.</value>
    internal uint UserRootHid { get; set; }

    /// <summary>
    /// Composes the <c>HID</c> for an item position.
    /// </summary>
    /// <param name="blockIndex">The zero-based heap-block index.</param>
    /// <param name="itemIndex">The one-based item index within the block.</param>
    /// <returns>The heap identifier.</returns>
    internal static uint Hid(int blockIndex, int itemIndex) =>
        (uint)((blockIndex << 16) | (itemIndex << 5));

    /// <summary>
    /// Adds an item to the current heap block.
    /// </summary>
    /// <param name="item">The item bytes.</param>
    /// <returns>The <c>HID</c> the item will resolve under.</returns>
    internal uint AddItem(byte[] item)
    {
        _blocks[^1].Add(item);
        return Hid(_blocks.Count - 1, _blocks[^1].Count);
    }

    /// <summary>
    /// Starts a new heap block; subsequent items are allocated there.
    /// </summary>
    internal void StartBlock() =>
        _blocks.Add([]);

    /// <summary>
    /// Builds the heap-block payloads, one per started block, each carrying its header and page map.
    /// </summary>
    /// <returns>The block payloads in order.</returns>
    internal List<byte[]> BuildBlocks()
    {
        var result = new List<byte[]>(_blocks.Count);
        for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
        {
            List<byte[]> items = _blocks[blockIndex];
            int headerSize = HeaderSize(blockIndex);
            int contentLength = items.Sum(static i => i.Length);
            int mapOffset = headerSize + contentLength;

            var block = new byte[mapOffset + 4 + ((items.Count + 1) * 2)];
            BinaryPrimitives.WriteUInt16LittleEndian(block, (ushort)mapOffset);
            if (blockIndex == 0)
            {
                block[2] = 0xEC;
                block[3] = ClientSignature;
                BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), UserRootHid);
            }

            int offset = headerSize;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(mapOffset), (ushort)items.Count);
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(mapOffset + 4), (ushort)offset);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].CopyTo(block, offset);
                offset += items[i].Length;
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(mapOffset + 4 + ((i + 1) * 2)), (ushort)offset);
            }

            result.Add(block);
        }

        return result;
    }

    /// <summary>
    /// Adds a leaf-only BTree-on-heap over the supplied records and returns the header item's <c>HID</c>.
    /// </summary>
    /// <param name="keySize">The key width in bytes.</param>
    /// <param name="dataSize">The leaf-record data width in bytes.</param>
    /// <param name="records">The records in key order: the key's little-endian unsigned value and its data bytes.</param>
    /// <returns>The <c>HID</c> of the <c>BTHHEADER</c> item.</returns>
    internal uint AddBTreeOnHeap(byte keySize, byte dataSize, params (ulong Key, byte[] Data)[] records)
    {
        uint rootHid = 0;
        if (records.Length > 0)
        {
            var leaf = new byte[(keySize + dataSize) * records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                int offset = i * (keySize + dataSize);
                WriteKey(leaf.AsSpan(offset, keySize), records[i].Key);
                records[i].Data.CopyTo(leaf, offset + keySize);
            }

            rootHid = AddItem(leaf);
        }

        return AddItem(BuildBthHeader(keySize, dataSize, indexLevels: 0, rootHid));
    }

    /// <summary>
    /// Adds a <c>BTHHEADER</c> item verbatim, for trees whose index items the test lays out itself.
    /// </summary>
    /// <param name="keySize">The key width in bytes.</param>
    /// <param name="dataSize">The leaf-record data width in bytes.</param>
    /// <param name="indexLevels">The number of index levels above the leaves.</param>
    /// <param name="rootHid">The root item's <c>HID</c>.</param>
    /// <returns>The <c>HID</c> of the header item.</returns>
    internal uint AddBthHeaderItem(byte keySize, byte dataSize, byte indexLevels, uint rootHid) =>
        AddItem(BuildBthHeader(keySize, dataSize, indexLevels, rootHid));

    /// <summary>
    /// Adds one BTH index item over child items.
    /// </summary>
    /// <param name="keySize">The key width in bytes.</param>
    /// <param name="entries">The index records: each child's first key and the child item's <c>HID</c>.</param>
    /// <returns>The <c>HID</c> of the index item.</returns>
    internal uint AddBthIndexItem(byte keySize, params (ulong Key, uint ChildHid)[] entries)
    {
        var item = new byte[(keySize + 4) * entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            int offset = i * (keySize + 4);
            WriteKey(item.AsSpan(offset, keySize), entries[i].Key);
            BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan(offset + keySize), entries[i].ChildHid);
        }

        return AddItem(item);
    }

    /// <summary>
    /// Adds one BTH leaf item over records, for trees whose index items the test lays out itself.
    /// </summary>
    /// <param name="keySize">The key width in bytes.</param>
    /// <param name="dataSize">The leaf-record data width in bytes.</param>
    /// <param name="records">The records in key order.</param>
    /// <returns>The <c>HID</c> of the leaf item.</returns>
    internal uint AddBthLeafItem(byte keySize, byte dataSize, params (ulong Key, byte[] Data)[] records)
    {
        var leaf = new byte[(keySize + dataSize) * records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            int offset = i * (keySize + dataSize);
            WriteKey(leaf.AsSpan(offset, keySize), records[i].Key);
            records[i].Data.CopyTo(leaf, offset + keySize);
        }

        return AddItem(leaf);
    }

    /// <summary>
    /// Adds a property context over the supplied records: the records' tree becomes the heap's client root and the
    /// heap declares the property-context signature.
    /// </summary>
    /// <param name="records">The property records: identifier, wire type, and raw value dword.</param>
    /// <returns>The <c>HID</c> of the context's <c>BTHHEADER</c> item.</returns>
    internal uint AddPropertyContext(params (ushort PropertyId, ushort WireType, uint RawValue)[] records)
    {
        var rows = records
            .OrderBy(static r => r.PropertyId)
            .Select(static r =>
            {
                var data = new byte[6];
                BinaryPrimitives.WriteUInt16LittleEndian(data, r.WireType);
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2), r.RawValue);
                return ((ulong)r.PropertyId, data);
            })
            .ToArray();

        uint headerHid = AddBTreeOnHeap(2, 6, rows);
        ClientSignature = 0xBC;
        UserRootHid = headerHid;
        return headerHid;
    }

    /// <summary>
    /// Builds a <c>BTHHEADER</c> item's bytes.
    /// </summary>
    /// <param name="keySize">The key width in bytes.</param>
    /// <param name="dataSize">The leaf-record data width in bytes.</param>
    /// <param name="indexLevels">The number of index levels above the leaves.</param>
    /// <param name="rootHid">The root item's <c>HID</c>.</param>
    /// <returns>The header bytes.</returns>
    private static byte[] BuildBthHeader(byte keySize, byte dataSize, byte indexLevels, uint rootHid)
    {
        var header = new byte[8];
        header[0] = 0xB5;
        header[1] = keySize;
        header[2] = dataSize;
        header[3] = indexLevels;
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), rootHid);
        return header;
    }

    /// <summary>
    /// Writes a key of up to eight bytes as its little-endian value.
    /// </summary>
    /// <param name="destination">The key bytes.</param>
    /// <param name="key">The key value.</param>
    private static void WriteKey(Span<byte> destination, ulong key)
    {
        Span<byte> full = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(full, key);
        full.Slice(0, destination.Length).CopyTo(destination);
    }

    /// <summary>
    /// Adds the heap's blocks to a container and declares a node whose data carries them.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <param name="nodeId">The node identifier to declare.</param>
    /// <param name="subnodeBlockId">The node's subnode-tree block identifier, or <c>0</c> for none.</param>
    /// <returns>The container builder, for chaining.</returns>
    internal PstFixtureBuilder AddHeapNode(PstFixtureBuilder file, uint nodeId, ulong subnodeBlockId = 0)
    {
        List<byte[]> blocks = BuildBlocks();
        ulong dataBlockId = blocks.Count == 1
            ? file.AddDataBlock(blocks[0])
            : file.AddXBlock((uint)blocks.Sum(static b => b.Length), [.. blocks.Select(file.AddDataBlock)]);

        return file.AddNode(nodeId, dataBlockId, subnodeBlockId);
    }

    /// <summary>
    /// Computes the header size a heap block carries at its position: the <c>HNHDR</c> on block zero, the
    /// <c>HNBITMAPHDR</c> on block 8 and every 128 blocks thereafter, and the two-byte <c>HNPAGEHDR</c> otherwise.
    /// </summary>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <returns>The header size in bytes.</returns>
    private static int HeaderSize(int blockIndex)
    {
        if (blockIndex == 0)
            return 12;

        return blockIndex == 8 || (blockIndex > 8 && (blockIndex - 8) % 128 == 0) ? 66 : 2;
    }
}
