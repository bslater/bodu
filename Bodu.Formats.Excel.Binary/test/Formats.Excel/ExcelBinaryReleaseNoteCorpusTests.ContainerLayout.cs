// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.ContainerLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the parts of a compound file a container patch resolves against: its sector size, its FAT sectors and
    /// its directory entries, read from the file's own header, DIFAT and directory chain.
    /// </summary>
    private sealed class ContainerLayout
    {
        /// <summary>The FAT value of a free sector (FREESECT).</summary>
        public const uint FreeSector = 0xFFFFFFFF;

        /// <summary>The FAT value of a DIFAT sector (DIFSECT).</summary>
        public const uint DifatSectorMarker = 0xFFFFFFFC;

        /// <summary>The FAT value that ends a chain (ENDOFCHAIN).</summary>
        private const uint EndOfChain = 0xFFFFFFFE;

        /// <summary>The FAT value of a FAT sector (FATSECT).</summary>
        private const uint FatSectorMarker = 0xFFFFFFFD;

        /// <summary>The first special sector value; every sector number below it names a sector.</summary>
        private const uint MaximumRegularSector = 0xFFFFFFFA;

        /// <summary>The number of FAT sector numbers the header itself holds.</summary>
        private const int HeaderDifatSlots = 109;

        /// <summary>The file's bytes, which the write methods change in place.</summary>
        private readonly byte[] _file;

        /// <summary>The FAT sectors, in FAT order.</summary>
        private readonly List<uint> _fatSectors = [];

        /// <summary>The file offset of each directory entry, in entry order.</summary>
        private readonly List<int> _entryOffsets = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerLayout" /> class.
        /// </summary>
        /// <param name="file">The compound file.</param>
        public ContainerLayout(byte[] file)
        {
            _file = file;
            SectorSize = 1 << BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(0x1E));
            FirstDirectorySector = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x30));

            var fatCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x2C));
            for (var i = 0; i < Math.Min(fatCount, HeaderDifatSlots); i++)
                _fatSectors.Add(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x4C + (i * 4))));

            uint difat = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x44));
            while (_fatSectors.Count < fatCount && difat < MaximumRegularSector)
            {
                int at = GetSectorOffset(difat);
                for (var i = 0; i < (SectorSize / 4) - 1 && _fatSectors.Count < fatCount; i++)
                    _fatSectors.Add(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(at + (i * 4))));

                difat = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(at + SectorSize - 4));
            }

            foreach (uint sector in GetChain(FirstDirectorySector))
            {
                for (var i = 0; i < SectorSize / 128; i++)
                    _entryOffsets.Add(GetSectorOffset(sector) + (i * 128));
            }
        }

        /// <summary>
        /// Gets the file's sector size.
        /// </summary>
        /// <value>The sector size in bytes, from the header's sector shift.</value>
        public int SectorSize { get; }

        /// <summary>
        /// Gets the first sector of the directory chain.
        /// </summary>
        /// <value>The sector number the header names.</value>
        public uint FirstDirectorySector { get; }

        /// <summary>
        /// Gets the DIFAT sectors, in chain order.
        /// </summary>
        /// <returns>
        /// The sectors from the header's first DIFAT sector, stopping at a special value or a repeated sector.
        /// </returns>
        public List<uint> GetDifatSectors()
        {
            var sectors = new List<uint>();
            uint sector = BinaryPrimitives.ReadUInt32LittleEndian(_file.AsSpan(0x44));
            while (sector < MaximumRegularSector && !sectors.Contains(sector))
            {
                sectors.Add(sector);
                sector = BinaryPrimitives.ReadUInt32LittleEndian(_file.AsSpan(GetSectorOffset(sector) + SectorSize - 4));
            }

            return sectors;
        }

        /// <summary>
        /// Gets one DIFAT sector.
        /// </summary>
        /// <param name="index">
        /// The sector's position in the DIFAT chain, from zero; a negative position counts from the end.
        /// </param>
        /// <returns>The sector number.</returns>
        /// <exception cref="FormatException">The DIFAT chain has no such sector.</exception>
        public uint GetDifatSector(string index)
        {
            List<uint> sectors = GetDifatSectors();
            int k = RecordBuilder.ParseInt32(index);
            if (k < 0)
                k += sectors.Count;

            return k >= 0 && k < sectors.Count ? sectors[k] : throw new FormatException($"The DIFAT chain has no sector {index}.");
        }

        /// <summary>
        /// Gets the file offset of a sector.
        /// </summary>
        /// <param name="sector">The sector number.</param>
        /// <returns>The offset of the sector's first byte; the header occupies the place of sector -1.</returns>
        public int GetSectorOffset(uint sector) =>
            checked((int)((sector + 1L) * SectorSize));

        /// <summary>
        /// Reads a sector's FAT entry.
        /// </summary>
        /// <param name="sector">The sector number.</param>
        /// <returns>The next sector of its chain, or a special value.</returns>
        public uint ReadFat(uint sector) =>
            BinaryPrimitives.ReadUInt32LittleEndian(_file.AsSpan(GetFatEntryOffset(sector)));

        /// <summary>
        /// Writes a sector's FAT entry.
        /// </summary>
        /// <param name="sector">The sector number.</param>
        /// <param name="value">The next sector of its chain, or a special value.</param>
        public void WriteFat(uint sector, uint value) =>
            BinaryPrimitives.WriteUInt32LittleEndian(_file.AsSpan(GetFatEntryOffset(sector)), value);

        /// <summary>
        /// Follows a chain through the FAT.
        /// </summary>
        /// <param name="start">The first sector.</param>
        /// <returns>The chain's sectors, stopping at a special value or a repeated sector.</returns>
        public List<uint> GetChain(uint start)
        {
            var chain = new List<uint>();
            var seen = new HashSet<uint>();
            for (uint sector = start; sector < MaximumRegularSector && seen.Add(sector); sector = ReadFat(sector))
                chain.Add(sector);

            return chain;
        }

        /// <summary>
        /// Gets the file offset of a directory entry.
        /// </summary>
        /// <param name="entry">The entry index.</param>
        /// <returns>The offset of the entry's first byte.</returns>
        public int GetEntryOffset(int entry) =>
            _entryOffsets[entry];

        /// <summary>
        /// Gets a directory entry's start sector.
        /// </summary>
        /// <param name="entry">The entry index.</param>
        /// <returns>The sector number the entry names.</returns>
        public uint GetStartSector(int entry) =>
            BinaryPrimitives.ReadUInt32LittleEndian(_file.AsSpan(_entryOffsets[entry] + 116));

        /// <summary>
        /// Finds a directory entry.
        /// </summary>
        /// <param name="name">
        /// <c>root</c> for entry 0, <c>#N</c> for the entry at index <c>N</c> (used or free), or an entry name.
        /// </param>
        /// <returns>The entry index.</returns>
        /// <exception cref="FormatException">No entry matches.</exception>
        public int FindEntry(string name)
        {
            if (name == "root")
                return 0;

            if (name.StartsWith('#'))
            {
                int index = RecordBuilder.ParseInt32(name[1..]);
                return index >= 0 && index < _entryOffsets.Count ? index : throw new FormatException($"The directory has no entry {name}.");
            }

            for (var i = 0; i < _entryOffsets.Count; i++)
            {
                int at = _entryOffsets[i];
                int length = BinaryPrimitives.ReadUInt16LittleEndian(_file.AsSpan(at + 64));
                if (length >= 2 && length <= 64 && Encoding.Unicode.GetString(_file, at, length - 2) == name)
                    return i;
            }

            throw new FormatException($"The directory has no entry named '{name}'.");
        }

        /// <summary>
        /// Resolves a sector value.
        /// </summary>
        /// <param name="text">
        /// A number, <c>end</c>, <c>free</c>, <c>fatsect</c>, <c>difsect</c>, or <c>@NAME</c> for the named entry's
        /// start sector.
        /// </param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is malformed, or names no entry.</exception>
        public uint ResolveValue(string text) =>
            text switch
            {
                "end" => EndOfChain,
                "free" => FreeSector,
                "fatsect" => FatSectorMarker,
                "difsect" => DifatSectorMarker,
                _ when text.StartsWith('@') => GetStartSector(FindEntry(text[1..])),
                _ => (uint)RecordBuilder.ParseInt64(text),
            };

        /// <summary>
        /// Gets the file offset of a sector's FAT entry.
        /// </summary>
        /// <param name="sector">The sector number.</param>
        /// <returns>The offset of the four-byte entry in its FAT sector.</returns>
        /// <exception cref="FormatException">The FAT does not reach <paramref name="sector" />.</exception>
        private int GetFatEntryOffset(uint sector)
        {
            int perSector = SectorSize / 4;
            long fatIndex = sector / perSector;
            if (fatIndex >= _fatSectors.Count)
                throw new FormatException($"The FAT does not reach sector {sector}.");

            return GetSectorOffset(_fatSectors[(int)fatIndex]) + (int)(sector % perSector * 4);
        }
    }
}
