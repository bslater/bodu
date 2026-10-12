// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.ContextGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Runs the generators that write one node holding a heap-on-node: <c>Pc</c>, a property context, and <c>Table</c>,
    /// a table context, with the heap patches both take.
    /// </summary>
    private static class ContextGenerator
    {
        /// <summary>
        /// Runs <c>Pc(prop=..., heapfill=k, emptyitem=true, bthlevels=1, leafsize=r, bthselfref=true, bthkey=n,
        /// bthent=n, bthidx=n, nid=0x21)</c> and the heap patches: one node (default 0x21) whose heap holds a property
        /// context over the <c>prop</c> values.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        /// <remarks>
        /// <para>
        /// <c>prop</c> is repeatable, in the syntax of <see cref="PropertyModel.Parse" />, with an 8-bit string read as
        /// Latin-1. <c>heapfill</c> first adds k 3000-byte zero items and <c>emptyitem</c> a zero-length item. A value
        /// stored in a subnode is <c>((0x800 + k) &lt;&lt; 5) | 0x1F</c>, k counting the node's subnodes.
        /// </para>
        /// <para>
        /// The records form one BTH leaf item, or with <c>bthlevels=1</c> leaf items of r records (default 2) under one
        /// index item; <c>bthselfref</c> makes the index item's last entry name the index item itself; <c>bthkey</c>,
        /// <c>bthent</c> and <c>bthidx</c> replace the BTHHEADER's cbKey, cbEnt and bIdxLevels.
        /// </para>
        /// </remarks>
        public static GeneratedFile PropertyContext(GeneratorCall call)
        {
            PstFixtureBuilder file = InputGenerator.NewBuilder(call);
            int fillItems = call.GetInt32("heapfill", 0);
            bool emptyItem = call.GetBoolean("emptyitem", false);
            IReadOnlyList<string> specs = call.GetAll("prop");
            int levels = call.GetInt32("bthlevels", 0);
            int perLeaf = levels == 1 ? call.GetInt32("leafsize", 2) : 0;
            bool selfReference = levels == 1 && call.GetBoolean("bthselfref", false);
            string? keySize = call.Get("bthkey");
            string? entrySize = call.Get("bthent");
            string? indexLevels = call.Get("bthidx");
            uint nodeId = call.GetUInt32("nid", 0x21);
            if (levels is not (0 or 1)) throw new FormatException("gen: Pc: bthlevels is 1, or left out for a single leaf.");

            var heap = new HeapWriter(InputGenerator.BlockPayload(file.Format));
            for (int i = 0; i < fillItems; i++)
                heap.AddItem(new byte[3000]);

            if (emptyItem)
                heap.AddItem([]);

            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            var records = new List<(ulong Key, byte[] Data)>();
            foreach (string spec in specs)
            {
                PropertyModel property = PropertyModel.Parse(spec, Encoding.Latin1);
                var data = new byte[6];
                BinaryPrimitives.WriteUInt16LittleEndian(data, property.Type);
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2), Reference(file, heap, property, subnodes));
                records.Add((property.Id, data));
            }

            records.Sort((left, right) => left.Key.CompareTo(right.Key));
            byte[] HeaderItem(byte defaultLevels, uint root)
            {
                var header = new byte[8];
                header[0] = 0xB5;
                header[1] = keySize is null ? (byte)2 : (byte)GeneratorCall.ParseInteger(keySize);
                header[2] = entrySize is null ? (byte)6 : (byte)GeneratorCall.ParseInteger(entrySize);
                header[3] = indexLevels is null ? defaultLevels : (byte)GeneratorCall.ParseInteger(indexLevels);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), root);

                return header;
            }

            uint headerHid;
            if (levels == 1)
            {
                var index = new List<(ulong Key, uint Hid)>();
                for (int start = 0; start < records.Count; start += perLeaf)
                {
                    List<(ulong Key, byte[] Data)> part = [.. records.Skip(start).Take(perLeaf)];
                    index.Add((part[0].Key, heap.AddItem(LeafItem(part))));
                }

                var item = new byte[index.Count * 6];
                for (int i = 0; i < index.Count; i++)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(item.AsSpan(i * 6), (ushort)index[i].Key);
                    BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan((i * 6) + 2), index[i].Hid);
                }

                if (selfReference)
                    BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan(((index.Count - 1) * 6) + 2), heap.PeekNextHid(item.Length));

                uint indexHid = heap.AddItem(item);
                headerHid = heap.AddItem(HeaderItem(1, indexHid));
            }
            else
            {
                uint root = records.Count == 0 ? 0 : heap.AddItem(LeafItem(records));
                headerHid = heap.AddItem(HeaderItem(0, root));
            }

            heap.Ltp.ClientSignature = 0xBC;
            heap.Ltp.UserRootHid = headerHid;
            List<byte[]> blocks = heap.Ltp.BuildBlocks();
            PatchHeap(blocks[0], call);

            ulong dataBlock = InputGenerator.WriteHeapBlocks(file, blocks);
            ulong subnodeBlock = subnodes.Count == 0 ? 0 : file.AddSubnodeLeafBlock([.. subnodes.OrderBy(subnode => subnode.NodeId)]);
            file.AddNode(nodeId, dataBlock, subnodeBlock, 0);
            return new GeneratedFile(file.Build(), file.Format);
        }

        /// <summary>
        /// Runs <c>Table(rows=n, width=w, matrix=auto|heap|subnode, padrows=true, heapfill=k, matrixbytes=m,
        /// rowidxent=e, rownumber=i:v, ibit=c:v, nid=0x802E)</c> and the heap patches: one node (default 0x802E) whose
        /// heap holds a table context of n rows (default 2) with identifiers 1 to n, each w bytes wide (default 5).
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        /// <remarks>
        /// <para>
        /// Each row holds column 0x67F20003 at offset 0, then filler columns 0x6000, 0x6001, ... (8-byte PtypInteger64
        /// first, then 4-byte PtypInteger32, 2-byte PtypInteger16 and 1-byte PtypBoolean, as many as fill the row) that
        /// no row has, then the cell existence bitmap with only the row identifier's bit set; iBit is each column's
        /// position.
        /// </para>
        /// <para>
        /// The row matrix is a heap item with <c>matrix=heap</c>, or with <c>auto</c> (the default) when n * w is at
        /// most 3580 bytes; otherwise subnode 0x0001001F, <c>floor(8176 / w)</c> rows per data block (8180 in ANSI)
        /// under an XBLOCK when there are several, each block but the last padded to the full payload with
        /// <c>padrows</c>. The row index BTH has key 4 and data 4 (2 in ANSI): row identifier to row number.
        /// <c>matrixbytes</c> keeps only the first m bytes of a heap-resident matrix, <c>rowidxent</c> replaces the row
        /// index BTHHEADER's cbEnt, <c>rownumber</c> the number recorded for row i, and <c>ibit</c> a column's iBit.
        /// </para>
        /// </remarks>
        public static GeneratedFile TableContext(GeneratorCall call)
        {
            PstFixtureBuilder file = InputGenerator.NewBuilder(call);
            int fillItems = call.GetInt32("heapfill", 0);
            int rows = call.GetInt32("rows", 2);
            int width = call.GetInt32("width", 5);
            string matrixMode = call.GetString("matrix", "auto");
            bool pad = call.GetBoolean("padrows", false);
            string? matrixBytes = call.Get("matrixbytes");
            string? rowNumber = call.Get("rownumber");
            string? indexEntrySize = call.Get("rowidxent");
            string? columnBit = call.Get("ibit");
            uint nodeId = call.GetUInt32("nid", 0x802E);
            if (matrixMode is not ("auto" or "heap" or "subnode")) throw new FormatException("gen: Table: matrix is auto, heap or subnode.");

            int payload = InputGenerator.BlockPayload(file.Format);
            var heap = new HeapWriter(payload);
            for (int i = 0; i < fillItems; i++)
                heap.AddItem(new byte[3000]);

            List<(uint Tag, int Offset, int Width)> columns = FillerColumns(width);
            int end4 = columns.Where(column => column.Width >= 4).Sum(column => column.Width);
            int end2 = end4 + columns.Where(column => column.Width == 2).Sum(column => column.Width);
            int end1 = end2 + columns.Where(column => column.Width == 1).Sum(column => column.Width);

            var matrix = new List<byte[]>();
            for (int i = 0; i < rows; i++)
            {
                var row = new byte[width];
                BinaryPrimitives.WriteUInt32LittleEndian(row, (uint)(i + 1));
                row[end1] = 0x80;
                matrix.Add(row);
            }

            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            uint matrixHnid = 0;
            if (rows > 0 && (matrixMode == "heap" || (matrixMode == "auto" && rows * width <= InputGenerator.MaxHeapItem)))
            {
                byte[] bytes = [.. matrix.SelectMany(row => row)];
                if (matrixBytes is not null)
                    bytes = bytes[..(int)GeneratorCall.ParseInteger(matrixBytes)];

                matrixHnid = heap.AddItem(bytes);
            }
            else if (rows > 0)
            {
                int perBlock = Math.Max(1, payload / width);
                var segments = new List<byte[]>();
                for (int start = 0; start < rows; start += perBlock)
                {
                    byte[] segment = [.. matrix.Skip(start).Take(perBlock).SelectMany(row => row)];
                    if (pad && start + perBlock < rows)
                        Array.Resize(ref segment, payload);

                    segments.Add(segment);
                }

                matrixHnid = (0x800u << 5) | 0x1F;
                ulong tree = InputGenerator.WriteHeapBlocks(file, segments);
                subnodes.Add((matrixHnid, tree, 0));
            }

            long[] numbers = [.. Enumerable.Range(0, rows).Select(i => (long)i)];
            if (rowNumber is not null)
            {
                (long row, long value) = ParsePair(rowNumber, "rownumber");
                numbers[row] = value;
            }

            int numberWidth = file.Format == PstFileFormat.Ansi ? 2 : 4;
            (ulong Key, byte[] Data)[] index = [.. Enumerable.Range(0, rows).Select(i =>
            {
                var number = new byte[numberWidth];
                if (numberWidth == 4)
                    BinaryPrimitives.WriteUInt32LittleEndian(number, (uint)numbers[i]);
                else
                    BinaryPrimitives.WriteUInt16LittleEndian(number, (ushort)numbers[i]);

                return ((ulong)(i + 1), number);
            })];
            byte? headerDataSize = indexEntrySize is null ? null : (byte)GeneratorCall.ParseInteger(indexEntrySize);
            uint rowIndex = heap.AddBth(4, (byte)numberWidth, index, headerDataSize);

            byte[] info = TableInfo(columns, end4, end2, end1, width, rowIndex, matrixHnid);
            if (columnBit is not null)
            {
                (long column, long bit) = ParsePair(columnBit, "ibit");
                info[22 + ((int)column * 8) + 7] = (byte)bit;
            }

            heap.Ltp.ClientSignature = 0x7C;
            heap.Ltp.UserRootHid = heap.AddItem(info);
            List<byte[]> blocks = heap.Ltp.BuildBlocks();
            PatchHeap(blocks[0], call);

            ulong dataBlock = InputGenerator.WriteHeapBlocks(file, blocks);
            ulong subnodeBlock = subnodes.Count == 0 ? 0 : file.AddSubnodeLeafBlock([.. subnodes]);
            file.AddNode(nodeId, dataBlock, subnodeBlock, 0);
            return new GeneratedFile(file.Build(), file.Format);
        }

        /// <summary>
        /// Lays out a TCINFO item: bType 0x7C, cCols, the four rgib offsets, hidRowIndex, hnidRows, hidIndex (zero),
        /// then one TCOLDESC per column, its iBit being its position.
        /// </summary>
        /// <param name="columns">The columns: tag, cell offset and cell width.</param>
        /// <param name="end4">The end of the 8- and 4-byte cells.</param>
        /// <param name="end2">The end of the 2-byte cells.</param>
        /// <param name="end1">The end of the 1-byte cells, where the existence bitmap starts.</param>
        /// <param name="rowWidth">The row width, the existence bitmap included.</param>
        /// <param name="rowIndex">The row index BTH's heap identifier.</param>
        /// <param name="matrixHnid">The row matrix's heap or subnode identifier, or zero.</param>
        /// <returns>The item bytes.</returns>
        public static byte[] TableInfo(IReadOnlyList<(uint Tag, int Offset, int Width)> columns, int end4, int end2, int end1, int rowWidth, uint rowIndex, uint matrixHnid)
        {
            var info = new byte[22 + (columns.Count * 8)];
            info[0] = 0x7C;
            info[1] = (byte)columns.Count;
            BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(2), (ushort)end4);
            BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(4), (ushort)end2);
            BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(6), (ushort)end1);
            BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(8), (ushort)rowWidth);
            BinaryPrimitives.WriteUInt32LittleEndian(info.AsSpan(10), rowIndex);
            BinaryPrimitives.WriteUInt32LittleEndian(info.AsSpan(14), matrixHnid);
            for (int i = 0; i < columns.Count; i++)
            {
                Span<byte> descriptor = info.AsSpan(22 + (i * 8), 8);
                BinaryPrimitives.WriteUInt32LittleEndian(descriptor, columns[i].Tag);
                BinaryPrimitives.WriteUInt16LittleEndian(descriptor[4..], (ushort)columns[i].Offset);
                descriptor[6] = (byte)columns[i].Width;
                descriptor[7] = (byte)i;
            }

            return info;
        }

        /// <summary>
        /// Applies the heap patches to heap block 0 after it is built: <c>hnsig</c> (bSig), <c>userroot</c>
        /// (hidUserRoot), <c>mapcount</c> (cAlloc), <c>mapfree</c> (cFree), <c>mapoffset=i:v</c> (rgibAlloc[i],
        /// repeatable), <c>hnpm</c> (ibHnpm, written last) and <c>heappatch=offset:hex</c> (repeatable).
        /// </summary>
        /// <param name="block">Heap block 0.</param>
        /// <param name="call">The call.</param>
        /// <remarks>
        /// The page map is located through the ibHnpm the builder wrote, so <c>hnpm</c> does not move the other
        /// patches.
        /// </remarks>
        private static void PatchHeap(byte[] block, GeneratorCall call)
        {
            int map = BinaryPrimitives.ReadUInt16LittleEndian(block);
            if (call.Get("hnsig") is string signature)
                block[2] = (byte)GeneratorCall.ParseInteger(signature);

            if (call.Get("userroot") is string userRoot)
                BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), (uint)GeneratorCall.ParseInteger(userRoot));

            if (call.Get("mapcount") is string count)
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(map), (ushort)GeneratorCall.ParseInteger(count));

            if (call.Get("mapfree") is string free)
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(map + 2), (ushort)GeneratorCall.ParseInteger(free));

            foreach (string entry in call.GetAll("mapoffset"))
            {
                (long i, long value) = ParsePair(entry, "mapoffset");
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(map + 4 + ((int)i * 2)), (ushort)value);
            }

            if (call.Get("hnpm") is string pageMap)
                BinaryPrimitives.WriteUInt16LittleEndian(block, (ushort)GeneratorCall.ParseInteger(pageMap));

            foreach (string patch in call.GetAll("heappatch"))
            {
                int colon = patch.IndexOf(':', StringComparison.Ordinal);
                if (colon < 0) throw new FormatException($"gen: heappatch '{patch}' is not offset:hex.");

                GeneratorCall.ParseHex(patch[(colon + 1)..]).CopyTo(block, (int)GeneratorCall.ParseInteger(patch[..colon]));
            }
        }

        /// <summary>
        /// Reads a <c>first:second</c> pair of integers.
        /// </summary>
        /// <param name="text">The pair.</param>
        /// <param name="key">The key that gave it, for error messages.</param>
        /// <returns>The two integers.</returns>
        /// <exception cref="FormatException">The text is not two integers separated by <c>:</c>.</exception>
        private static (long First, long Second) ParsePair(string text, string key)
        {
            int colon = text.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0) throw new FormatException($"gen: {key} '{text}' is not two integers separated by ':'.");

            return (GeneratorCall.ParseInteger(text[..colon]), GeneratorCall.ParseInteger(text[(colon + 1)..]));
        }

        /// <summary>
        /// Chooses the columns of a <c>Table</c> row of a given width: the row identifier, then filler columns, widest
        /// first, that fill the row once the existence bitmap, one bit per column, takes its share.
        /// </summary>
        /// <param name="width">The row width.</param>
        /// <returns>The columns: tag, cell offset and cell width.</returns>
        /// <exception cref="FormatException">No set of columns fills the width.</exception>
        private static List<(uint Tag, int Offset, int Width)> FillerColumns(int width)
        {
            // The bitmap grows with the number of columns, so try each bitmap size until the columns that fill the
            // rest need exactly that many bitmap bytes.
            List<int> widths = [];
            for (int bitmap = 1; ; bitmap++)
            {
                if (bitmap > 64) throw new FormatException("gen: Table: no column layout fills the width.");

                widths = [];
                int remaining = width - bitmap - 4;
                foreach (int cell in new[] { 8, 4, 2, 1 })
                {
                    while (remaining >= cell)
                    {
                        widths.Add(cell);
                        remaining -= cell;
                    }
                }

                if ((widths.Count + 1 + 7) / 8 == bitmap)
                    break;
            }

            var columns = new List<(uint Tag, int Offset, int Width)> { (0x67F20003, 0, 4) };
            int offset = 4;
            ushort id = 0x6000;
            foreach (int cell in widths)
            {
                ushort type = cell switch { 8 => 0x0014, 4 => 0x0003, 2 => 0x0002, _ => 0x000B };
                columns.Add((((uint)id++ << 16) | type, offset, cell));
                offset += cell;
            }

            return columns;
        }

        /// <summary>
        /// Lays out a property-context BTH leaf item: per record a 2-byte key and its 6 bytes of data.
        /// </summary>
        /// <param name="records">The records in key order.</param>
        /// <returns>The item bytes.</returns>
        private static byte[] LeafItem(List<(ulong Key, byte[] Data)> records)
        {
            var leaf = new byte[records.Count * 8];
            for (int i = 0; i < records.Count; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(leaf.AsSpan(i * 8), (ushort)records[i].Key);
                records[i].Data.CopyTo(leaf, (i * 8) + 2);
            }

            return leaf;
        }

        /// <summary>
        /// Stores a property value and returns the reference its record holds.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <param name="heap">The node's heap.</param>
        /// <param name="property">The property.</param>
        /// <param name="subnodes">The node's subnodes, to which a subnode-resident value is added.</param>
        /// <returns>The inline value, a heap identifier, a subnode identifier, zero or the raw reference.</returns>
        private static uint Reference(PstFixtureBuilder file, HeapWriter heap, PropertyModel property, List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes)
        {
            if (property.IsInline)
                return property.Inline;

            switch (property.Placement)
            {
                case ValuePlacement.Null:
                    return 0;

                case ValuePlacement.Raw:
                    return property.RawReference;

                case ValuePlacement.Heap:
                case ValuePlacement.Auto when property.Data.Length <= InputGenerator.MaxHeapItem:
                    return heap.AddItem(property.Data);

                default:
                    uint nodeId = ((0x800u + (uint)subnodes.Count) << 5) | 0x1F;
                    subnodes.Add((nodeId, InputGenerator.WriteData(file, property.Data), 0));
                    return nodeId;
            }
        }
    }
}
