// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Represents the parsed 512-byte header of an OLE2 / Compound File Binary container and the sector-layout constants
/// derived from it.
/// </summary>
/// <remarks>
/// The header fixes the sizes and entry points the rest of the reader depends on: the regular and mini sector sizes,
/// the location of the file-allocation table (FAT) via the double-indirect FAT (DIFAT), the first directory sector, and
/// the mini-FAT chain. Field offsets follow the specification exactly.
/// </remarks>
internal sealed class CompoundFileHeader
{
    /// <summary>The sentinel marking the end of a sector chain.</summary>
    internal const uint EndOfChain = 0xFFFFFFFE;

    /// <summary>The sentinel marking a free (unallocated) sector.</summary>
    internal const uint FreeSector = 0xFFFFFFFF;

    /// <summary>The sentinel marking a sector that holds part of the FAT.</summary>
    internal const uint FatSector = 0xFFFFFFFD;

    /// <summary>The sentinel marking a sector that holds part of the DIFAT.</summary>
    internal const uint DifatSector = 0xFFFFFFFC;

    /// <summary>The sentinel stored in a directory entry's sibling/child slots when there is no such entry.</summary>
    internal const uint NoStream = 0xFFFFFFFF;

    /// <summary>The number of DIFAT entries stored inline in the header.</summary>
    internal const int HeaderDifatCount = 109;

    /// <summary>The eight-byte signature that identifies a compound file.</summary>
    internal static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileHeader" /> class.
    /// </summary>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    /// <param name="miniSectorSize">The mini sector size, in bytes.</param>
    /// <param name="miniStreamCutoff">The maximum stream size, in bytes, that is stored in the mini stream.</param>
    /// <param name="fatSectorCount">The number of sectors occupied by the FAT.</param>
    /// <param name="firstDirectorySector">The first sector of the directory chain.</param>
    /// <param name="firstMiniFatSector">The first sector of the mini-FAT chain.</param>
    /// <param name="miniFatSectorCount">The number of sectors occupied by the mini-FAT.</param>
    /// <param name="firstDifatSector">
    /// The first sector of the DIFAT chain, or <see cref="EndOfChain" /> when none.
    /// </param>
    /// <param name="difatSectorCount">The number of sectors occupied by the DIFAT chain.</param>
    /// <param name="difat">The inline DIFAT entries from the header.</param>
    private CompoundFileHeader(
        int sectorSize,
        int miniSectorSize,
        uint miniStreamCutoff,
        uint fatSectorCount,
        uint firstDirectorySector,
        uint firstMiniFatSector,
        uint miniFatSectorCount,
        uint firstDifatSector,
        uint difatSectorCount,
        uint[] difat)
    {
        SectorSize = sectorSize;
        MiniSectorSize = miniSectorSize;
        MiniStreamCutoff = miniStreamCutoff;
        FatSectorCount = fatSectorCount;
        FirstDirectorySector = firstDirectorySector;
        FirstMiniFatSector = firstMiniFatSector;
        MiniFatSectorCount = miniFatSectorCount;
        FirstDifatSector = firstDifatSector;
        DifatSectorCount = difatSectorCount;
        Difat = difat;
    }

    /// <summary>
    /// Gets the regular sector size, in bytes (512 for version 3, 4096 for version 4).
    /// </summary>
    /// <returns>The regular sector size.</returns>
    internal int SectorSize { get; }

    /// <summary>
    /// Gets the mini sector size, in bytes (typically 64).
    /// </summary>
    /// <returns>The mini sector size.</returns>
    internal int MiniSectorSize { get; }

