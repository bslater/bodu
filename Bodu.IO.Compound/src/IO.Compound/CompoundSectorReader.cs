// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundSectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound;

/// <summary>
/// Resolves sector chains within a compound file, translating the regular FAT and the mini-FAT into contiguous stream
/// payloads.
/// </summary>
/// <remarks>
/// The reader materializes the full FAT on construction. The mini-stream and mini-FAT are built lazily through
/// <see cref="InitializeMiniStream(uint, long)" /> once the root storage entry has been located, because the mini
/// stream is itself stored as the root entry's regular-sector chain.
/// </remarks>
internal sealed class CompoundSectorReader
{
    /// <summary>
    /// The full compound-file byte content.
    /// </summary>
    private readonly byte[] _data;

    /// <summary>
    /// The parsed header providing sector sizes and entry points.
    /// </summary>
    private readonly CompoundFileHeader _header;

    /// <summary>
    /// The materialized regular file-allocation table.
    /// </summary>
    private readonly uint[] _fat;

    /// <summary>
    /// The materialized mini-FAT, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" />
    /// runs.
    /// </summary>
    private uint[]? _miniFat;

    /// <summary>
    /// The materialized mini stream, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" />
    /// runs.
    /// </summary>
    private byte[]? _miniStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundSectorReader" /> class and builds the regular FAT.
    /// </summary>
    /// <param name="data">The full compound-file byte content.</param>
    /// <param name="header">The parsed compound-file header.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the FAT cannot be assembled from the declared layout.
    /// </exception>
    internal CompoundSectorReader(byte[] data, CompoundFileHeader header)
    {
        _data = data;
        _header = header;
        _fat = BuildFat();
    }

