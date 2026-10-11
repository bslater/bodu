// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Represents the parsed 512-byte header of an OLE2 / Compound File Binary container and the sector-layout constants
/// derived from it.
/// </summary>
/// <remarks>
/// The header fixes the sizes and entry points the rest of the reader depends on: the regular and mini sector sizes,
/// the location of the file-allocation table (FAT) via the double-indirect FAT (DIFAT), the first directory sector, and
/// the mini-FAT chain. Field offsets follow the specification exactly.
/// </remarks>
internal sealed class CfbHeader
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

    /// <summary>The size, in bytes, below which a stream is stored in the mini stream; [MS-CFB] fixes it at 4096.</summary>
    internal const uint MiniStreamCutoff = 4096;

    /// <summary>The eight-byte signature that identifies a compound file.</summary>
    internal static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbHeader" /> class.
    /// </summary>
    /// <param name="majorVersion">The major version the header declares.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    /// <param name="miniSectorSize">The mini sector size, in bytes.</param>
    /// <param name="directorySectorCount">The number of directory sectors the header declares.</param>
    /// <param name="fatSectorCount">The number of sectors occupied by the FAT.</param>
    /// <param name="firstDirectorySector">The first sector of the directory chain.</param>
    /// <param name="firstMiniFatSector">The first sector of the mini-FAT chain.</param>
    /// <param name="miniFatSectorCount">The number of sectors occupied by the mini-FAT.</param>
    /// <param name="firstDifatSector">
    /// The first sector of the DIFAT chain, or <see cref="EndOfChain" /> when none.
    /// </param>
    /// <param name="difatSectorCount">The number of sectors occupied by the DIFAT chain.</param>
    /// <param name="difat">The inline DIFAT entries from the header.</param>
    private CfbHeader(
        ushort majorVersion,
        int sectorSize,
        int miniSectorSize,
        uint directorySectorCount,
        uint fatSectorCount,
        uint firstDirectorySector,
        uint firstMiniFatSector,
        uint miniFatSectorCount,
        uint firstDifatSector,
        uint difatSectorCount,
        uint[] difat)
    {
        MajorVersion = majorVersion;
        SectorSize = sectorSize;
        MiniSectorSize = miniSectorSize;
        DirectorySectorCount = directorySectorCount;
        FatSectorCount = fatSectorCount;
        FirstDirectorySector = firstDirectorySector;
        FirstMiniFatSector = firstMiniFatSector;
        MiniFatSectorCount = miniFatSectorCount;
        FirstDifatSector = firstDifatSector;
        DifatSectorCount = difatSectorCount;
        Difat = difat;
    }

    /// <summary>
    /// Gets the major version the header declares (3 or 4 in a conformant file).
    /// </summary>
    /// <value>The major version.</value>
    internal ushort MajorVersion { get; }

    /// <summary>
    /// Gets the regular sector size, in bytes (512 for version 3, 4096 for version 4).
    /// </summary>
    /// <value>The regular sector size.</value>
    internal int SectorSize { get; }

    /// <summary>
    /// Gets the mini sector size, in bytes (typically 64).
    /// </summary>
    /// <value>The mini sector size.</value>
    internal int MiniSectorSize { get; }

    /// <summary>
    /// Gets the number of directory sectors the header declares.
    /// </summary>
    /// <value>Zero in a conformant version-3 file; the length of the directory chain in a version-4 file.</value>
    internal uint DirectorySectorCount { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the FAT.
    /// </summary>
    /// <value>The FAT sector count.</value>
    internal uint FatSectorCount { get; }

    /// <summary>
    /// Gets the first sector of the directory chain.
    /// </summary>
    /// <value>The first directory sector identifier.</value>
    internal uint FirstDirectorySector { get; }

    /// <summary>
    /// Gets the first sector of the mini-FAT chain.
    /// </summary>
    /// <value>The first mini-FAT sector identifier, or <see cref="EndOfChain" /> when there is no mini-FAT.</value>
    internal uint FirstMiniFatSector { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the mini-FAT.
    /// </summary>
    /// <value>The mini-FAT sector count.</value>
    internal uint MiniFatSectorCount { get; }

    /// <summary>
    /// Gets the first sector of the DIFAT chain.
    /// </summary>
    /// <value>
    /// The first DIFAT sector identifier, or <see cref="EndOfChain" /> when the DIFAT fits in the header.
    /// </value>
    internal uint FirstDifatSector { get; }

    /// <summary>
    /// Gets the number of sectors occupied by the DIFAT chain.
    /// </summary>
    /// <value>The DIFAT sector count.</value>
    internal uint DifatSectorCount { get; }

    /// <summary>
    /// Gets the inline DIFAT entries stored in the header.
    /// </summary>
    /// <value>
    /// An array of <see cref="HeaderDifatCount" /> sector identifiers, each pointing at a FAT sector or set to
    /// <see cref="FreeSector" />.
    /// </value>
    internal uint[] Difat { get; }

    /// <summary>
    /// Gets the number of 32-bit entries that fit in a single regular sector.
    /// </summary>
    /// <value>The count of <see cref="uint" /> slots per sector.</value>
    internal int EntriesPerSector => SectorSize / sizeof(uint);

    /// <summary>
    /// Parses the compound-file header from the start of the supplied data.
    /// </summary>
    /// <param name="data">The full compound-file byte content; must be at least 512 bytes.</param>
    /// <param name="level">
    /// The validation level; <see cref="CompoundValidationLevel.Strict" /> also checks the fields [MS-CFB] fixes.
    /// </param>
    /// <returns>The parsed <see cref="CfbHeader" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is too short, the signature is invalid, or the header declares an unsupported layout; at
    /// <see cref="CompoundValidationLevel.Strict" />, also when a field [MS-CFB] fixes holds another value.
    /// </exception>
    /// <remarks>
    /// The mini stream cutoff the header declares is checked at <see cref="CompoundValidationLevel.Strict" /> and
    /// otherwise ignored: the reader always uses <see cref="MiniStreamCutoff" />, the only value [MS-CFB] permits.
    /// </remarks>
    internal static CfbHeader Parse(ReadOnlySpan<byte> data, CompoundValidationLevel level = CompoundValidationLevel.Compatible)
    {
        CompoundThrowHelper.ThrowFormatIf(data.Length < 512, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.TruncatedFile);

        if (!data.Slice(0, Signature.Length).SequenceEqual(Signature))
            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundSignature, CompoundFileError.InvalidSignature);

        CfbBinaryReader reader = new(data);
        reader.Seek(26);
        ushort majorVersion = reader.ReadUInt16();
        ushort byteOrder = reader.ReadUInt16();
        ushort sectorShift = reader.ReadUInt16();
        ushort miniSectorShift = reader.ReadUInt16();

        CompoundThrowHelper.ThrowFormatIf(byteOrder != 0xFFFE, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidByteOrder);
        CompoundThrowHelper.ThrowFormatIf(sectorShift is not 9 and not 12, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidSectorSize);
        CompoundThrowHelper.ThrowFormatIf(miniSectorShift != 6, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidSectorSize);

        reader.Seek(40);
        uint directorySectorCount = reader.ReadUInt32();
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

        if (level == CompoundValidationLevel.Strict)
            ValidateFixedFields(data, majorVersion, sectorShift, directorySectorCount, miniStreamCutoff);

        return new CfbHeader(
            majorVersion,
            1 << sectorShift,
            1 << miniSectorShift,
            directorySectorCount,
            fatSectorCount,
            firstDirectorySector,
            firstMiniFatSector,
            miniFatSectorCount,
            firstDifatSector,
            difatSectorCount,
            difat);
    }

    /// <summary>
    /// Verifies that a version-4 header counts the sectors of the directory chain, as [MS-CFB] 2.2 requires.
    /// </summary>
    /// <param name="directorySectors">The number of sectors in the directory chain.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when a version-4 header declares another number.
    /// </exception>
    internal void ValidateDirectorySectorCount(long directorySectors) =>
        CompoundThrowHelper.ThrowFormatIf(
            MajorVersion == 4 && DirectorySectorCount != directorySectors,
            CompoundResourceStrings.Format_Invalid_CompoundHeader,
            CompoundFileError.InvalidHeader);

    /// <summary>
    /// Verifies the header fields that [MS-CFB] 2.2 fixes.
    /// </summary>
    /// <param name="data">The header bytes.</param>
    /// <param name="majorVersion">The major version the header declares.</param>
    /// <param name="sectorShift">The sector shift the header declares.</param>
    /// <param name="directorySectorCount">The number of directory sectors the header declares.</param>
    /// <param name="miniStreamCutoff">The mini stream cutoff the header declares.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when any of the fields holds a value [MS-CFB] forbids.
    /// </exception>
    private static void ValidateFixedFields(
        ReadOnlySpan<byte> data, ushort majorVersion, ushort sectorShift, uint directorySectorCount, uint miniStreamCutoff)
    {
        // The header CLSID (offset 8) and the six reserved bytes (offset 34) are zero; version 3 uses 512-byte sectors
        // and counts no directory sectors, version 4 uses 4096-byte sectors; the mini stream cutoff is 4096.
        bool conformant = !data.Slice(8, 16).ContainsAnyExcept((byte)0)
            && !data.Slice(34, 6).ContainsAnyExcept((byte)0)
            && ((majorVersion == 3 && sectorShift == 9) || (majorVersion == 4 && sectorShift == 12))
            && (majorVersion != 3 || directorySectorCount == 0)
            && miniStreamCutoff == MiniStreamCutoff;

        CompoundThrowHelper.ThrowFormatIf(!conformant, CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidHeader);
    }
}