    /// <summary>
    /// Gets the maximum stream size, in bytes, that is stored within the mini stream rather than the FAT.
    /// </summary>
    /// <returns>The mini-stream cutoff (typically 4096).</returns>
    internal uint MiniStreamCutoff { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the FAT.
    /// </summary>
    /// <returns>The FAT sector count.</returns>
    internal uint FatSectorCount { get; }

    /// <summary>
    /// Gets the first sector of the directory chain.
    /// </summary>
    /// <returns>The first directory sector identifier.</returns>
    internal uint FirstDirectorySector { get; }

    /// <summary>
    /// Gets the first sector of the mini-FAT chain.
    /// </summary>
    /// <returns>The first mini-FAT sector identifier, or <see cref="EndOfChain" /> when there is no mini-FAT.</returns>
    internal uint FirstMiniFatSector { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the mini-FAT.
    /// </summary>
    /// <returns>The mini-FAT sector count.</returns>
    internal uint MiniFatSectorCount { get; }

    /// <summary>
    /// Gets the first sector of the DIFAT chain.
    /// </summary>
    /// <returns>
    /// The first DIFAT sector identifier, or <see cref="EndOfChain" /> when the DIFAT fits in the header.
    /// </returns>
    internal uint FirstDifatSector { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the DIFAT chain.
    /// </summary>
    /// <returns>The DIFAT sector count.</returns>
    internal uint DifatSectorCount { get; }

    /// <summary>
    /// Gets the inline DIFAT entries stored in the header.
    /// </summary>
    /// <returns>
    /// An array of <see cref="HeaderDifatCount" /> sector identifiers, each pointing at a FAT sector or set to
    /// <see cref="FreeSector" />.
    /// </returns>
    internal uint[] Difat { get; }

    /// <summary>
    /// Gets the number of 32-bit entries that fit in a single regular sector.
    /// </summary>
    /// <returns>The count of <see cref="uint" /> slots per sector.</returns>
    internal int EntriesPerSector => SectorSize / sizeof(uint);

    /// <summary>
    /// Parses the compound-file header from the start of the supplied data.
    /// </summary>
    /// <param name="data">The full compound-file byte content; must be at least 512 bytes.</param>
    /// <returns>The parsed <see cref="CompoundFileHeader" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is too short, the signature is invalid, or the header declares an unsupported layout.
    /// </exception>
    internal static CompoundFileHeader Parse(ReadOnlySpan<byte> data)
    {
        CompoundThrowHelper.ThrowFormatIf(data.Length < 512, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.TruncatedFile);

        if (!data.Slice(0, Signature.Length).SequenceEqual(Signature))
            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundSignature, CompoundFileError.InvalidSignature);

        CompoundStreamReader reader = new(data);
        reader.Seek(28);
        ushort byteOrder = reader.ReadUInt16();
        ushort sectorShift = reader.ReadUInt16();
        ushort miniSectorShift = reader.ReadUInt16();

        CompoundThrowHelper.ThrowFormatIf(byteOrder != 0xFFFE, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidByteOrder);
        CompoundThrowHelper.ThrowFormatIf(sectorShift is not 9 and not 12, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidSectorSize);
        CompoundThrowHelper.ThrowFormatIf(miniSectorShift != 6, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidSectorSize);

        reader.Seek(44);
        uint fatSectorCount = reader.ReadUInt32();
        uint firstDirectorySector = reader.ReadUInt32();
        reader.Skip(4); // transaction signature
        uint miniStreamCutoff = reader.ReadUInt32();
        uint firstMiniFatSector = reader.ReadUInt32();
        uint miniFatSectorCount = reader.ReadUInt32();
        uint firstDifatSector = reader.ReadUInt32();
        uint difatSectorCount = reader.ReadUInt32();

        uint[] difat = new uint[HeaderDifatCount];
        for (int i = 0; i < HeaderDifatCount; i++)
            difat[i] = reader.ReadUInt32();

        return new CompoundFileHeader(
            1 << sectorShift,
            1 << miniSectorShift,
            miniStreamCutoff,
            fatSectorCount,
            firstDirectorySector,
            firstMiniFatSector,
            miniFatSectorCount,
            firstDifatSector,
            difatSectorCount,
            difat);
    }
}
