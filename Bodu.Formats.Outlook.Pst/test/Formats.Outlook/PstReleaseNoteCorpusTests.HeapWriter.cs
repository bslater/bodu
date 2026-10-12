// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.HeapWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Fills a heap-on-node item by item over <see cref="PstLtpFixtureBuilder" />, starting a new heap block whenever
    /// an item would not fit, by the catalogue README's heap rule.
    /// </summary>
    /// <remarks>
    /// An item goes in the current block unless <c>used + length + 4 + 2 * (items + 2)</c> exceeds the block payload,
    /// where <c>used</c> counts the block's header and items so far and <c>items</c> its item count: the 4 bytes are
    /// the page map's cAlloc and cFree, and the page map holds one offset more than the block's items. <c>used</c>
    /// starts at 12 (the HNHDR) in block 0, at 66 (an HNBITMAPHDR) in block 8 and every 128th block after it, and at 2
    /// (an HNPAGEHDR) otherwise.
    /// </remarks>
    private sealed class HeapWriter
    {
        /// <summary>The largest payload of one heap block.</summary>
        private readonly int _blockPayload;

        /// <summary>The zero-based index of the current heap block.</summary>
        private int _blockIndex;

        /// <summary>The bytes the current block's header and items take.</summary>
        private int _used = 12;

        /// <summary>The number of items in the current block.</summary>
        private int _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeapWriter" /> class.
        /// </summary>
        /// <param name="blockPayload">The largest payload of one heap block in the file's format.</param>
        public HeapWriter(int blockPayload)
        {
            _blockPayload = blockPayload;
        }

        /// <summary>
        /// Gets the builder that lays out the heap blocks.
        /// </summary>
        /// <remarks>
        /// A generator sets the heap's client signature and root on it, and may add items to the current block
        /// directly, as a property context's BTH is added.
        /// </remarks>
        public PstLtpFixtureBuilder Ltp { get; } = new();

        /// <summary>
        /// Gets the identifier the next <see cref="AddItem" /> call returns for an item of a given length.
        /// </summary>
        /// <param name="length">The item's length.</param>
        /// <returns>The heap identifier.</returns>
        public uint PeekNextHid(int length) =>
            Fits(length) ? PstLtpFixtureBuilder.Hid(_blockIndex, _items + 1) : PstLtpFixtureBuilder.Hid(_blockIndex + 1, 1);

        /// <summary>
        /// Adds an item, in the current heap block or the first item of a new one.
        /// </summary>
        /// <param name="item">The item bytes.</param>
        /// <returns>The item's heap identifier.</returns>
        public uint AddItem(byte[] item)
        {
            if (!Fits(item.Length))
            {
                Ltp.StartBlock();
                _blockIndex++;
                _used = _blockIndex == 8 || (_blockIndex > 8 && (_blockIndex - 8) % 128 == 0) ? 66 : 2;
                _items = 0;
            }

            _used += item.Length;
            _items++;
            return Ltp.AddItem(item);
        }

        /// <summary>
        /// Adds a BTH over records: one leaf item, or leaf items of up to 3580 bytes under one index item when the
        /// records do not fit one, then the BTHHEADER.
        /// </summary>
        /// <param name="keySize">The key width.</param>
        /// <param name="dataSize">The record data width.</param>
        /// <param name="records">The records in key order.</param>
        /// <param name="headerDataSize">
        /// The cbEnt the BTHHEADER records instead of <paramref name="dataSize" />, or <see langword="null" />.
        /// </param>
        /// <returns>The BTHHEADER's heap identifier.</returns>
        public uint AddBth(byte keySize, byte dataSize, (ulong Key, byte[] Data)[] records, byte? headerDataSize = null)
        {
            int perLeaf = InputGenerator.MaxHeapItem / (keySize + dataSize);
            if (records.Length <= perLeaf)
            {
                uint root = records.Length == 0 ? 0 : AddItem(Leaf(keySize, records));
                return AddItem(Header(keySize, headerDataSize ?? dataSize, 0, root));
            }

            var index = new List<(ulong Key, uint Hid)>();
            for (int start = 0; start < records.Length; start += perLeaf)
            {
                (ulong Key, byte[] Data)[] part = records[start..Math.Min(records.Length, start + perLeaf)];
                index.Add((part[0].Key, AddItem(Leaf(keySize, part))));
            }

            var item = new byte[(keySize + 4) * index.Count];
            for (int i = 0; i < index.Count; i++)
            {
                WriteKey(item.AsSpan(i * (keySize + 4), keySize), index[i].Key);
                BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan((i * (keySize + 4)) + keySize), index[i].Hid);
            }

            uint indexHid = AddItem(item);
            return AddItem(Header(keySize, headerDataSize ?? dataSize, 1, indexHid));
        }

        /// <summary>
        /// Writes the heap as one data block, or as data blocks under an XBLOCK.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <returns>The identifier of the data block or of the XBLOCK.</returns>
        public ulong Emit(PstFixtureBuilder file) =>
            InputGenerator.WriteHeapBlocks(file, Ltp.BuildBlocks());

        /// <summary>
        /// Lays out a BTH leaf item.
        /// </summary>
        /// <param name="keySize">The key width.</param>
        /// <param name="records">The records in key order, all with data of one width.</param>
        /// <returns>The item bytes.</returns>
        private static byte[] Leaf(byte keySize, (ulong Key, byte[] Data)[] records)
        {
            int stride = keySize + records[0].Data.Length;
            var leaf = new byte[stride * records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                WriteKey(leaf.AsSpan(i * stride, keySize), records[i].Key);
                records[i].Data.CopyTo(leaf, (i * stride) + keySize);
            }

            return leaf;
        }

        /// <summary>
        /// Lays out a BTHHEADER item: bType 0xB5, cbKey, cbEnt, bIdxLevels and hidRoot.
        /// </summary>
        /// <param name="keySize">The cbKey.</param>
        /// <param name="dataSize">The cbEnt.</param>
        /// <param name="levels">The bIdxLevels.</param>
        /// <param name="root">The hidRoot.</param>
        /// <returns>The item bytes.</returns>
        private static byte[] Header(byte keySize, byte dataSize, byte levels, uint root)
        {
            var header = new byte[8];
            header[0] = 0xB5;
            header[1] = keySize;
            header[2] = dataSize;
            header[3] = levels;
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), root);

            return header;
        }

        /// <summary>
        /// Writes a key little-endian in its width.
        /// </summary>
        /// <param name="destination">The key's bytes.</param>
        /// <param name="key">The key.</param>
        private static void WriteKey(Span<byte> destination, ulong key)
        {
            Span<byte> full = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(full, key);
            full[..destination.Length].CopyTo(destination);
        }

        /// <summary>
        /// Determines whether an item fits the current heap block.
        /// </summary>
        /// <param name="length">The item's length.</param>
        /// <returns>
        /// <see langword="true" /> when the block, its page map included, stays within the block payload.
        /// </returns>
        private bool Fits(int length) =>
            _used + length + 4 + ((_items + 2) * 2) <= _blockPayload;
    }
}
