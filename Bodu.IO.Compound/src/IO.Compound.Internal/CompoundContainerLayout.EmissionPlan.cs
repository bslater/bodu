// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerLayout.EmissionPlan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Internal;

internal static partial class CompoundContainerLayout
{
    /// <summary>
    /// The precomputed geometry of a compound-file serialization: the directory entries and every sector count and
    /// start index the emit phase needs. Computed once by <see cref="PreparePlan" /> with no byte output, then consumed
    /// by the synchronous and asynchronous emit paths so they can never diverge in layout.
    /// </summary>
    /// <param name="Entries">The directory entries, in stream-identifier order, with sector indices assigned.</param>
    /// <param name="SectorSize">The regular sector size, in bytes.</param>
    /// <param name="EntriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <param name="IsVersion4">Whether the file uses the version-4 (4096-byte sector) layout.</param>
    /// <param name="DirectoryStart">The index of the first directory sector.</param>
    /// <param name="DirectorySectors">The number of directory sectors.</param>
    /// <param name="MiniFatStart">The index of the first mini-FAT sector, or the end-of-chain marker when none.</param>
    /// <param name="MiniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="MiniStreamStart">
    /// The index of the first mini-stream sector, or the end-of-chain marker when none.
    /// </param>
    /// <param name="MiniStreamSectors">The number of mini-stream sectors.</param>
    /// <param name="TotalMiniSectors">The total number of mini sectors.</param>
    /// <param name="DataSectorCount">The number of non-FAT, non-DIFAT sectors.</param>
    /// <param name="FatStart">The index of the first FAT sector.</param>
    /// <param name="FatSectors">The number of FAT sectors.</param>
    /// <param name="DifatStart">The index of the first extended-DIFAT sector.</param>
    /// <param name="DifatSectors">The number of extended-DIFAT sectors.</param>
    private sealed record EmissionPlan(
        List<Entry> Entries,
        int SectorSize,
        int EntriesPerSector,
        bool IsVersion4,
        long DirectoryStart,
        long DirectorySectors,
        uint MiniFatStart,
        long MiniFatSectors,
        uint MiniStreamStart,
        long MiniStreamSectors,
        int TotalMiniSectors,
        long DataSectorCount,
        long FatStart,
        long FatSectors,
        long DifatStart,
        long DifatSectors);
}
