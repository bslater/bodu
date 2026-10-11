// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.CompoundImage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents a version 3 compound file (512-byte sectors) opened for in-place surgery, and applies the whole-file
    /// transforms of a <c>gen:Msg(...)</c> specification to it.
    /// </summary>
    /// <remarks>
    /// The transforms describe container layouts the writer never produces: a truncated file, a stream whose sectors are
    /// stored out of order, unallocated directory entries, and a mini FAT placed after every other sector. Each follows
    /// the DIFAT to read the FAT, and changes only the bytes <c>corpus/msg/README.md</c> names. A sector is appended only
    /// where the FAT already has an entry for it.
    /// </remarks>
    private sealed class CompoundImage
    {
        /// <summary>The FAT value of a free sector.</summary>
        private const uint FreeSector = 0xFFFFFFFF;

        /// <summary>The FAT value that ends a chain.</summary>
        private const uint EndOfChain = 0xFFFFFFFE;

        /// <summary>The directory value of an absent sibling or child.</summary>
        private const uint NoStream = 0xFFFFFFFF;

        /// <summary>The sector size of a version 3 compound file.</summary>
        private const int SectorSize = 512;

        /// <summary>The size of a directory entry.</summary>
        private const int EntrySize = 128;

        /// <summary>The number of FAT sector locations the header holds; the rest are in DIFAT sectors.</summary>
        private const int HeaderDifatEntries = 109;

        /// <summary>The offset of the object type in a directory entry.</summary>
        private const int ObjectTypeOffset = 66;

        /// <summary>The offsets of the left-sibling, right-sibling and child fields in a directory entry.</summary>
        private static readonly int[] s_referenceOffsets = [68, 72, 76];

        /// <summary>The locations of the FAT sectors, in order.</summary>
        private readonly List<uint> _fatSectors = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundImage" /> class.
        /// </summary>
        /// <param name="file">The compound file, changed in place.</param>
        /// <exception cref="FormatException">The bytes are not a version 3 compound file with 512-byte sectors.</exception>
        private CompoundImage(byte[] file)
        {
            Data = file;
            if (!file.AsSpan(0, 8).SequenceEqual((ReadOnlySpan<byte>)[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]))
                throw new FormatException("The bytes are not a compound file.");

            if (BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(0x1A)) != 3 || BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(0x1E)) != 9)
                throw new FormatException("Only version 3 compound files with 512-byte sectors are supported.");

            uint fatCount = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x2C));
            DirectoryStart = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x30));
            MiniStreamCutoff = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x38));
            MiniFatStart = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x3C));
            uint difat = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x44));
            for (int i = 0; i < Math.Min(HeaderDifatEntries, (int)fatCount); i++)
                _fatSectors.Add(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x4C + (4 * i))));

            while (_fatSectors.Count < fatCount && difat < EndOfChain)
            {
                for (int i = 0; i < (SectorSize / 4) - 1 && _fatSectors.Count < fatCount; i++)
                    _fatSectors.Add(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(Offset(difat) + (4 * i))));

                difat = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(Offset(difat) + (SectorSize - 4)));
            }

            Fat = new uint[_fatSectors.Count * (SectorSize / 4)];
            for (int f = 0; f < _fatSectors.Count; f++)
            {
                for (int i = 0; i < SectorSize / 4; i++)
                    Fat[(f * (SectorSize / 4)) + i] = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(Offset(_fatSectors[f]) + (4 * i)));
            }
        }

        /// <summary>
        /// Gets the file's bytes, which grow when a sector is appended.
        /// </summary>
        /// <value>The file.</value>
        private byte[] Data { get; set; }

        /// <summary>
        /// Gets the FAT, one entry per sector the FAT sectors can describe.
        /// </summary>
        /// <value>The FAT entries.</value>
        private uint[] Fat { get; }

        /// <summary>
        /// Gets the first sector of the directory chain.
        /// </summary>
        /// <value>The sector location.</value>
        private uint DirectoryStart { get; }

        /// <summary>
        /// Gets the first sector of the mini FAT chain.
        /// </summary>
        /// <value>The sector location, or end-of-chain when the file has no mini FAT.</value>
        private uint MiniFatStart { get; }

        /// <summary>
        /// Gets the size below which a stream is stored in the mini stream.
        /// </summary>
        /// <value>The cutoff, in bytes.</value>
        private uint MiniStreamCutoff { get; }

        /// <summary>
        /// Applies one whole-file transform.
        /// </summary>
        /// <param name="file">The compound file.</param>
        /// <param name="kind">The transform: <c>cut</c>, <c>reverse</c>, <c>unallocated</c> or <c>move</c>.</param>
        /// <param name="argument">The transform's argument.</param>
        /// <returns>The transformed file.</returns>
        /// <exception cref="FormatException">The transform is unknown, or cannot apply to the file.</exception>
        internal static byte[] Apply(byte[] file, string kind, string argument) =>
            kind switch
            {
                "cut" => Cut(file, ParseCount(argument)),
                "reverse" => new CompoundImage(file).ReverseStream(argument),
                "unallocated" => new CompoundImage(file).InsertUnallocated(ParseCount(argument)),
                "move" when argument == "minifat" => new CompoundImage(file).MoveMiniFatToEnd(),
                "move" => throw new FormatException($"move:{argument} is not move:minifat."),
                _ => throw new FormatException($"There is no transform named {kind}."),
            };

        /// <summary>
        /// Keeps the first bytes of a file, or drops its last bytes.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="length">The number of bytes to keep, or, when negative, the number to drop from the end.</param>
        /// <returns>The shortened file.</returns>
        /// <exception cref="FormatException">The file is too short.</exception>
        private static byte[] Cut(byte[] file, int length)
        {
            int keep = length >= 0 ? length : file.Length + length;
            if (keep < 0 || keep > file.Length)
                throw new FormatException($"cut={length} does not fit a {file.Length}-byte file.");

            return file[..keep];
        }

        /// <summary>
        /// Parses a transform's signed decimal argument.
        /// </summary>
        /// <param name="text">The argument.</param>
        /// <returns>The number.</returns>
        /// <exception cref="FormatException">The argument is not a decimal integer.</exception>
        private static int ParseCount(string text) =>
            int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        /// <summary>
        /// Creates an unallocated directory entry: zero bytes, with the sibling and child fields set to
        /// <see cref="NoStream" />.
        /// </summary>
        /// <returns>The entry.</returns>
        private static byte[] FreeEntry()
        {
            var entry = new byte[EntrySize];
            foreach (int field in s_referenceOffsets)
                BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(field), NoStream);

            return entry;
        }

        /// <summary>
        /// Computes the file offset of a sector.
        /// </summary>
        /// <param name="sector">The sector location.</param>
        /// <returns>The offset, after the 512-byte header.</returns>
        private static int Offset(uint sector) =>
            SectorSize + checked((int)sector * SectorSize);

        /// <summary>
        /// Stores the regular sectors of the first stream with a name in reverse physical order.
        /// </summary>
        /// <param name="name">The stream name.</param>
        /// <returns>The transformed file.</returns>
        /// <exception cref="FormatException">There is no such stream, or it is in the mini stream or holds one sector.</exception>
        /// <remarks>
        /// Logical sector <c>i</c> moves to the physical sector that held logical sector <c>n - 1 - i</c>, the FAT links
        /// the sectors in logical order, and the entry's starting sector becomes the sector that held the last one.
        /// </remarks>
        private byte[] ReverseStream(string name)
        {
            int entryOffset = FindStreamEntry(name);
            uint start = BinaryPrimitives.ReadUInt32LittleEndian(Data.AsSpan(entryOffset + 116));
            ulong size = BinaryPrimitives.ReadUInt64LittleEndian(Data.AsSpan(entryOffset + 120));
            if (size < MiniStreamCutoff)
                throw new FormatException($"reverse:{name} is in the mini stream ({size} bytes).");

            List<uint> chain = Chain(start);
            int n = chain.Count;
            if (n < 2)
                throw new FormatException($"reverse:{name} has only one sector.");

            byte[][] blocks = [.. chain.Select(s => Data.AsSpan(Offset(s), SectorSize).ToArray())];
            for (int i = 0; i < n; i++)
                blocks[i].CopyTo(Data, Offset(chain[n - 1 - i]));

            for (int i = 0; i < n - 1; i++)
                Fat[chain[n - 1 - i]] = chain[n - 2 - i];

            Fat[chain[0]] = EndOfChain;
            BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(entryOffset + 116), chain[n - 1]);
            WriteFat();
            return Data;
        }

        /// <summary>
        /// Inserts unallocated directory entries after the root entry.
        /// </summary>
        /// <param name="count">The number of entries.</param>
        /// <returns>The transformed file.</returns>
        /// <exception cref="FormatException"><paramref name="count" /> is not positive, or the FAT has no entry for a needed sector.</exception>
        /// <remarks>
        /// Free entries that only pad the last directory sector are dropped first; every sibling and child field of an
        /// allocated entry that is 1 or more grows by <paramref name="count" />; and the directory chain is extended with
        /// appended sectors when it runs out of room.
        /// </remarks>
        private byte[] InsertUnallocated(int count)
        {
            if (count < 1)
                throw new FormatException("unallocated= needs a positive count.");

            List<uint> chain = Chain(DirectoryStart);
            var entries = new List<byte[]>();
            foreach (uint sector in chain)
            {
                for (int k = 0; k < SectorSize / EntrySize; k++)
                    entries.Add(Data.AsSpan(Offset(sector) + (k * EntrySize), EntrySize).ToArray());
            }

            while (entries.Count > 1 && entries[^1][ObjectTypeOffset] == 0)
                entries.RemoveAt(entries.Count - 1);

            foreach (byte[] entry in entries.Where(entry => entry[ObjectTypeOffset] != 0))
            {
                foreach (int field in s_referenceOffsets)
                {
                    uint value = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(field));
                    if (value != NoStream && value >= 1)
                        BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(field), value + (uint)count);
                }
            }

            for (int i = 0; i < count; i++)
                entries.Insert(1, FreeEntry());

            const int perSector = SectorSize / EntrySize;
            while (entries.Count % perSector != 0)
                entries.Add(FreeEntry());

            while (chain.Count < entries.Count / perSector)
            {
                uint sector = AppendSector();
                Fat[chain[^1]] = sector;
                Fat[sector] = EndOfChain;
                chain.Add(sector);
            }

            for (int i = 0; i < entries.Count; i++)
                entries[i].CopyTo(Data, Offset(chain[i / perSector]) + ((i % perSector) * EntrySize));

            WriteFat();
            return Data;
        }

        /// <summary>
        /// Copies the mini FAT to sectors appended at the end of the file.
        /// </summary>
        /// <returns>The transformed file.</returns>
        /// <exception cref="FormatException">The file has no mini FAT, or the FAT has no entry for an appended sector.</exception>
        /// <remarks>
        /// The new sectors are linked in order, the old ones are marked free, and the header's first mini FAT sector
        /// (offset <c>0x3C</c>) points at the first new one.
        /// </remarks>
        private byte[] MoveMiniFatToEnd()
        {
            List<uint> chain = Chain(MiniFatStart);
            if (chain.Count == 0)
                throw new FormatException("move:minifat needs a file with a mini FAT.");

            var moved = new List<uint>();
            foreach (uint sector in chain)
            {
                byte[] content = Data.AsSpan(Offset(sector), SectorSize).ToArray();
                uint target = AppendSector();
                content.CopyTo(Data, Offset(target));
                moved.Add(target);
            }

            foreach (uint sector in chain)
                Fat[sector] = FreeSector;

            for (int i = 0; i < moved.Count; i++)
                Fat[moved[i]] = i + 1 < moved.Count ? moved[i + 1] : EndOfChain;

            BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(0x3C), moved[0]);
            WriteFat();
            return Data;
        }

        /// <summary>
        /// Follows a sector chain through the FAT.
        /// </summary>
        /// <param name="start">The first sector, or an end or free marker for an empty chain.</param>
        /// <returns>The chain's sectors, in order.</returns>
        /// <exception cref="FormatException">The chain is longer than the FAT, so it has a cycle.</exception>
        private List<uint> Chain(uint start)
        {
            var chain = new List<uint>();
            for (uint s = start; s < 0xFFFFFFFA; s = Fat[s])
            {
                if (chain.Count > Fat.Length)
                    throw new FormatException("The FAT has a cycle.");

                chain.Add(s);
            }

            return chain;
        }

        /// <summary>
        /// Finds the first stream entry with a name, in directory order.
        /// </summary>
        /// <param name="name">The stream name.</param>
        /// <returns>The entry's offset in the file.</returns>
        /// <exception cref="FormatException">No stream entry has the name.</exception>
        private int FindStreamEntry(string name)
        {
            foreach (uint sector in Chain(DirectoryStart))
            {
                for (int k = 0; k < SectorSize / EntrySize; k++)
                {
                    int offset = Offset(sector) + (k * EntrySize);
                    int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(offset + 64));
                    if (Data[offset + ObjectTypeOffset] != 2 || nameLength < 2)
                        continue;

                    if (Encoding.Unicode.GetString(Data, offset, nameLength - 2) == name)
                        return offset;
                }
            }

            throw new FormatException($"There is no stream entry named {name}.");
        }

        /// <summary>
        /// Appends a zeroed sector at the end of the file.
        /// </summary>
        /// <returns>The new sector's location.</returns>
        /// <exception cref="FormatException">The FAT has no entry for the new sector.</exception>
        private uint AppendSector()
        {
            uint sector = (uint)((Data.Length - SectorSize) / SectorSize);
            if (sector >= Fat.Length)
                throw new FormatException("The FAT has no entry for an appended sector.");

            var grown = new byte[Data.Length + SectorSize];
            Data.CopyTo(grown, 0);
            Data = grown;
            return sector;
        }

        /// <summary>
        /// Writes the FAT back to its sectors.
        /// </summary>
        private void WriteFat()
        {
            for (int f = 0; f < _fatSectors.Count; f++)
            {
                for (int i = 0; i < SectorSize / 4; i++)
                    BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(Offset(_fatSectors[f]) + (4 * i)), Fat[(f * (SectorSize / 4)) + i]);
            }
        }
    }
}
