// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntheticVersion4ContainerStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides a read-only, seekable stream whose bytes are a version-4 compound file holding one stream of any length,
/// computed as they are read, so that a test can read a stream of more than 2 GB without writing it anywhere.
/// </summary>
/// <remarks>
/// <para>
/// The container is conformant to [MS-CFB]: its sectors are the FAT sectors, then the DIFAT sectors, the single
/// directory sector and the stream's data, which skips the range lock sector (0x7FFFE) that section 2.8 reserves once a
/// file passes 2 GB. Byte <c>i</c> of the stream is the catalog pattern, <c>(i * 31 + 7) mod 256</c>.
/// </para>
/// <para>
/// Reading is computed per sector, so a reader that buffers the whole stream allocates the container's full length; a
/// test reads it with <see cref="CompoundReadStrategy.Streaming" />.
/// </para>
/// </remarks>
internal sealed class SyntheticVersion4ContainerStream : Stream
{
    /// <summary>The size of a version-4 sector, in bytes.</summary>
    private const int SectorSize = 4096;

    /// <summary>The number of 32-bit entries a FAT or DIFAT sector holds.</summary>
    private const int EntriesPerSector = SectorSize / 4;

    /// <summary>The sector that covers the file offsets 0x7FFFFF00 to 0x7FFFFFFF in a file of 4096-byte sectors.</summary>
    private const uint RangeLockSector = 0x7FFFE;

    /// <summary>The name of the stream the container holds.</summary>
    private readonly string _streamName;

    /// <summary>The length of the stream the container holds, in bytes.</summary>
    private readonly long _streamLength;

    /// <summary>The number of FAT sectors, which begin at sector 0.</summary>
    private readonly uint _fatSectors;

    /// <summary>The number of DIFAT sectors, which follow the FAT sectors.</summary>
    private readonly uint _difatSectors;

    /// <summary>The number of sectors the container allocates, the range lock sector included.</summary>
    private readonly uint _totalSectors;

    /// <summary>The offset of the next byte to read.</summary>
    private long _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticVersion4ContainerStream" /> class.
    /// </summary>
    /// <param name="streamName">The name of the stream the container holds.</param>
    /// <param name="streamLength">The length of the stream the container holds, in bytes.</param>
    public SyntheticVersion4ContainerStream(string streamName, long streamLength)
    {
        _streamName = streamName;
        _streamLength = streamLength;

        long dataSectors = (streamLength + SectorSize - 1) / SectorSize;
        long fatSectors = 0;
        long difatSectors = 0;
        while (true)
        {
            long withoutRangeLock = fatSectors + difatSectors + 1 + dataSectors;
            long total = withoutRangeLock > RangeLockSector ? withoutRangeLock + 1 : withoutRangeLock;
            long newFat = (total + EntriesPerSector - 1) / EntriesPerSector;
            long newDifat = newFat <= CfbHeader.HeaderDifatCount
                ? 0
                : (newFat - CfbHeader.HeaderDifatCount + EntriesPerSector - 2) / (EntriesPerSector - 1);
            if (newFat == fatSectors && newDifat == difatSectors)
            {
                _totalSectors = (uint)total;
                break;
            }

            fatSectors = newFat;
            difatSectors = newDifat;
        }

        _fatSectors = (uint)fatSectors;
        _difatSectors = (uint)difatSectors;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => ((long)_totalSectors + 1) * SectorSize;

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    /// <summary>Gets the sector that holds the directory.</summary>
    /// <value>The sector after the FAT and DIFAT sectors.</value>
    private uint DirectorySector => _fatSectors + _difatSectors;

    /// <summary>Gets the sector that holds the first bytes of the stream.</summary>
    /// <value>The sector after the directory.</value>
    private uint FirstDataSector => DirectorySector + 1;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        long remaining = Length - _position;
        int count = (int)Math.Max(0, Math.Min(buffer.Length, remaining));
        int done = 0;
        Span<byte> sector = stackalloc byte[SectorSize];

        while (done < count)
        {
            long sectorIndex = (_position / SectorSize) - 1;
            int within = (int)(_position % SectorSize);
            int take = Math.Min(SectorSize - within, count - done);

            FillSector(sectorIndex, sector);
            sector.Slice(within, take).CopyTo(buffer.Slice(done));
            _position += take;
            done += take;
        }

        return count;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => Length + offset,
        };

        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <summary>
    /// Computes the bytes of one sector.
    /// </summary>
    /// <param name="sectorIndex">The sector number, or -1 for the header sector.</param>
    /// <param name="sector">Receives the sector's bytes.</param>
    private void FillSector(long sectorIndex, Span<byte> sector)
    {
        sector.Clear();
        if (sectorIndex < 0)
        {
            FillHeader(sector);
        }
        else if (sectorIndex < _fatSectors)
        {
            for (int i = 0; i < EntriesPerSector; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(i * 4), FatEntry((sectorIndex * EntriesPerSector) + i));
        }
        else if (sectorIndex < DirectorySector)
        {
            FillDifat((uint)(sectorIndex - _fatSectors), sector);
        }
        else if (sectorIndex == DirectorySector)
        {
            FillDirectory(sector);
        }
        else if (sectorIndex != RangeLockSector)
        {
            long dataIndex = sectorIndex - FirstDataSector - (sectorIndex > RangeLockSector ? 1 : 0);
            long start = dataIndex * SectorSize;
            for (int i = 0; i < SectorSize && start + i < _streamLength; i++)
                sector[i] = CompoundContainerBytes.PatternByte(start + i);
        }
    }

