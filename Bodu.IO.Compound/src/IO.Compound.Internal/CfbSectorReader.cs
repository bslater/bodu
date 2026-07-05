// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbSectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Resolves sector chains within a compound file, translating the regular FAT and the mini-FAT into contiguous stream
/// payloads.
/// </summary>
/// <remarks>
/// The reader materializes the full FAT on construction. The mini-stream and mini-FAT are built lazily through
/// <see cref="InitializeMiniStream(uint, long)" /> once the root storage entry has been located, because the mini
/// stream is itself stored as the root entry's regular-sector chain.
/// </remarks>
internal sealed class CfbSectorReader
{
    /// <summary>The random-access source of compound-file bytes.</summary>
    private readonly CfbDataSource _source;

    /// <summary>The parsed header providing sector sizes and entry points.</summary>
    private readonly CfbHeader _header;

    /// <summary>The materialized regular file-allocation table.</summary>
    private readonly uint[] _fat;

    /// <summary>The validation level governing how recoverable chain anomalies are handled.</summary>
    private readonly CompoundValidationLevel _level;

    /// <summary>The materialized mini-FAT, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" /> runs.</summary>
    private uint[]? _miniFat;

    /// <summary>The materialized mini stream, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" /> runs.</summary>
    private byte[]? _miniStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbSectorReader" /> class and builds the regular FAT.
    /// </summary>
    /// <param name="source">The random-access source of compound-file bytes.</param>
    /// <param name="header">The parsed compound-file header.</param>
    /// <param name="level">The validation level governing recoverable chain anomalies.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the FAT cannot be assembled from the declared layout.
    /// </exception>
    internal CfbSectorReader(CfbDataSource source, CfbHeader header, CompoundValidationLevel level)
    {
        _source = source;
        _header = header;
        _level = level;
        _fat = BuildFat();
    }

