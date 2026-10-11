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
    /// <summary>The minimum size a short sector chain may be zero-padded to under a tolerant validation level.</summary>
    private const long MinRecoveredStreamBytes = 1L << 20;

    /// <summary>The random-access source of compound-file bytes.</summary>
    private readonly CfbDataSource _source;

    /// <summary>The parsed header providing sector sizes and entry points.</summary>
    private readonly CfbHeader _header;

    /// <summary>The materialized regular file-allocation table.</summary>
    private readonly uint[] _fat;

    /// <summary>The validation level governing how recoverable chain anomalies are handled.</summary>
    private readonly CompoundValidationLevel _level;

    /// <summary>The sectors the DIFAT names as FAT sectors, in FAT order.</summary>
    private readonly List<uint> _fatSectorIds = new();

    /// <summary>The DIFAT sectors read, in chain order.</summary>
    private readonly List<uint> _difatSectorIds = new();

    /// <summary>The materialized mini-FAT, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" /> runs.</summary>
    private uint[]? _miniFat;

    /// <summary>The materialized mini stream, or <see langword="null" /> until <see cref="InitializeMiniStream(uint, long)" /> runs.</summary>
    private byte[]? _miniStream;

    /// <summary>The value that ended the DIFAT chain: its last sector's next-sector field.</summary>
    private uint _difatTerminator = CfbHeader.EndOfChain;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbSectorReader" /> class and builds the regular FAT.
    /// </summary>
    /// <param name="source">The random-access source of compound-file bytes.</param>
    /// <param name="header">The parsed compound-file header.</param>
    /// <param name="level">The validation level governing recoverable chain anomalies.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the FAT cannot be assembled from the declared layout, or, at a level stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />, when the data is shorter than the header sector.
    /// </exception>
    internal CfbSectorReader(CfbDataSource source, CfbHeader header, CompoundValidationLevel level)
    {
        _source = source;
        _header = header;
        _level = level;

        // The header sector declares every one of its bytes: 512 in version 3, 4096 in version 4.
        _ = MissingDeclaredBytes(source.Length < header.SectorSize);
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
    /// Evaluates bytes a structure declares that the file, cut short inside its final sector, does not hold: returns
    /// <see langword="true" /> at <see cref="CompoundValidationLevel.Minimal" />, where the caller reads them as zero,
    /// and throws at stricter levels.
    /// </summary>
    /// <param name="condition">Whether declared bytes are missing.</param>
    /// <returns>
    /// <see langword="true" /> when bytes are missing and are to be read as zero; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown with <see cref="CompoundFileError.TruncatedFile" /> when <paramref name="condition" /> holds and the
    /// level is stricter than <see cref="CompoundValidationLevel.Minimal" />.
    /// </exception>
    private bool MissingDeclaredBytes(bool condition)
    {
        if (!condition)
            return false;

        if (_level == CompoundValidationLevel.Minimal)
            return true;

        CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundTruncated, CompoundFileError.TruncatedFile);
        return true;
    }

    /// <summary>
    /// Bounds a declared payload size before it is used to size a zero-padded allocation, so a corrupt oversized size
    /// field cannot drive a huge (or <see cref="int" />-overflowing) allocation on the recovery path.
    /// </summary>
    /// <param name="size">The declared payload size, taken from an untrusted directory entry or header field.</param>
    /// <returns>The bounded size to allocate.</returns>
    /// <remarks>
    /// A valid stream's payload never exceeds the container, so this ceiling never affects a well-formed read (which
    /// returns its chain directly rather than padding). The ceiling is the larger of a 1 MiB floor and the container
    /// length, capped at <see cref="Array.MaxLength" />; a size beyond it is rejected under a strict level and clamped
    /// under a tolerant one.
    /// </remarks>
    private long BoundPaddedSize(long size)
    {
        long ceiling = Math.Min(Array.MaxLength, Math.Max(MinRecoveredStreamBytes, _source.Length));
        return StopOrThrow(size > ceiling, CompoundFileError.StreamChainTooShort) ? ceiling : size;
    }

    /// <summary>
    /// Reads the contiguous payload of a regular-sector chain.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <param name="size">
    /// The number of payload bytes to return; the chain is read in full then trimmed to this size.
    /// </param>
    /// <param name="fill">
    /// The value of a byte that a tolerant level supplies for a byte the chain does not hold: zero for a payload, and
    /// <c>0xFF</c> for an allocation table, whose missing entries then read as FREESECT.
    /// </param>
    /// <param name="rangeError">
    /// The category of a chain that leaves the FAT: <see cref="CompoundFileError.SectorOutOfRange" /> for a stream, and
    /// <see cref="CompoundFileError.InvalidMiniFat" /> for the chain that holds the mini FAT.
    /// </param>
    /// <returns>The materialized payload, exactly <paramref name="size" /> bytes long.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular, references an out-of-range sector, or is shorter than
    /// <paramref name="size" />, or when the file ends inside its final sector before a byte the payload declares.
    /// </exception>
    /// <remarks>
    /// A chain shorter than <paramref name="size" /> is a defect: a tolerant level pads it to the declared length
    /// rather than returning the short payload, so the return is always exactly <paramref name="size" /> bytes long
    /// whichever level is in force. A final sector the file cuts short is read as far as the file goes, which is enough
    /// when the payload ends there; a declared byte past the end of the file is
    /// <see cref="CompoundFileError.TruncatedFile" />, read as <paramref name="fill" /> at
    /// <see cref="CompoundValidationLevel.Minimal" />.
    /// </remarks>
    internal byte[] ReadChain(uint startSector, long size, byte fill = 0, CompoundFileError rangeError = CompoundFileError.SectorOutOfRange)
    {
        if (size <= 0 || startSector == CfbHeader.EndOfChain)
            return [];

        size = BoundPaddedSize(size);

        // The result is sized once from the declared length and filled sector by sector; a growable buffer would
        // allocate roughly three times the payload for a large stream. The chain is still walked to its end so a
        // cycle or an out-of-range sector past the declared length is detected as before.
        byte[] result = new byte[size];
        if (fill != 0)
            result.AsSpan().Fill(fill);

        byte[] scratch = new byte[_header.SectorSize];
        long filled = 0;
        foreach (uint sector in WalkChain(startSector, _fat, CompoundFileError.FatCycle, rangeError))
        {
            if (filled >= size)
                continue;

            ReadOnlySpan<byte> bytes = ReadSector(sector, scratch);
            int need = (int)Math.Min(_header.SectorSize, size - filled);
            int take = Math.Min(bytes.Length, need);
            bytes.Slice(0, take).CopyTo(result.AsSpan((int)filled));
            _ = MissingDeclaredBytes(take < need);
            filled += need;
        }

        if (filled < size)
            _ = StopOrThrow(true, CompoundFileError.StreamChainTooShort);

        return result;
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

        foreach (uint sector in WalkChain(startSector, _fat, CompoundFileError.FatCycle, CompoundFileError.SectorOutOfRange))
            chain.Add(sector);

        return chain.ToArray();
    }

    /// <summary>
    /// Walks a FAT chain from <paramref name="startSector" />, yielding each sector identifier exactly once.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <param name="fat">The allocation table (regular FAT or mini-FAT) that links the chain.</param>
    /// <param name="cycleError">The failure category reported when a sector is revisited.</param>
    /// <param name="rangeError">The failure category reported when the chain leaves the table.</param>
    /// <returns>The chain's sector identifiers in order, each appearing at most once.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain references an out-of-range sector, or revisits a sector, at a validation level stricter
    /// than <see cref="CompoundValidationLevel.Minimal" />.
    /// </exception>
    /// <remarks>
    /// A per-chain visited set bounds the walk to the number of distinct real sectors and rejects the first revisit as
    /// a cycle. This is what prevents a crafted self-loop or back-edge from re-reading sectors and amplifying a
    /// caller's accumulated allocation far beyond the container size - the amplification is capped regardless of
    /// validation level, because the visited set is consulted before any sector is yielded.
    /// </remarks>
    private IEnumerable<uint> WalkChain(uint startSector, uint[] fat, CompoundFileError cycleError, CompoundFileError rangeError)
    {
        // An empty chain needs no guard: the loop below tests the same sentinel before its first iteration.
        HashSet<uint> visited = new();
        uint sector = startSector;

        while (sector != CfbHeader.EndOfChain)
        {
            if (StopOrThrow(sector >= (uint)fat.Length, rangeError))
                yield break;
            if (StopOrThrow(!visited.Add(sector), cycleError))
                yield break;

            yield return sector;
            sector = fat[sector];
        }
    }

    /// <summary>
    /// Reads a sub-range of a single regular sector into the destination.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="within">The byte offset within the sector at which to start.</param>
    /// <param name="destination">
    /// The buffer that receives the bytes; its length must not exceed the remaining sector bytes.
    /// </param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector begins at or past the end of the data, or, at a level stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />, when the data ends inside the range.
    /// </exception>
    /// <remarks>
    /// The bytes of the range that a final sector cut short does not hold are read as zero at
    /// <see cref="CompoundValidationLevel.Minimal" />.
    /// </remarks>
    internal void ReadWithinSector(uint sector, int within, Span<byte> destination)
    {
        long offset = ResolveWithinSector(sector, within, destination.Length, out int present);
        if (present < destination.Length)
            destination.Slice(present).Clear();

        _source.Read(offset, destination.Slice(0, present));
    }

    /// <summary>
    /// Asynchronously reads a sub-range of a single regular sector into the destination.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="within">The byte offset within the sector at which to start.</param>
    /// <param name="destination">
    /// The buffer that receives the bytes; its length must not exceed the remaining sector bytes.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>A task that completes when the bytes have been read.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector begins at or past the end of the data, or, at a level stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />, when the data ends inside the range.
    /// </exception>
    /// <remarks>
    /// The bytes of the range that a final sector cut short does not hold are read as zero at
    /// <see cref="CompoundValidationLevel.Minimal" />.
    /// </remarks>
    internal ValueTask ReadWithinSectorAsync(uint sector, int within, Memory<byte> destination, CancellationToken cancellationToken)
    {
        long offset = ResolveWithinSector(sector, within, destination.Length, out int present);
        if (present < destination.Length)
            destination.Span.Slice(present).Clear();

        return _source.ReadAsync(offset, destination.Slice(0, present), cancellationToken);
    }

    /// <summary>
    /// Locates a sub-range of a regular sector and measures how much of it the data holds.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="within">The byte offset within the sector at which the range starts.</param>
    /// <param name="length">The length of the range.</param>
    /// <param name="present">Receives the number of bytes of the range that the data holds.</param>
    /// <returns>The offset of the range in the data.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector begins at or past the end of the data, or, at a level stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />, when the data ends inside the range.
    /// </exception>
    private long ResolveWithinSector(uint sector, int within, int length, out int present)
    {
        long start = ((long)sector + 1) * _header.SectorSize;
        CompoundThrowHelper.ThrowFormatIf(
            start >= _source.Length,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.SectorOutOfRange);

        long offset = start + within;
        present = (int)Math.Clamp(_source.Length - offset, 0, length);
        _ = MissingDeclaredBytes(present < length);
        return offset;
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

        byte[] miniFatBytes = ReadChain(
            _header.FirstMiniFatSector,
            (long)_header.MiniFatSectorCount * _header.SectorSize,
            fill: 0xFF,
            rangeError: CompoundFileError.InvalidMiniFat);
        uint[] miniFat = new uint[miniFatBytes.Length / sizeof(uint)];
        for (int i = 0; i < miniFat.Length; i++)
            miniFat[i] = BinaryPrimitives.ReadUInt32LittleEndian(miniFatBytes.AsSpan(i * sizeof(uint)));

        _miniFat = miniFat;
    }

    /// <summary>
    /// Reads every byte of a regular-sector chain, following the FAT until the end-of-chain sentinel.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <returns>
    /// The concatenated bytes of every sector in the chain, with zeros for the bytes of a final sector the file cuts
    /// short; empty when the chain is empty.
    /// </returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular or references an out-of-range sector.
    /// </exception>
    internal byte[] ReadChainToEnd(uint startSector) =>
        ReadChainToEnd(startSector, out _, out _);

    /// <summary>
    /// Reads every byte of a regular-sector chain, following the FAT until the end-of-chain sentinel, and reports the
    /// bytes of a final sector that the file cuts short.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <param name="missingStart">
    /// Receives the offset, in the result, of the bytes of a final sector that the file cuts short, which read as zero.
    /// </param>
    /// <param name="missingLength">
    /// Receives the number of those bytes; zero when the file holds every sector whole.
    /// </param>
    /// <returns>The concatenated bytes of every sector in the chain; empty when the chain is empty.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is circular or references an out-of-range sector.
    /// </exception>
    /// <remarks>
    /// Only the file's final sector can be cut short, so at most one range of a chain is missing; the caller decides
    /// whether a structure it needs lies in it.
    /// </remarks>
    internal byte[] ReadChainToEnd(uint startSector, out long missingStart, out int missingLength)
    {
        missingStart = 0;
        missingLength = 0;
        if (startSector == CfbHeader.EndOfChain)
            return [];

        using MemoryStream buffer = new();
        Span<byte> scratch = stackalloc byte[_header.SectorSize];

        foreach (uint sector in WalkChain(startSector, _fat, CompoundFileError.FatCycle, CompoundFileError.SectorOutOfRange))
        {
            ReadOnlySpan<byte> bytes = ReadSector(sector, scratch);
            buffer.Write(bytes);
            if (bytes.Length < _header.SectorSize)
            {
                missingStart = buffer.Length;
                missingLength = _header.SectorSize - bytes.Length;
                buffer.Write(new byte[missingLength]);
            }
        }

        return buffer.ToArray();
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

        size = BoundPaddedSize(size);

        if (_miniFat is null || _miniStream is null)
        {
            if (StopOrThrow(true, CompoundFileError.InvalidMiniFat))
                return new byte[size];
        }

        using MemoryStream buffer = new();

        foreach (uint sector in WalkChain(startMiniSector, _miniFat!, CompoundFileError.InvalidMiniFat, CompoundFileError.InvalidMiniFat))
        {
            // Compute the mini-stream offset in a wider type so a large (corrupt) sector id cannot overflow the
            // int arithmetic before the bounds check runs.
            long offset = (long)sector * _header.MiniSectorSize;
            if (StopOrThrow(offset + _header.MiniSectorSize > _miniStream!.Length, CompoundFileError.InvalidMiniFat))
                break;

            buffer.Write(_miniStream.AsSpan((int)offset, _header.MiniSectorSize));
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
    /// Returns a span over the bytes of a single regular sector that the data holds.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="scratch">
    /// A buffer of at least one sector used by a streaming source; ignored by an in-memory source.
    /// </param>
    /// <returns>
    /// A span over the sector's bytes: all of them, or, for a final sector the file cuts short, those before the end of
    /// the data.
    /// </returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector begins at or past the end of the data.
    /// </exception>
    private ReadOnlySpan<byte> ReadSector(uint sector, Span<byte> scratch)
    {
        long offset = ((long)sector + 1) * _header.SectorSize;
        CompoundThrowHelper.ThrowFormatIf(
            offset >= _source.Length,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.SectorOutOfRange);

        return _source.GetSpan(offset, (int)Math.Min(_header.SectorSize, _source.Length - offset), scratch);
    }

    /// <summary>
    /// Returns the bytes of a FAT or DIFAT sector, all of which the sector declares.
    /// </summary>
    /// <param name="sector">The sector identifier.</param>
    /// <param name="scratch">A buffer of at least one sector.</param>
    /// <returns>A span over the whole sector.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the sector begins at or past the end of the data, or, at a level stricter than
    /// <see cref="CompoundValidationLevel.Minimal" />, when the data ends inside it.
    /// </exception>
    /// <remarks>
    /// At <see cref="CompoundValidationLevel.Minimal" /> the entries of a sector the file cuts short read as FREESECT.
    /// </remarks>
    private ReadOnlySpan<byte> ReadTableSector(uint sector, Span<byte> scratch)
    {
        ReadOnlySpan<byte> bytes = ReadSector(sector, scratch);
        if (!MissingDeclaredBytes(bytes.Length < _header.SectorSize))
            return bytes;

        Span<byte> whole = scratch.Slice(0, _header.SectorSize);
        bytes.CopyTo(whole);
        whole.Slice(bytes.Length).Fill(0xFF);
        return whole;
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
            _fatSectorIds.Add(id);
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

        // A Number of DIFAT Sectors of zero means there is no DIFAT chain, whatever the First DIFAT Sector Location holds.
        uint difatSector = _header.DifatSectorCount == 0 ? CfbHeader.EndOfChain : _header.FirstDifatSector;
        int perSector = _header.EntriesPerSector;
        int guard = 0;
        Span<byte> scratch = stackalloc byte[_header.SectorSize];

        // [MS-CFB] ends the chain with ENDOFCHAIN; FREESECT and DIFSECT in its place are read as its end too, and
        // ValidateAllocation rejects them at Strict.
        while (!stop && IsRegularSector(difatSector))
        {
            if (StopOrThrow(guard++ > (_source.Length / _header.SectorSize) + 1, CompoundFileError.InvalidDifat))
                break;

            ReadOnlySpan<byte> sector = ReadTableSector(difatSector, scratch);
            _difatSectorIds.Add(difatSector);
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

        _difatTerminator = difatSector;
        uint[] fat = new uint[fatSectors.Count * perSector];
        int index = 0;
        foreach (uint fatSector in fatSectors)
        {
            ReadOnlySpan<byte> sector = ReadTableSector(fatSector, scratch);
            for (int i = 0; i < perSector; i++)
                fat[index++] = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice(i * sizeof(uint)));
        }

        return fat;
    }

    /// <summary>
    /// Verifies the allocation tables against [MS-CFB] as <see cref="CompoundValidationLevel.Strict" /> requires,
    /// beyond what the chains the reader follows need.
    /// </summary>
    /// <param name="directory">The parsed directory, whose stream entries own the chains walked.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown with <see cref="CompoundFileError.InvalidDifat" /> when the header's Number of FAT Sectors or Number of
    /// DIFAT Sectors disagrees with the tables, a FAT or DIFAT sector is not marked as one in the FAT, or the DIFAT
    /// chain does not end in ENDOFCHAIN; with <see cref="CompoundFileError.SectorOutOfRange" /> when the FAT allocates
    /// a sector past the end of the file; with <see cref="CompoundFileError.FatCycle" /> when two chains share a
    /// sector; and with <see cref="CompoundFileError.InvalidMiniFat" /> when two mini chains share a mini sector.
    /// </exception>
    /// <remarks>
    /// Every chain is walked once, the directory, the mini FAT, the mini stream and each stream's own, with one set of
    /// used sectors per allocation table, so the cost is linear in the size of the file.
    /// </remarks>
    internal void ValidateAllocation(CfbDirectory directory)
    {
        CompoundThrowHelper.ThrowFormatIf(
            _fatSectorIds.Count != _header.FatSectorCount || _difatSectorIds.Count != _header.DifatSectorCount,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.InvalidDifat);

        foreach (uint id in _fatSectorIds)
            ThrowIfNotMarked(id, CfbHeader.FatSector);

        foreach (uint id in _difatSectorIds)
            ThrowIfNotMarked(id, CfbHeader.DifatSector);

        CompoundThrowHelper.ThrowFormatIf(
            _difatSectorIds.Count > 0 && _difatTerminator != CfbHeader.EndOfChain,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.InvalidDifat);

        // A sector whose first byte lies at or past the end of the file must be free.
        long sectorsInFile = Math.Max(0, ((_source.Length + _header.SectorSize - 1) / _header.SectorSize) - 1);
        for (long sector = sectorsInFile; sector < _fat.Length; sector++)
        {
            CompoundThrowHelper.ThrowFormatIf(
                _fat[sector] != CfbHeader.FreeSector,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
                CompoundFileError.SectorOutOfRange);
        }

        bool[] used = new bool[_fat.Length];
        MarkChain(_header.FirstDirectorySector, _fat, used, CompoundFileError.FatCycle, CompoundFileError.SectorOutOfRange, CompoundFileError.FatCycle);
        MarkChain(_header.FirstMiniFatSector, _fat, used, CompoundFileError.FatCycle, CompoundFileError.InvalidMiniFat, CompoundFileError.FatCycle);
        if (directory.Root.Size > 0)
            MarkChain(directory.Root.StartSector, _fat, used, CompoundFileError.FatCycle, CompoundFileError.SectorOutOfRange, CompoundFileError.FatCycle);

        bool[] miniUsed = new bool[_miniFat?.Length ?? 0];
        foreach (CfbDirectoryEntry entry in directory.AllocatedEntries)
        {
            if (entry.Type != CompoundEntryType.Stream || entry.Size == 0)
                continue;

            if (entry.Size >= CfbHeader.MiniStreamCutoff)
                MarkChain(entry.StartSector, _fat, used, CompoundFileError.FatCycle, CompoundFileError.SectorOutOfRange, CompoundFileError.FatCycle);
            else if (_miniFat is not null)
                MarkChain(entry.StartSector, _miniFat, miniUsed, CompoundFileError.InvalidMiniFat, CompoundFileError.InvalidMiniFat, CompoundFileError.InvalidMiniFat);
        }
    }

    /// <summary>
    /// Throws when the FAT does not mark a sector with the expected special value.
    /// </summary>
    /// <param name="sector">The sector.</param>
    /// <param name="mark">The value the FAT must hold for it.</param>
    /// <exception cref="CompoundFileFormatException">Thrown when the FAT holds another value.</exception>
    private void ThrowIfNotMarked(uint sector, uint mark) =>
        CompoundThrowHelper.ThrowFormatIf(
            sector >= (uint)_fat.Length || _fat[sector] != mark,
            CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
            CompoundFileError.InvalidDifat);

    /// <summary>
    /// Walks a chain and marks its sectors used, rejecting a sector another chain has already used.
    /// </summary>
    /// <param name="startSector">The first sector of the chain.</param>
    /// <param name="table">The allocation table that links the chain.</param>
    /// <param name="used">The sectors of the table that earlier chains used.</param>
    /// <param name="cycleError">The failure category of a cycle in the chain.</param>
    /// <param name="rangeError">The failure category of a chain that leaves the table.</param>
    /// <param name="sharedError">The failure category of a sector another chain used.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the chain is cyclic, leaves the table, or reaches a sector another chain used.
    /// </exception>
    private void MarkChain(uint startSector, uint[] table, bool[] used, CompoundFileError cycleError, CompoundFileError rangeError, CompoundFileError sharedError)
    {
        foreach (uint sector in WalkChain(startSector, table, cycleError, rangeError))
        {
            CompoundThrowHelper.ThrowFormatIf(used[sector], CompoundResourceStrings.Format_Invalid_CompoundSectorChain, sharedError);
            used[sector] = true;
        }
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