    /// <summary>
    /// Computes the header of the container.
    /// </summary>
    /// <param name="sector">Receives the header sector, cleared.</param>
    private void FillHeader(Span<byte> sector)
    {
        CfbHeader.Signature.CopyTo(sector);
        BinaryPrimitives.WriteUInt16LittleEndian(sector.Slice(0x18), 0x003E);
        BinaryPrimitives.WriteUInt16LittleEndian(sector.Slice(0x1A), 4);
        BinaryPrimitives.WriteUInt16LittleEndian(sector.Slice(0x1C), 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(sector.Slice(0x1E), 12);
        BinaryPrimitives.WriteUInt16LittleEndian(sector.Slice(0x20), 6);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x28), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x2C), _fatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x30), DirectorySector);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x38), 4096);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x3C), CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x44), _difatSectors > 0 ? _fatSectors : CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x48), _difatSectors);
        for (uint i = 0; i < CfbHeader.HeaderDifatCount; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(0x4C + ((int)i * 4)), i < _fatSectors ? i : CfbHeader.FreeSector);
    }

    /// <summary>
    /// Computes one DIFAT sector.
    /// </summary>
    /// <param name="difatIndex">The position of the sector in the DIFAT chain.</param>
    /// <param name="sector">Receives the sector, cleared.</param>
    private void FillDifat(uint difatIndex, Span<byte> sector)
    {
        for (int i = 0; i < EntriesPerSector - 1; i++)
        {
            long fatSector = CfbHeader.HeaderDifatCount + ((long)difatIndex * (EntriesPerSector - 1)) + i;
            BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(i * 4), fatSector < _fatSectors ? (uint)fatSector : CfbHeader.FreeSector);
        }

        uint next = difatIndex + 1 < _difatSectors ? _fatSectors + difatIndex + 1 : CfbHeader.EndOfChain;
        BinaryPrimitives.WriteUInt32LittleEndian(sector.Slice(SectorSize - 4), next);
    }

    /// <summary>
    /// Computes the directory sector: the root entry, the stream's entry and free entries.
    /// </summary>
    /// <param name="sector">Receives the sector, cleared.</param>
    private void FillDirectory(Span<byte> sector)
    {
        for (int i = 0; i < SectorSize / CompoundContainerBytes.DirectoryEntrySize; i++)
        {
            Span<byte> entry = sector.Slice(i * CompoundContainerBytes.DirectoryEntrySize, CompoundContainerBytes.DirectoryEntrySize);
            BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(CompoundContainerBytes.LeftSiblingOffset), CfbHeader.NoStream);
            BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(CompoundContainerBytes.RightSiblingOffset), CfbHeader.NoStream);
            BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(CompoundContainerBytes.ChildOffset), CfbHeader.NoStream);
        }

        Span<byte> root = sector.Slice(0, CompoundContainerBytes.DirectoryEntrySize);
        WriteName(root, "Root Entry");
        root[CompoundContainerBytes.TypeOffset] = (byte)CompoundEntryType.RootStorage;
        root[CompoundContainerBytes.ColorOffset] = (byte)CompoundEntryColor.Black;
        BinaryPrimitives.WriteUInt32LittleEndian(root.Slice(CompoundContainerBytes.ChildOffset), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(root.Slice(CompoundContainerBytes.StartSectorOffset), CfbHeader.EndOfChain);

        Span<byte> stream = sector.Slice(CompoundContainerBytes.DirectoryEntrySize, CompoundContainerBytes.DirectoryEntrySize);
        WriteName(stream, _streamName);
        stream[CompoundContainerBytes.TypeOffset] = (byte)CompoundEntryType.Stream;
        stream[CompoundContainerBytes.ColorOffset] = (byte)CompoundEntryColor.Black;
        BinaryPrimitives.WriteUInt32LittleEndian(stream.Slice(CompoundContainerBytes.StartSectorOffset), FirstDataSector);
        BinaryPrimitives.WriteUInt64LittleEndian(stream.Slice(CompoundContainerBytes.SizeOffset), (ulong)_streamLength);
    }

    /// <summary>
    /// Returns the FAT entry of a sector.
    /// </summary>
    /// <param name="sector">The sector number.</param>
    /// <returns>
    /// The sector that follows <paramref name="sector" /> in its chain, or the special value that marks it.
    /// </returns>
    private uint FatEntry(long sector)
    {
        if (sector >= _totalSectors)
            return CfbHeader.FreeSector;

        if (sector < _fatSectors)
            return CfbHeader.FatSector;

        if (sector < DirectorySector)
            return CfbHeader.DifatSector;

        if (sector == DirectorySector || sector == RangeLockSector)
            return CfbHeader.EndOfChain;

        long next = sector + 1 == RangeLockSector ? sector + 2 : sector + 1;
        return next < _totalSectors ? (uint)next : CfbHeader.EndOfChain;
    }

    /// <summary>
    /// Writes a directory entry name and its length.
    /// </summary>
    /// <param name="entry">The directory entry.</param>
    /// <param name="name">The name.</param>
    private static void WriteName(Span<byte> entry, string name)
    {
        int written = Encoding.Unicode.GetBytes(name, entry);
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(CompoundContainerBytes.NameLengthOffset), (ushort)(written + 2));
    }
}