    /// <summary>
    /// Evaluates a recoverable chain anomaly: returns <see langword="true" /> to stop walking at
    /// <see cref="CompoundValidationLevel.Minimal" />, throws at stricter levels, and returns <see langword="false" />
    /// when the condition does not hold.
    /// </summary>
    /// <param name="condition">The anomaly condition to test.</param>
    /// <param name="category">The failure category reported when the anomaly is rejected.</param>
    /// <returns><see langword="true" /> to stop the current chain walk; otherwise <see langword="false" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="condition" /> holds and the level is stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />.
    /// </exception>
    private bool StopOrThrow(bool condition, CompoundFileError category)
    {
        if (!condition)
            return false;

        if (_level == CompoundValidationLevel.Minimal)
            return true;

        CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundSectorChain, category);
        return true;
    }

    /// <summary>
    /// Gets the regular sector size, in bytes.
    /// </summary>
    /// <value>The sector size.</value>
    internal int SectorSize => _header.SectorSize;

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
        if (size <= 0 || startSector == CfbHeader.EndOfChain)
            return [];

        byte[] chain = ReadChainToEnd(startSector);
        if (chain.Length < size && !StopOrThrow(true, CompoundFileError.StreamChainTooShort))
            return chain;

        if (chain.Length == size)
            return chain;

        // Under a tolerant level a short chain is padded with zeros to the declared length.
        byte[] result = new byte[size];
        Array.Copy(chain, result, Math.Min(chain.Length, size));
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
        if (startSector == CfbHeader.EndOfChain)
            return [];

        using MemoryStream buffer = new();
        Span<byte> scratch = stackalloc byte[_header.SectorSize];
        uint sector = startSector;
        int guard = 0;

        while (sector != CfbHeader.EndOfChain)
        {
            if (StopOrThrow(sector >= (uint)_fat.Length, CompoundFileError.SectorOutOfRange))
                break;
            if (StopOrThrow(guard++ > _fat.Length, CompoundFileError.FatCycle))
                break;

            buffer.Write(ReadSector(sector, scratch));
            sector = _fat[sector];
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Walks the regular FAT to collect the ordered sector identifiers of a chain.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <returns>The ordered sector identifiers; empty when the chain is empty.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular or references an out-of-range sector.
    /// </exception>
    internal uint[] GetSectorChain(uint startSector)
    {
        if (startSector == CfbHeader.EndOfChain)
            return [];

        List<uint> chain = new();
        uint sector = startSector;

        while (sector != CfbHeader.EndOfChain)
        {
            if (StopOrThrow(sector >= (uint)_fat.Length, CompoundFileError.SectorOutOfRange))
                break;
            if (StopOrThrow(chain.Count > _fat.Length, CompoundFileError.FatCycle))
                break;

            chain.Add(sector);
            sector = _fat[sector];
        }

        return chain.ToArray();
    }

    /// <summary>
    /// Reads a sub-range of a single regular sector into the destination.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="within">The byte offset within the sector at which to start.</param>
    /// <param name="destination">
    /// The buffer that receives the bytes; its length must not exceed the remaining sector bytes.
    /// </param>
    /// <exception cref="CompoundFileFormatException">Thrown when the range lies beyond the end of the data.</exception>
    internal void ReadWithinSector(uint sector, int within, Span<byte> destination)
    {
        long offset = ((long)(sector + 1) * _header.SectorSize) + within;
        CompoundThrowHelper.ThrowFormatIf(
            offset + destination.Length > _source.Length,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.SectorOutOfRange);

        _source.Read(offset, destination);
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
        if (size <= 0 || startMiniSector == CfbHeader.EndOfChain)
            return [];

        if (_miniFat is null || _miniStream is null)
        {
            if (StopOrThrow(true, CompoundFileError.InvalidMiniFat))
                return new byte[size];
        }

        using MemoryStream buffer = new();
        uint sector = startMiniSector;
        int guard = 0;

        while (sector != CfbHeader.EndOfChain)
        {
            if (StopOrThrow(sector >= (uint)_miniFat!.Length, CompoundFileError.SectorOutOfRange))
                break;
            if (StopOrThrow(guard++ > _miniFat.Length, CompoundFileError.InvalidMiniFat))
                break;

            int offset = (int)sector * _header.MiniSectorSize;
            if (StopOrThrow(offset + _header.MiniSectorSize > _miniStream!.Length, CompoundFileError.InvalidMiniFat))
                break;

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
    private byte[] Trim(MemoryStream buffer, long size)
    {
        // Under a tolerant level a short chain is padded with zeros to the declared length.
        _ = StopOrThrow(buffer.Length < size, CompoundFileError.StreamChainTooShort);

        byte[] result = new byte[size];
        buffer.Position = 0;
        _ = buffer.Read(result, 0, (int)Math.Min(size, buffer.Length));
        return result;
    }

    /// <summary>
    /// Returns a span over the bytes of a single regular sector.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="scratch">
    /// A buffer of at least one sector used by a streaming source; ignored by an in-memory source.
    /// </param>
    /// <returns>A span over the sector's bytes.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector lies beyond the end of the data.
    /// </exception>
    private ReadOnlySpan<byte> ReadSector(uint sector, Span<byte> scratch)
    {
        long offset = (long)(sector + 1) * _header.SectorSize;
        CompoundThrowHelper.ThrowFormatIf(
            offset + _header.SectorSize > _source.Length,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.SectorOutOfRange);

        return _source.GetSpan(offset, _header.SectorSize, scratch);
    }

    /// <summary>
    /// Assembles the regular FAT from the inline DIFAT and any extended DIFAT sectors.
    /// </summary>
    /// <returns>The flattened FAT, one entry per regular sector.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when the DIFAT chain is circular or malformed.</exception>
    private uint[] BuildFat()
    {
        // A FAT sector is referenced exactly once and there can be no more FAT sectors than physical sectors in
        // the file. Deduplicating the DIFAT entries and capping the count against the source length prevents a
        // crafted DIFAT (duplicate or inflated entries) from amplifying the FAT allocation ~127x and overflowing
        // the array-size arithmetic.
        long maxFatSectors = _header.SectorSize > 0 ? _source.Length / _header.SectorSize : 0;
        List<uint> fatSectors = new();
        HashSet<uint> seen = new();
        bool stop = false;

        bool TryAddFatSector(uint id)
        {
            if (!IsRegularSector(id) || !seen.Add(id))
                return true;

            if (StopOrThrow(fatSectors.Count >= maxFatSectors, CompoundFileError.InvalidDifat))
                return false;

            fatSectors.Add(id);
            return true;
        }

        foreach (uint id in _header.Difat)
        {
            if (!TryAddFatSector(id))
            {
                stop = true;
                break;
            }
        }

        uint difatSector = _header.FirstDifatSector;
        int perSector = _header.EntriesPerSector;
        int guard = 0;
        Span<byte> scratch = stackalloc byte[_header.SectorSize];

        while (!stop && difatSector != CfbHeader.EndOfChain && difatSector != CfbHeader.FreeSector)
        {
            if (StopOrThrow(guard++ > (_source.Length / _header.SectorSize) + 1, CompoundFileError.InvalidDifat))
                break;

            ReadOnlySpan<byte> sector = ReadSector(difatSector, scratch);
            for (int i = 0; i < perSector - 1; i++)
            {
                uint id = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice(i * sizeof(uint)));
                if (!TryAddFatSector(id))
                {
                    stop = true;
                    break;
                }
            }

            difatSector = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice((perSector - 1) * sizeof(uint)));
        }

        uint[] fat = new uint[fatSectors.Count * perSector];
        int index = 0;
        foreach (uint fatSector in fatSectors)
        {
            ReadOnlySpan<byte> sector = ReadSector(fatSector, scratch);
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
        id is not CfbHeader.FreeSector
            and not CfbHeader.EndOfChain
            and not CfbHeader.FatSector
            and not CfbHeader.DifatSector;
}