    /// <summary>
    /// Reads the contiguous payload of a regular-sector chain.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <param name="size">
    /// The number of payload bytes to return; the chain is read in full then trimmed to this size.
    /// </param>
    /// <returns>The materialized payload, exactly <paramref name="size" /> bytes long.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular, references an out-of-range sector, or is shorter than
    /// <paramref name="size" />.
    /// </exception>
    internal byte[] ReadChain(uint startSector, long size)
    {
        if (size <= 0 || startSector == CompoundFileHeader.EndOfChain)
            return [];

        byte[] chain = ReadChainToEnd(startSector);
        CompoundThrowHelper.ThrowFormatIf(chain.Length < size, CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

        if (chain.Length == size)
            return chain;

        byte[] result = new byte[size];
        Array.Copy(chain, result, size);
        return result;
    }

    /// <summary>
    /// Reads every byte of a regular-sector chain, following the FAT until the end-of-chain sentinel.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <returns>The concatenated bytes of every sector in the chain; empty when the chain is empty.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular or references an out-of-range sector.
    /// </exception>
    internal byte[] ReadChainToEnd(uint startSector)
    {
        if (startSector == CompoundFileHeader.EndOfChain)
            return [];

        using MemoryStream buffer = new();
        uint sector = startSector;
        int guard = 0;

        while (sector != CompoundFileHeader.EndOfChain)
        {
            CompoundThrowHelper.ThrowFormatIf(
                sector >= (uint)_fat.Length || guard++ > _fat.Length,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

            buffer.Write(ReadSector(sector));
            sector = _fat[sector];
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Builds the mini stream and mini-FAT from the root storage entry so that <see cref="ReadMiniChain(uint, long)" />
    /// can resolve small streams.
    /// </summary>
    /// <param name="rootStartSector">The first regular sector of the root storage's mini stream.</param>
    /// <param name="rootStreamSize">The size, in bytes, of the mini stream.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the mini stream or mini-FAT chains are malformed.
    /// </exception>
    internal void InitializeMiniStream(uint rootStartSector, long rootStreamSize)
    {
        _miniStream = ReadChain(rootStartSector, rootStreamSize);

        byte[] miniFatBytes = ReadChain(_header.FirstMiniFatSector, (long)_header.MiniFatSectorCount * _header.SectorSize);
        uint[] miniFat = new uint[miniFatBytes.Length / sizeof(uint)];
        for (int i = 0; i < miniFat.Length; i++)
            miniFat[i] = BinaryPrimitives.ReadUInt32LittleEndian(miniFatBytes.AsSpan(i * sizeof(uint)));

        _miniFat = miniFat;
    }

    /// <summary>
    /// Reads the contiguous payload of a mini-sector chain.
    /// </summary>
    /// <param name="startMiniSector">The first mini sector of the chain.</param>
    /// <param name="size">
    /// The number of payload bytes to return; the chain is read in full then trimmed to this size.
    /// </param>
    /// <returns>The materialized payload, exactly <paramref name="size" /> bytes long.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the mini stream is uninitialized, the chain is circular, references an out-of-range mini sector, or
    /// is shorter than <paramref name="size" />.
    /// </exception>
    internal byte[] ReadMiniChain(uint startMiniSector, long size)
    {
        if (size <= 0 || startMiniSector == CompoundFileHeader.EndOfChain)
            return [];

        if (_miniFat is null || _miniStream is null)
            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

        using MemoryStream buffer = new();
        uint sector = startMiniSector;
        int guard = 0;

        while (sector != CompoundFileHeader.EndOfChain)
        {
            CompoundThrowHelper.ThrowFormatIf(
                sector >= (uint)_miniFat!.Length || guard++ > _miniFat.Length,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

            int offset = (int)sector * _header.MiniSectorSize;
            CompoundThrowHelper.ThrowFormatIf(
                offset + _header.MiniSectorSize > _miniStream!.Length,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

            buffer.Write(_miniStream.AsSpan(offset, _header.MiniSectorSize));
            sector = _miniFat[sector];
        }

        return Trim(buffer, size);
    }

    /// <summary>
    /// Copies the first <paramref name="size" /> bytes of <paramref name="buffer" /> into a fresh array.
    /// </summary>
    /// <param name="buffer">The accumulated chain payload.</param>
    /// <param name="size">The declared payload size.</param>
    /// <returns>An array of exactly <paramref name="size" /> bytes.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain produced fewer than <paramref name="size" /> bytes.
    /// </exception>
    private static byte[] Trim(MemoryStream buffer, long size)
    {
        CompoundThrowHelper.ThrowFormatIf(buffer.Length < size, CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

        byte[] result = new byte[size];
        buffer.Position = 0;
        _ = buffer.Read(result, 0, (int)size);
        return result;
    }

    /// <summary>
    /// Returns a span over the bytes of a single regular sector.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <returns>A span over the sector's bytes.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector lies beyond the end of the data.
    /// </exception>
    private ReadOnlySpan<byte> ReadSector(uint sector)
    {
        long offset = (long)(sector + 1) * _header.SectorSize;
        CompoundThrowHelper.ThrowFormatIf(
            offset + _header.SectorSize > _data.Length,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain);

        return _data.AsSpan((int)offset, _header.SectorSize);
    }

    /// <summary>
    /// Assembles the regular FAT from the inline DIFAT and any extended DIFAT sectors.
    /// </summary>
    /// <returns>The flattened FAT, one entry per regular sector.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when the DIFAT chain is circular or malformed.</exception>
    private uint[] BuildFat()
    {
        List<uint> fatSectors = new();

        foreach (uint id in _header.Difat)
        {
            if (IsRegularSector(id))
                fatSectors.Add(id);
        }

        uint difatSector = _header.FirstDifatSector;
        int perSector = _header.EntriesPerSector;
        int guard = 0;

        while (difatSector != CompoundFileHeader.EndOfChain && difatSector != CompoundFileHeader.FreeSector)
        {
            CompoundThrowHelper.ThrowFormatIf(guard++ > (_data.Length / _header.SectorSize) + 1, CompoundResourceStrings.Format_Invalid_CompoundDirectory);

            ReadOnlySpan<byte> sector = ReadSector(difatSector);
            for (int i = 0; i < perSector - 1; i++)
            {
                uint id = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice(i * sizeof(uint)));
                if (IsRegularSector(id))
                    fatSectors.Add(id);
            }

            difatSector = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice((perSector - 1) * sizeof(uint)));
        }

        uint[] fat = new uint[fatSectors.Count * perSector];
        int index = 0;
        foreach (uint fatSector in fatSectors)
        {
            ReadOnlySpan<byte> sector = ReadSector(fatSector);
            for (int i = 0; i < perSector; i++)
                fat[index++] = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice(i * sizeof(uint)));
        }

        return fat;
    }

    /// <summary>
    /// Determines whether a sector identifier refers to a real sector rather than a reserved sentinel.
    /// </summary>
    /// <param name="id">The sector identifier to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="id" /> is a regular sector; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsRegularSector(uint id) =>
        id is not CompoundFileHeader.FreeSector
            and not CompoundFileHeader.EndOfChain
            and not CompoundFileHeader.FatSector
            and not CompoundFileHeader.DifatSector;
}
