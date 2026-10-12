// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.Layout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Reads a well-formed container as Bodu's builder lays it out, to resolve the paths, sectors and offsets that the
    /// patch steps of a <c>gen:</c> pipeline name.
    /// </summary>
    /// <remarks>
    /// The reader is independent of Bodu.IO.Compound's own, so a defect in the reader under test cannot change where a
    /// patch lands. It follows the header's DIFAT slots and DIFAT sector chain (skipping values of <c>0xFFFFFFFA</c>
    /// and above), the FAT, the directory chain and the mini FAT chain, and resolves a path by walking each storage's
    /// red-black tree from its child link, comparing names ordinally.
    /// </remarks>
    private sealed class Layout
    {
        /// <summary>The first value of an allocation table that is a sentinel rather than a sector number.</summary>
        private const uint FirstSentinel = 0xFFFFFFFA;

        /// <summary>The value of a directory link that names no entry.</summary>
        private const uint NoStream = 0xFFFFFFFF;

        /// <summary>The smallest stream size held in regular sectors rather than in the mini stream.</summary>
        private const long MiniStreamCutoff = 4096;

        /// <summary>The size, in bytes, of a directory entry.</summary>
        private const int EntrySize = 128;

        /// <summary>The container bytes as built.</summary>
        private readonly byte[] _data;

        /// <summary>The FAT, one entry per sector the FAT sectors describe.</summary>
        private readonly uint[] _fat;

        /// <summary>The mini FAT, one entry per mini sector the mini FAT sectors describe.</summary>
        private readonly uint[] _miniFat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Layout" /> class over a container as built.
        /// </summary>
        /// <param name="data">The container bytes as built.</param>
        private Layout(byte[] data)
        {
            _data = data;
            SectorSize = 1 << BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0x1E));
            int perSector = SectorSize / 4;

            var fatSectors = new List<uint>();
            for (int i = 0; i < HeaderDifatSlots; i++)
            {
                uint id = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x4C + (4 * i)));
                if (id < FirstSentinel)
                    fatSectors.Add(id);
            }

            uint difat = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x44));
            while (difat < FirstSentinel)
            {
                long at = (difat + 1L) * SectorSize;
                for (int i = 0; i < perSector - 1; i++)
                {
                    uint id = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)(at + (4 * i))));
                    if (id < FirstSentinel)
                        fatSectors.Add(id);
                }

                difat = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)(at + SectorSize - 4)));
            }

            FatSectors = fatSectors;
            _fat = ReadTable(fatSectors);
            DirectorySectors = Walk(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x30)), _fat);
            MiniFatSectors = Walk(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x3C)), _fat);
            _miniFat = ReadTable(MiniFatSectors);
        }

        /// <summary>
        /// Gets the sector size, in bytes, from the header's sector shift.
        /// </summary>
        /// <value>512 for a version-3 container; 4096 for version 4.</value>
        public int SectorSize { get; }

        /// <summary>
        /// Gets the FAT sectors, in DIFAT order.
        /// </summary>
        /// <value>The sector numbers of the FAT.</value>
        public IReadOnlyList<uint> FatSectors { get; }

        /// <summary>
        /// Gets the sectors of the directory chain.
        /// </summary>
        /// <value>The sector numbers, in chain order.</value>
        public IReadOnlyList<uint> DirectorySectors { get; }

        /// <summary>
        /// Gets the sectors of the mini FAT chain.
        /// </summary>
        /// <value>The sector numbers, in chain order; none when the container has no mini FAT.</value>
        public IReadOnlyList<uint> MiniFatSectors { get; }

        /// <summary>Gets the length of the container as built.</summary>
        /// <value>The length, in bytes.</value>
        public long Length => _data.Length;

        /// <summary>
        /// Gets a value indicating whether the container has DIFAT sectors.
        /// </summary>
        /// <value><see langword="true" /> when the header's First DIFAT Sector Location names a sector.</value>
        public bool HasDifatSectors => BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(0x44)) < FirstSentinel;

        /// <summary>
        /// Gets the number of whole sectors after the header, which is the number the next appended sector takes.
        /// </summary>
        /// <value>The sector count.</value>
        public long SectorCount => (_data.Length / SectorSize) - 1;

        /// <summary>
        /// Reads a container as built.
        /// </summary>
        /// <param name="data">The container bytes.</param>
        /// <returns>The layout.</returns>
        public static Layout Parse(byte[] data) =>
            new(data);

        /// <summary>
        /// Selects an element of a sector list, counting from the end when the index is negative.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index; <c>-1</c> is the last element.</param>
        /// <returns>The element.</returns>
        /// <exception cref="FormatException">The index lies outside the list.</exception>
        public static long Index(IReadOnlyList<uint> list, int index)
        {
            int at = index < 0 ? list.Count + index : index;
            if (at < 0 || at >= list.Count)
                throw new FormatException($"The index {index} lies outside a list of {list.Count}.");

            return list[at];
        }

        /// <summary>
        /// Resolves an entry reference to its stream identifier.
        /// </summary>
        /// <param name="reference">
        /// A path such as <c>/S/A</c>, where <c>/</c> is the root, or <c>#&lt;sid&gt;</c>.
        /// </param>
        /// <returns>The stream identifier.</returns>
        /// <exception cref="FormatException">The reference names no entry.</exception>
        public int Resolve(string reference)
        {
            if (reference.StartsWith('#'))
                return (int)Number(reference[1..]);

            int sid = 0;
            foreach (string segment in SplitPath(reference))
            {
                int found = -1;
                var pending = new Stack<uint>();
                uint current = Field32(sid, 76);
                while (found < 0 && (current != NoStream || pending.Count > 0))
                {
                    while (current != NoStream)
                    {
                        pending.Push(current);
                        current = Field32((int)current, 68);
                    }

                    uint node = pending.Pop();
                    if (string.Equals(EntryName((int)node), segment, StringComparison.Ordinal))
                        found = (int)node;

                    current = Field32((int)node, 72);
                }

                if (found < 0)
                    throw new FormatException($"The reference '{reference}' names no entry '{segment}'.");

                sid = found;
            }

            return sid;
        }

        /// <summary>
        /// Gets the file offset of a directory entry.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <returns>The offset of the entry's 128-byte record.</returns>
        /// <exception cref="FormatException">The directory chain holds no such entry.</exception>
        public long EntryOffset(int sid)
        {
            int perSector = SectorSize / EntrySize;
            return ((Index(DirectorySectors, sid / perSector) + 1) * SectorSize) + ((sid % perSector) * (long)EntrySize);
        }

        /// <summary>
        /// Gets the file offset of the FAT entry for a sector.
        /// </summary>
        /// <param name="sector">The sector.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="FormatException">The FAT as built does not describe the sector.</exception>
        public long FatEntryOffset(long sector)
        {
            int perSector = SectorSize / 4;
            return ((Index(FatSectors, (int)(sector / perSector)) + 1) * SectorSize) + ((sector % perSector) * 4);
        }

        /// <summary>
        /// Gets the file offset of the mini FAT entry for a mini sector.
        /// </summary>
        /// <param name="miniSector">The mini sector.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="FormatException">The mini FAT as built does not describe the mini sector.</exception>
        public long MiniFatEntryOffset(long miniSector)
        {
            int perSector = SectorSize / 4;
            return ((Index(MiniFatSectors, (int)(miniSector / perSector)) + 1) * SectorSize) + ((miniSector % perSector) * 4);
        }

        /// <summary>
        /// Gets an entry's regular chain: the mini stream for the root, the payload of a stream of 4096 bytes or more.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <returns>The chain's sectors, in order.</returns>
        /// <exception cref="FormatException">The entry is a stream held in the mini stream.</exception>
        public IReadOnlyList<uint> Chain(int sid)
        {
            if (sid != 0 && Size(sid) < MiniStreamCutoff)
                throw new FormatException($"The entry #{sid} is held in the mini stream; name its sectors with minisector.");

            return Walk(Field32(sid, 116), _fat);
        }

        /// <summary>
        /// Gets a sector of an entry's regular chain.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <param name="index">The position in the chain; negative counts from the end.</param>
        /// <returns>The sector.</returns>
        /// <exception cref="FormatException">
        /// The entry has no regular chain, or the chain has no such position.
        /// </exception>
        public long ChainSector(int sid, int index) =>
            Index(Chain(sid), index);

        /// <summary>
        /// Gets a mini sector of a stream's mini chain.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <param name="index">The position in the chain; negative counts from the end.</param>
        /// <returns>The mini sector.</returns>
        /// <exception cref="FormatException">
        /// The entry is not a stream held in the mini stream, or the chain has no such position.
        /// </exception>
        public long MiniChainSector(int sid, int index)
        {
            if (sid == 0 || Size(sid) >= MiniStreamCutoff)
                throw new FormatException($"The entry #{sid} is not held in the mini stream; name its sectors with sector.");

            return Index(Walk(Field32(sid, 116), _miniFat), index);
        }

        /// <summary>
        /// Walks a chain through an allocation table.
        /// </summary>
        /// <param name="start">The first sector, or a sentinel for an empty chain.</param>
        /// <param name="table">The allocation table.</param>
        /// <returns>The chain's sectors, in order.</returns>
        /// <exception cref="FormatException">
        /// The chain leaves the table or is cyclic, which a container as built never is.
        /// </exception>
        private static List<uint> Walk(uint start, uint[] table)
        {
            var chain = new List<uint>();
            for (uint sector = start; sector < FirstSentinel; sector = table[sector])
            {
                if (sector >= table.Length || chain.Count > table.Length)
                    throw new FormatException("The container as built has a chain that leaves its table or is cyclic.");

                chain.Add(sector);
            }

            return chain;
        }

        /// <summary>
        /// Reads the allocation table the given sectors hold.
        /// </summary>
        /// <param name="sectors">The table's sectors, in order.</param>
        /// <returns>The table's entries.</returns>
        private uint[] ReadTable(IReadOnlyList<uint> sectors)
        {
            int perSector = SectorSize / 4;
            uint[] table = new uint[sectors.Count * perSector];
            for (int k = 0; k < sectors.Count; k++)
            {
                long at = (sectors[k] + 1L) * SectorSize;
                for (int i = 0; i < perSector; i++)
                    table[(k * perSector) + i] = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan((int)(at + (4 * i))));
            }

            return table;
        }

        /// <summary>
        /// Reads a 32-bit field of a directory entry.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <param name="offset">The field's offset within the entry.</param>
        /// <returns>The value.</returns>
        private uint Field32(int sid, int offset) =>
            BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan((int)(EntryOffset(sid) + offset)));

        /// <summary>
        /// Reads an entry's size field.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <returns>The size, in bytes.</returns>
        private long Size(int sid) =>
            (long)BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan((int)(EntryOffset(sid) + 120)));

        /// <summary>
        /// Reads an entry's name.
        /// </summary>
        /// <param name="sid">The stream identifier.</param>
        /// <returns>The name, without its terminator.</returns>
        private string EntryName(int sid)
        {
            long at = EntryOffset(sid);
            int length = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan((int)(at + 64)));
            return length < 2 ? string.Empty : Encoding.Unicode.GetString(_data, (int)at, length - 2);
        }
    }
}
