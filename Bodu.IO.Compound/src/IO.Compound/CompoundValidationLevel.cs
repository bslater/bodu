// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundValidationLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies how strictly the compound-file reader validates a container's structure.
/// </summary>
/// <remarks>
/// All levels enforce the memory-safety invariants required to parse a file without faulting - the signature,
/// byte-order marker, sector sizes, allocation-table bounds, and the presence of a root storage are always validated,
/// and a sector that begins at or past the end of the file is <see cref="CompoundFileError.SectorOutOfRange" />. The
/// level controls only how the reader responds to the remaining, recoverable inconsistencies. A file may end inside its
/// final sector, as [MS-CFB] 2.3 allows, when it holds every byte a structure there declares; a declared byte it lacks
/// is <see cref="CompoundFileError.TruncatedFile" /> at <see cref="Strict" /> and <see cref="Compatible" />, and reads
/// as zero at <see cref="Minimal" />, or, in an allocation table, as FREESECT. Every level reads a stream smaller than
/// 4096 bytes from the mini stream, the cutoff [MS-CFB] fixes, whatever cutoff the header declares. Below
/// <see cref="Strict" />, a directory entry whose name length is odd or above 64, or whose object type [MS-CFB] does
/// not define, is malformed and skipped, while a name that lacks its terminating null is read up to its declared
/// length, and the header's Number of FAT Sectors and Number of DIFAT Sectors are ignored in favour of the DIFAT
/// itself. At every level a Number of DIFAT Sectors of zero means there is no DIFAT chain, whatever the First DIFAT
/// Sector Location holds.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Salvage what a damaged legacy file still holds instead of rejecting it outright.
/// using var salvage = CompoundFile.OpenRead("damaged.doc", new CompoundFileOptions
/// {
///     ValidationLevel = CompoundValidationLevel.Minimal,
/// });
///
/// // Conformance tooling wants the opposite: fail on anything non-conformant.
/// var strict = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };
///]]>
/// </code>
/// </example>
/// </remarks>
public enum CompoundValidationLevel
{
    /// <summary>
    /// Rejects every non-conformant condition, including directory entries with an invalid name length or entry type
    /// that more lenient levels silently skip.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The header must hold the values [MS-CFB] fixes, or the file is rejected with
    /// <see cref="CompoundFileError.InvalidHeader" />: a zero class identifier and zero reserved bytes, major version 3
    /// with 512-byte sectors or 4 with 4096-byte sectors, no directory sectors counted in version 3 and the length of
    /// the directory chain counted in version 4, and a mini stream cutoff of 4096.
    /// </para>
    /// <para>
    /// The allocation tables are checked whole, not only where a chain the reader follows needs them: the header's
    /// Number of FAT Sectors and Number of DIFAT Sectors must count the FAT sectors the DIFAT lists and the sectors of
    /// the DIFAT chain, every FAT and DIFAT sector must be marked as one in the FAT and the DIFAT chain must end in
    /// ENDOFCHAIN (<see cref="CompoundFileError.InvalidDifat" />), every FAT entry for a sector past the end of the
    /// file must be free (<see cref="CompoundFileError.SectorOutOfRange" />), and no sector may belong to two chains (<see cref="CompoundFileError.FatCycle" />,
    /// or <see cref="CompoundFileError.InvalidMiniFat" /> for a mini sector). The other levels read a DIFAT chain that
    /// ends in FREESECT or DIFSECT as ending there.
    /// </para>
    /// <para>
    /// Each directory entry must meet [MS-CFB] 2.6, or the file is rejected with
    /// <see cref="CompoundFileError.InvalidDirectory" />: its name length is even and counts a terminating null that is
    /// present, the root is named <c>Root Entry</c> and records no creation time, a stream records no class identifier
    /// and no times, a storage records no starting sector, and no red node of a sibling tree links to another red node.
    /// </para>
    /// </remarks>
    Strict,

    /// <summary>
    /// The default: rejects structural corruption (allocation-table cycles, out-of-range or short sector chains, and
    /// cyclic directory links) while tolerating individually malformed directory entries by skipping them, and pruning
    /// the link that reaches one, with the entries beneath it.
    /// </summary>
    Compatible,

    /// <summary>
    /// Recovers from structural corruption where it can: cyclic, out-of-range, or short sector chains and cyclic
    /// directory links stop and yield the data collected so far instead of throwing.
    /// </summary>
    Minimal,
}
