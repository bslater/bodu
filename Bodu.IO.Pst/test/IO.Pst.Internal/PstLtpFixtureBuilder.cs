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
