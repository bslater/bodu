// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.InputGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Runs <c>gen:</c> pipelines: the container generators <c>Ndb</c>, <c>DataTree</c> and <c>Subnodes</c>, the
    /// transforms <c>Header</c> and <c>BTree</c>, and the dispatch to <see cref="ContextGenerator" /> and
    /// <see cref="MailGenerator" />.
    /// </summary>
    /// <remarks>
    /// Every generator writes through <see cref="PstFixtureBuilder" />, whose documentation defines the layout: block
    /// identifiers from 4 in steps of 4 in write order, with bit 0x2 set on internal blocks; external blocks encoded
    /// with the file's crypt method; and B-trees written as leaves of up to <c>pagesize</c> entries, with one branch
    /// page over them when there is more than one leaf. Every generator takes <c>format</c>, <c>crypt</c>,
    /// <c>pagesize</c> and <c>version</c>.
    /// </remarks>
    private static class InputGenerator
    {
        /// <summary>The largest heap item, in bytes; a longer value is stored in a subnode (MS-PST 2.3.3.3).</summary>
        public const int MaxHeapItem = 3580;

        /// <summary>The MS-PST 5.3 CRC table for the reflected polynomial 0xEDB88320, one entry per byte.</summary>
        private static readonly uint[] s_crcTable = BuildCrcTable();

        /// <summary>
        /// Builds the bytes a catalog input describes.
        /// </summary>
        /// <param name="input">The decoded input: <c>gen:</c> followed by a generator pipeline.</param>
        /// <returns>The file bytes.</returns>
        /// <exception cref="FormatException">The input is not a well-formed pipeline of known generators.</exception>
        public static byte[] Generate(string input)
        {
            if (!input.StartsWith("gen:", StringComparison.Ordinal)) throw new FormatException("An input is gen: followed by a generator pipeline.");

            IReadOnlyList<GeneratorCall> calls = GeneratorCall.ParsePipeline(input[4..]);
            GeneratedFile file = RunGenerator(calls[0]);
            foreach (GeneratorCall call in calls.Skip(1))
                RunTransform(file, call);

            return file.Bytes;
        }

        /// <summary>
        /// Creates a builder with the knobs every generator takes: <c>format</c> (<c>Unicode</c>, the default, or
        /// <c>Ansi</c>), <c>crypt</c> (<c>None</c>, the default, <c>Permute</c> or <c>Cyclic</c>), <c>pagesize</c> (the
        /// largest number of entries on one B-tree page) and <c>version</c> (the header's <c>wVer</c>).
        /// </summary>
        /// <param name="call">The generator call.</param>
        /// <returns>The builder.</returns>
        /// <exception cref="FormatException">A knob's value is not one it takes.</exception>
        public static PstFixtureBuilder NewBuilder(GeneratorCall call)
        {
            var file = new PstFixtureBuilder
            {
                Format = call.GetString("format", "Unicode") switch
                {
                    "Unicode" => PstFileFormat.Unicode,
                    "Ansi" => PstFileFormat.Ansi,
                    string other => throw new FormatException($"gen: {call.Name}: format is Unicode or Ansi, not '{other}'."),
                },
                CryptMethod = call.GetString("crypt", "None") switch
                {
                    "None" => PstCryptMethod.None,
                    "Permute" => PstCryptMethod.Permute,
                    "Cyclic" => PstCryptMethod.Cyclic,
                    string other => throw new FormatException($"gen: {call.Name}: crypt is None, Permute or Cyclic, not '{other}'."),
                },
                MaxEntriesPerPage = call.GetInt32("pagesize", int.MaxValue),
            };

            if (call.Get("version") is string version)
                file.Version = (ushort)GeneratorCall.ParseInteger(version);

            return file;
        }

        /// <summary>
        /// Gets the largest payload of one block in a format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>8176 bytes in the Unicode format and 8180 in the ANSI format.</returns>
        public static int BlockPayload(PstFileFormat format) =>
            format == PstFileFormat.Ansi ? 8180 : 8176;

        /// <summary>
        /// Writes a payload as one data block, or as a data tree when it exceeds one block.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <param name="data">The payload.</param>
        /// <returns>The identifier of the data block or of the tree's root block.</returns>
        public static ulong WriteData(PstFixtureBuilder file, byte[] data) =>
            WriteSegments(file, [.. Chunk(data, BlockPayload(file.Format))]);

        /// <summary>
        /// Writes segments as data blocks: one block alone, several under an XBLOCK, or under XBLOCKs under an XXBLOCK
        /// when one XBLOCK cannot name them all.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <param name="segments">The segments, each at most one block's payload.</param>
        /// <returns>The identifier of the data block or of the tree's root block.</returns>
        public static ulong WriteSegments(PstFixtureBuilder file, IReadOnlyList<byte[]> segments)
        {
            if (segments.Count == 1)
                return file.AddDataBlock(segments[0]);

            ulong[] ids = [.. segments.Select(file.AddDataBlock)];
            long total = segments.Sum(segment => (long)segment.Length);
            int perXBlock = (BlockPayload(file.Format) - 8) / IdWidth(file.Format);
            if (ids.Length <= perXBlock)
                return file.AddXBlock((uint)total, ids);

            var xblocks = new List<ulong>();
            for (int start = 0; start < ids.Length; start += perXBlock)
            {
                int count = Math.Min(perXBlock, ids.Length - start);
                long length = segments.Skip(start).Take(count).Sum(segment => (long)segment.Length);
                xblocks.Add(file.AddXBlock((uint)length, ids[start..(start + count)]));
            }

            return file.AddXXBlock((uint)total, [.. xblocks]);
        }

        /// <summary>
        /// Writes the heap blocks a <see cref="HeapWriter" /> built: one data block, or several under an XBLOCK.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <param name="blocks">The heap blocks.</param>
        /// <returns>The identifier of the data block or of the XBLOCK.</returns>
        public static ulong WriteHeapBlocks(PstFixtureBuilder file, List<byte[]> blocks) =>
            blocks.Count == 1
                ? file.AddDataBlock(blocks[0])
                : file.AddXBlock((uint)blocks.Sum(block => block.Length), [.. blocks.Select(file.AddDataBlock)]);

        /// <summary>
        /// Creates the fill pattern.
        /// </summary>
        /// <param name="length">The number of bytes.</param>
        /// <returns><paramref name="length" /> bytes, byte <c>i</c> being <c>i mod 251</c>.</returns>
        public static byte[] Fill(int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
                bytes[i] = (byte)(i % 251);

            return bytes;
        }

        /// <summary>
        /// Computes the MS-PST 5.3 CRC: the reflected CRC-32 with an initial value of zero and no final complement.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The CRC.</returns>
        public static uint Crc32(ReadOnlySpan<byte> data)
        {
            uint crc = 0;
            foreach (byte b in data)
                crc = s_crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);

            return crc;
        }

        /// <summary>
        /// Runs a pipeline's first call, which must be a generator.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file the generator wrote.</returns>
        /// <exception cref="FormatException">The call is not a generator, or its arguments are malformed.</exception>
        private static GeneratedFile RunGenerator(GeneratorCall call)
        {
            GeneratedFile file = call.Name switch
            {
                "Ndb" => Ndb(call),
                "DataTree" => DataTree(call),
                "Subnodes" => Subnodes(call),
                "Pc" => ContextGenerator.PropertyContext(call),
                "Table" => ContextGenerator.TableContext(call),
                "Mail" => MailGenerator.Build(call),
                _ => throw new FormatException($"gen: '{call.Name}' is not a generator."),
            };

            call.EnsureAllRead();
            return file;
        }

        /// <summary>
        /// Runs one of a pipeline's later calls, which must be a transform.
        /// </summary>
        /// <param name="file">The file the earlier calls wrote, patched in place.</param>
        /// <param name="call">The call.</param>
        /// <exception cref="FormatException">The call is not a transform, or its arguments are malformed.</exception>
        private static void RunTransform(GeneratedFile file, GeneratorCall call)
        {
            switch (call.Name)
            {
                case "Header":
                    PatchHeader(file, call);
                    break;

                case "BTree":
                    PatchBTree(file, call);
                    break;

                default:
                    throw new FormatException($"gen: '{call.Name}' is not a transform.");
            }

            call.EnsureAllRead();
        }

        /// <summary>
        /// Builds the file a builder holds.
        /// </summary>
        /// <param name="file">The builder.</param>
        /// <returns>The file.</returns>
        private static GeneratedFile Finish(PstFixtureBuilder file) =>
            new(file.Build(), file.Format);

        /// <summary>
        /// Gets the identifier <c>Ndb</c> gives its i-th node.
        /// </summary>
        /// <param name="i">The node's zero-based position.</param>
        /// <returns>
        /// 0x21, 0x61 and 0x122 for the first three, then <c>((0x401 + i - 3) &lt;&lt; 5) | 0x02</c>.
        /// </returns>
        private static uint NdbNodeId(int i) =>
            i switch
            {
                0 => 0x21,
                1 => 0x61,
                2 => 0x122,
                _ => ((0x401u + (uint)(i - 3)) << 5) | 0x02,
            };

        /// <summary>
        /// Runs <c>Ndb(nodes=n, data=hex)</c>: n nodes (default 1), each with its own data block holding <c>data</c>
        /// (default <c>010203</c>), no subnodes and parent 0.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        private static GeneratedFile Ndb(GeneratorCall call)
        {
            PstFixtureBuilder file = NewBuilder(call);
            int nodes = call.GetInt32("nodes", 1);
            byte[] data = GeneratorCall.ParseHex(call.GetString("data", "010203"));
            for (int i = 0; i < nodes; i++)
                file.AddNode(NdbNodeId(i), file.AddDataBlock(data), 0, 0);

            return Finish(file);
        }

        /// <summary>
        /// Runs <c>DataTree(blocks=n, size=s, level=1|2, per=m, childlevel=l, count=c, total=t, selfref=true,
        /// internalleaf=true, rawroot=hex)</c>: node 0x21 whose data is n data blocks (default 2) under an XBLOCK, or
        /// under an XXBLOCK over XBLOCKs of m blocks each.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        /// <remarks>
        /// Block k holds s bytes (default 16), byte j being <c>(16k + j) mod 256</c>. At level 2 each XBLOCK declares
        /// cLevel <c>childlevel</c> (default 1). <c>count</c> replaces the root's cEnt and <c>total</c> its lcbTotal;
        /// <c>selfref</c> appends the root's own identifier as a last child and <c>internalleaf</c> an XBLOCK over
        /// block 0; <c>rawroot</c> writes the root block as the given bytes instead.
        /// </remarks>
        private static GeneratedFile DataTree(GeneratorCall call)
        {
            PstFixtureBuilder file = NewBuilder(call);
            int blocks = call.GetInt32("blocks", 2);
            int size = call.GetInt32("size", 16);
            int level = call.GetInt32("level", 1);
            int per = call.GetInt32("per", 2);
            var childLevel = (byte)call.GetInt32("childlevel", 1);
            string? count = call.Get("count");
            string? total = call.Get("total");
            bool selfReference = call.GetBoolean("selfref", false);
            bool internalLeaf = call.GetBoolean("internalleaf", false);
            string? rawRoot = call.Get("rawroot");
            if (level is not (1 or 2)) throw new FormatException("gen: DataTree: level is 1 or 2.");

            var ids = new List<ulong>();
            for (int k = 0; k < blocks; k++)
            {
                var payload = new byte[size];
                for (int j = 0; j < size; j++)
                    payload[j] = (byte)((k * 16) + j);

                ids.Add(file.AddDataBlock(payload));
            }

            long length = (long)blocks * size;
            ulong root;
            if (rawRoot is not null)
            {
                root = file.AddRawBlock(GeneratorCall.ParseHex(rawRoot), isInternal: true);
            }
            else if (level == 1)
            {
                List<ulong> children = [.. ids];

                // The builder assigns identifiers in steps of four, so the next block it writes, the internal root,
                // receives this identifier.
                if (selfReference)
                    children.Add((ids.Max() + 4) | 0x2);

                if (internalLeaf)
                    children.Add(file.AddRawBlock(DataTreeBlock(file.Format, 1, [ids[0]], size, null, null), isInternal: true));

                root = file.AddRawBlock(DataTreeBlock(file.Format, 1, children, length, count, total), isInternal: true);
            }
            else
            {
                var xblocks = new List<ulong>();
                for (int start = 0; start < ids.Count; start += per)
                {
                    List<ulong> part = [.. ids.Skip(start).Take(per)];
                    xblocks.Add(file.AddRawBlock(DataTreeBlock(file.Format, childLevel, part, part.Count * (long)size, null, null), isInternal: true));
                }

                root = file.AddRawBlock(DataTreeBlock(file.Format, 2, xblocks, length, count, total), isInternal: true);
            }

            file.AddNode(0x21, root, 0, 0);
            return Finish(file);
        }

        /// <summary>
        /// Lays out an XBLOCK or XXBLOCK: btype 0x01, cLevel, cEnt, lcbTotal, then the child identifiers.
        /// </summary>
        /// <param name="format">The format, which fixes the identifier width.</param>
        /// <param name="level">The cLevel.</param>
        /// <param name="children">The child block identifiers.</param>
        /// <param name="length">The lcbTotal.</param>
        /// <param name="count">The cEnt to write instead of the child count, or <see langword="null" />.</param>
        /// <param name="total">
        /// The lcbTotal to write instead of <paramref name="length" />, or <see langword="null" />.
        /// </param>
        /// <returns>The block payload.</returns>
        private static byte[] DataTreeBlock(PstFileFormat format, byte level, IReadOnlyList<ulong> children, long length, string? count, string? total)
        {
            int idWidth = IdWidth(format);
            var block = new byte[8 + (children.Count * idWidth)];
            block[0] = 0x01;
            block[1] = level;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), count is null ? (ushort)children.Count : (ushort)GeneratorCall.ParseInteger(count));
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), total is null ? (uint)length : (uint)GeneratorCall.ParseInteger(total));
            for (int i = 0; i < children.Count; i++)
                WriteId(block, 8 + (i * idWidth), children[i], idWidth);

            return block;
        }

        /// <summary>
        /// Runs <c>Subnodes(entries=n, index=true, count=c, nidhigh=h, data=hex, slraw=hex)</c>: node 0x21 (data
        /// <c>010203</c> or <c>data</c>) with n subnodes (default 2), subnode i being <c>((0x400 + i) &lt;&lt; 5) |
        /// 0x1F</c> with a data block holding the byte i.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        /// <remarks>
        /// The subnodes are one SLBLOCK or, with <c>index</c>, one SLBLOCK per entry under an SIBLOCK. <c>count</c>
        /// replaces the SLBLOCK's cEnt, <c>nidhigh</c> sets the upper 32 bits of every Unicode SLENTRY identifier, and
        /// <c>slraw</c> writes the subnode tree block as the given bytes.
        /// </remarks>
        private static GeneratedFile Subnodes(GeneratorCall call)
        {
            PstFixtureBuilder file = NewBuilder(call);
            int entries = call.GetInt32("entries", 2);
            bool index = call.GetBoolean("index", false);
            uint high = call.GetUInt32("nidhigh", 0);
            string? count = call.Get("count");
            string? raw = call.Get("slraw");
            byte[] data = GeneratorCall.ParseHex(call.GetString("data", "010203"));

            var rows = new List<(uint NodeId, ulong DataBlockId)>();
            for (int i = 0; i < entries; i++)
                rows.Add((((0x400u + (uint)i) << 5) | 0x1F, file.AddDataBlock([(byte)i])));

            ulong Leaf(IReadOnlyList<(uint NodeId, ulong DataBlockId)> part)
            {
                int idWidth = IdWidth(file.Format);
                var block = new byte[idWidth + (part.Count * idWidth * 3)];
                block[0] = 0x02;
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), count is null ? (ushort)part.Count : (ushort)GeneratorCall.ParseInteger(count));
                for (int i = 0; i < part.Count; i++)
                {
                    int at = idWidth + (i * idWidth * 3);
                    WriteId(block, at, ((ulong)high << 32) | part[i].NodeId, idWidth);
                    WriteId(block, at + idWidth, part[i].DataBlockId, idWidth);
                }

                return file.AddRawBlock(block, isInternal: true);
            }

            ulong tree = raw is not null
                ? file.AddRawBlock(GeneratorCall.ParseHex(raw), isInternal: true)
                : index
                    ? file.AddSubnodeIndexBlock([.. rows.Select(row => Leaf([row]))])
                    : Leaf(rows);
            file.AddNode(0x21, file.AddDataBlock(data), tree, 0);
            return Finish(file);
        }

        /// <summary>
        /// Runs <c>Header(magic, client, version, sentinel, crypt, eof, nbtoffset, bbtoffset, crc)</c>: patches header
        /// fields, then recomputes dwCRCPartial (<c>crc=repair</c>, the default), recomputes and inverts it
        /// (<c>bad</c>), or leaves it (<c>keep</c>).
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="call">The call.</param>
        /// <remarks>
        /// The fields are dwMagic (offset 0), wMagicClient (8), wVer (10), bSentinel (512; 460 in ANSI), bCryptMethod
        /// (513; 461), ibFileEof (184, 8 bytes; 168, 4 bytes) and the offsets of the node and block B-tree roots (224
        /// and 240; 188 and 196).
        /// </remarks>
        private static void PatchHeader(GeneratedFile file, GeneratorCall call)
        {
            byte[] b = file.Bytes;
            bool ansi = file.Format == PstFileFormat.Ansi;
            int idWidth = IdWidth(file.Format);
            if (call.Get("magic") is string magic)
                BinaryPrimitives.WriteUInt32LittleEndian(b, (uint)GeneratorCall.ParseInteger(magic));

            if (call.Get("client") is string client)
                BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(8), (ushort)GeneratorCall.ParseInteger(client));

            if (call.Get("version") is string version)
                BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(10), (ushort)GeneratorCall.ParseInteger(version));

            if (call.Get("sentinel") is string sentinel)
                b[ansi ? 460 : 512] = (byte)GeneratorCall.ParseInteger(sentinel);

            if (call.Get("crypt") is string crypt)
                b[ansi ? 461 : 513] = (byte)GeneratorCall.ParseInteger(crypt);

            if (call.Get("eof") is string eof)
                WriteId(b, ansi ? 168 : 184, (ulong)GeneratorCall.ParseInteger(eof), idWidth);

            if (call.Get("nbtoffset") is string nodeTreeOffset)
                WriteId(b, NodeTreeRootField(file.Format) + idWidth, (ulong)GeneratorCall.ParseInteger(nodeTreeOffset), idWidth);

            if (call.Get("bbtoffset") is string blockTreeOffset)
                WriteId(b, BlockTreeRootField(file.Format) + idWidth, (ulong)GeneratorCall.ParseInteger(blockTreeOffset), idWidth);

            switch (call.GetString("crc", "repair"))
            {
                case "repair":
                    PstFixtureBuilder.RepairHeaderChecksum(b);
                    break;

                case "bad":
                    PstFixtureBuilder.RepairHeaderChecksum(b);
                    BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4), ~BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4)));
                    break;

                case "keep":
                    break;

                default:
                    throw new FormatException("gen: Header: crc is repair, bad or keep.");
            }
        }

        /// <summary>
        /// Runs <c>BTree(tree=nbt|bbt, page=root|leaf, leaf=i, entry=e, count=n, level=l, type=t,
        /// child=self|sibling|offset, btkey=k, nidhigh=h, bbtoffset=o, bbtlength=n, cbent=c, crc=repair|bad|keep)</c>:
        /// patches one page of the node (default) or block B-tree.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="call">The call.</param>
        /// <remarks>
        /// <para>
        /// The page is the root the header names (<c>page=root</c>, the default) or the i-th child of the root
        /// (<c>page=leaf</c>). <c>count</c>, <c>level</c> and <c>cbent</c> replace cEnt, cLevel and cbEnt (cbEnt after
        /// the entry is located with the stride written), and <c>type</c> replaces ptype and ptypeRepeat.
        /// </para>
        /// <para>
        /// For entry e (default 0, at e * cbEnt), <c>child</c> points its BREF at the root itself (<c>self</c>), copies
        /// the previous entry's BREF (<c>sibling</c>), or replaces its offset; <c>btkey</c> replaces its key,
        /// <c>nidhigh</c> the upper 32 bits of its first identifier, <c>bbtoffset</c> the identifier-width field after
        /// the first (a BBTENTRY's offset) and <c>bbtlength</c> the 2 bytes after the two identifiers (its cb). The
        /// page CRC is then recomputed over the page's first 496 bytes (500 in ANSI), recomputed and inverted, or kept.
        /// </para>
        /// </remarks>
        private static void PatchBTree(GeneratedFile file, GeneratorCall call)
        {
            byte[] b = file.Bytes;
            bool ansi = file.Format == PstFileFormat.Ansi;
            int idWidth = IdWidth(file.Format);
            int entryArea = ansi ? 496 : 488;
            int trailer = ansi ? 500 : 496;
            int crcOffset = ansi ? 508 : 500;
            int rootField = call.GetString("tree", "nbt") switch
            {
                "nbt" => NodeTreeRootField(file.Format),
                "bbt" => BlockTreeRootField(file.Format),
                string other => throw new FormatException($"gen: BTree: tree is nbt or bbt, not '{other}'."),
            };

            ulong rootBlockId = ReadId(b, rootField, idWidth);
            var rootOffset = (int)ReadId(b, rootField + idWidth, idWidth);
            int page = call.GetString("page", "root") switch
            {
                "root" => rootOffset,
                "leaf" => (int)ReadId(b, rootOffset + (call.GetInt32("leaf", 0) * b[rootOffset + entryArea + 2]) + (2 * idWidth), idWidth),
                string other => throw new FormatException($"gen: BTree: page is root or leaf, not '{other}'."),
            };

            int stride = b[page + entryArea + 2];
            if (call.Get("count") is string count)
                b[page + entryArea] = (byte)GeneratorCall.ParseInteger(count);

            if (call.Get("level") is string level)
                b[page + entryArea + 3] = (byte)GeneratorCall.ParseInteger(level);

            if (call.Get("type") is string type)
            {
                b[page + trailer] = (byte)GeneratorCall.ParseInteger(type);
                b[page + trailer + 1] = (byte)GeneratorCall.ParseInteger(type);
            }

            int at = page + (call.GetInt32("entry", 0) * stride);
            switch (call.Get("child"))
            {
                case null:
                    break;

                case "self":
                    WriteId(b, at + idWidth, rootBlockId, idWidth);
                    WriteId(b, at + (2 * idWidth), (ulong)rootOffset, idWidth);
                    break;

                case "sibling":
                    Array.Copy(b, at - stride + idWidth, b, at + idWidth, 2 * idWidth);
                    break;

                case string offset:
                    WriteId(b, at + (2 * idWidth), (ulong)GeneratorCall.ParseInteger(offset), idWidth);
                    break;
            }

            if (call.Get("btkey") is string key)
                WriteId(b, at, (ulong)GeneratorCall.ParseInteger(key), idWidth);

            if (call.Get("nidhigh") is string high)
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(at + 4), (uint)GeneratorCall.ParseInteger(high));

            if (call.Get("bbtoffset") is string blockOffset)
                WriteId(b, at + idWidth, (ulong)GeneratorCall.ParseInteger(blockOffset), idWidth);

            if (call.Get("bbtlength") is string blockLength)
                BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(at + (2 * idWidth)), (ushort)GeneratorCall.ParseInteger(blockLength));

            if (call.Get("cbent") is string entrySize)
                b[page + entryArea + 2] = (byte)GeneratorCall.ParseInteger(entrySize);

            switch (call.GetString("crc", "repair"))
            {
                case "repair":
                    BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(page + crcOffset), Crc32(b.AsSpan(page, trailer)));
                    break;

                case "bad":
                    BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(page + crcOffset), ~Crc32(b.AsSpan(page, trailer)));
                    break;

                case "keep":
                    break;

                default:
                    throw new FormatException("gen: BTree: crc is repair, bad or keep.");
            }
        }

        /// <summary>
        /// Gets the offset of the header's BREF to the node B-tree root.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// 216 in the Unicode format and 184 in the ANSI format; the root's offset follows the identifier.
        /// </returns>
        private static int NodeTreeRootField(PstFileFormat format) =>
            format == PstFileFormat.Ansi ? 184 : 216;

        /// <summary>
        /// Gets the offset of the header's BREF to the block B-tree root.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// 232 in the Unicode format and 192 in the ANSI format; the root's offset follows the identifier.
        /// </returns>
        private static int BlockTreeRootField(PstFileFormat format) =>
            format == PstFileFormat.Ansi ? 192 : 232;

        /// <summary>
        /// Gets the width of a block or node identifier in a format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>8 bytes in the Unicode format and 4 in the ANSI format.</returns>
        private static int IdWidth(PstFileFormat format) =>
            format == PstFileFormat.Ansi ? 4 : 8;

        /// <summary>
        /// Reads an identifier-width little-endian field.
        /// </summary>
        /// <param name="b">The bytes.</param>
        /// <param name="at">The field's offset.</param>
        /// <param name="width">The field's width: 8 or 4 bytes.</param>
        /// <returns>The value.</returns>
        private static ulong ReadId(byte[] b, int at, int width) =>
            width == 8 ? BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(at)) : BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(at));

        /// <summary>
        /// Writes an identifier-width little-endian field.
        /// </summary>
        /// <param name="b">The bytes.</param>
        /// <param name="at">The field's offset.</param>
        /// <param name="value">The value; a 4-byte field keeps its low 32 bits.</param>
        /// <param name="width">The field's width: 8 or 4 bytes.</param>
        private static void WriteId(byte[] b, int at, ulong value, int width)
        {
            if (width == 8)
                BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(at), value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(at), (uint)value);
        }

        /// <summary>
        /// Splits a payload into segments of at most one block each.
        /// </summary>
        /// <param name="data">The payload.</param>
        /// <param name="size">The largest segment.</param>
        /// <returns>The segments; one empty segment for an empty payload.</returns>
        private static IEnumerable<byte[]> Chunk(byte[] data, int size)
        {
            if (data.Length == 0)
            {
                yield return [];
                yield break;
            }

            for (int start = 0; start < data.Length; start += size)
                yield return data[start..Math.Min(data.Length, start + size)];
        }

        /// <summary>
        /// Builds the CRC table for <see cref="Crc32" />.
        /// </summary>
        /// <returns>The 256 entries.</returns>
        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;

                table[n] = c;
            }

            return table;
        }
    }
}
