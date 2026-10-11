// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Serializes a mutable compound-file object model into the OLE2 / Compound File Binary byte layout.
/// </summary>
/// <remarks>
/// <para>
/// The layout is computed once into a small plan (the directory entries and the sector geometry), then emitted to the
/// destination one sector at a time. The header, directory, file-allocation table (FAT), mini-FAT, and double-indirect
/// FAT (DIFAT) are generated on the fly, and stream payloads are streamed directly from their sources, so peak memory
/// is proportional to the number of directory entries plus a single sector - independent of the output size.
/// </para>
/// <para>
/// Streams are partitioned into the mini stream (small) and the regular sectors, the directory is encoded as a
/// red-black tree per storage, and the FAT/DIFAT sector counts are resolved to a fixed point. The result is
/// byte-compatible with the reader and with other conforming parsers.
/// </para>
/// <para>
/// A version-4 file that passes 2 GB reaches the range lock sector [MS-CFB] 2.8 reserves (sector 0x7FFFE). The sector
/// is written as zeros and marked ENDOFCHAIN in the FAT, and every sector the plan lays out at or past it moves one
/// sector on, so no chain passes through it.
/// </para>
/// </remarks>
internal static partial class CompoundContainerLayout
{
    /// <summary>The fixed size, in bytes, of a directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>The mini sector size, in bytes.</summary>
    private const int MiniSectorSize = 64;

    /// <summary>The maximum stream size, in bytes, stored in the mini stream.</summary>
    private const int MiniStreamCutoff = 4096;

    /// <summary>The minor-version word written to the header.</summary>
    private const ushort MinorVersion = 0x003E;

    /// <summary>The little-endian byte-order marker written to the header.</summary>
    private const ushort ByteOrderMarker = 0xFFFE;

    /// <summary>The buffer size, in bytes, used when copying a deferred stream payload.</summary>
    private const int CopyBufferSize = 81920;

    /// <summary>The maximum stream size, in bytes, that a version-3 compound file can represent.</summary>
    private const long MaxVersion3StreamSize = 0x80000000;

    /// <summary>The largest version-3 file, in bytes: [MS-CFB] 2.9 limits a file of 512-byte sectors to 2 GB.</summary>
    private const long MaxVersion3FileSize = 0x80000000;

    /// <summary>The sector of a file of 4096-byte sectors that covers file offsets 0x7FFFFF00-0x7FFFFFFF, which [MS-CFB] 2.8 reserves as the range lock sector.</summary>
    private const long RangeLockSector = 0x7FFFE;

    /// <summary>The sector the header occupies, one before sector 0.</summary>
    private const long HeaderSector = -1;

    /// <summary>
    /// Serializes the supplied root storage into a compound-file byte array.
    /// </summary>
    /// <param name="root">The root storage to serialize.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    internal static byte[] Write(CompoundStorageBuilder root, CompoundBuildOptions options)
    {
        using MemoryStream buffer = new();
        WriteTo(buffer, root, options);
        return buffer.ToArray();
    }

    /// <summary>
    /// Serializes the supplied root storage to a destination stream, emitting one sector at a time.
    /// </summary>
    /// <param name="destination">The stream that receives the compound-file content.</param>
    /// <param name="root">The root storage to serialize.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    internal static void WriteTo(Stream destination, CompoundStorageBuilder root, CompoundBuildOptions options)
    {
        EmissionPlan plan = PreparePlan(root, options);
        var writer = new SectorWriter(destination, plan, HeaderSector);

        EmitHeader(writer, plan);
        EmitDirectory(writer, plan);
        if (plan.MiniFatSectors > 0)
            EmitMiniFat(writer, plan);

        if (plan.MiniStreamSectors > 0)
        {
            EmitMiniStream(writer, plan.Entries);
            writer.PadToSector();
        }

        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsRegularStream)
            {
                CopyContent(writer, entry);
                writer.WriteZeros((CeilDiv(entry.Size, plan.SectorSize) * plan.SectorSize) - entry.Size);
            }
        }

        EmitFat(writer, plan);

        if (plan.DifatSectors > 0)
            EmitDifat(writer, plan);
    }

    /// <summary>
    /// Writes the allocation tables a tree's container ends with, from its first FAT sector to the end of the file,
    /// without writing the header, the directory or any stream.
    /// </summary>
    /// <param name="destination">The stream that receives the FAT and DIFAT sectors.</param>
    /// <param name="root">The root storage whose container is planned.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <remarks>
    /// A seam for tests: it shows the tables of a container too large to write in a test.
    /// </remarks>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    internal static void WriteAllocationTables(Stream destination, CompoundStorageBuilder root, CompoundBuildOptions options)
    {
        EmissionPlan plan = PreparePlan(root, options);
        var writer = new SectorWriter(destination, plan, plan.ToFileSector(plan.FatStart));

        EmitFat(writer, plan);
        if (plan.DifatSectors > 0)
            EmitDifat(writer, plan);
    }

    /// <summary>
    /// Computes the container geometry - the directory entries and every sector count and start index - without
    /// emitting any bytes, so the synchronous and asynchronous emit paths consume one plan and cannot diverge in
    /// layout.
    /// </summary>
    /// <param name="root">The root storage to serialize.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The precomputed emission plan.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    private static EmissionPlan PreparePlan(CompoundStorageBuilder root, CompoundBuildOptions options)
    {
        int sectorSize = options.SectorSize;
        int entriesPerSector = sectorSize / 4;
        int directoriesPerSector = sectorSize / DirectoryEntrySize;
        bool isVersion4 = options.Version == CompoundFileVersion.V4;

        List<Entry> entries = Flatten(root, options.EffectiveMaxDepth);

        // MS-CFB: a version-3 file stores stream sizes in the low 32 bits, so each stream must be <= 0x80000000.
        if (!isVersion4)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Type == CompoundEntryType.Stream && entry.Size > MaxVersion3StreamSize)
                {
                    throw new CompoundFileSerializationException(
                        string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundV3StreamTooLarge, entry.Name));
                }
            }
        }

        // Mini-stream geometry: assign each small stream its mini-sector start without materializing any bytes.
        int totalMiniSectors = AssignMiniStream(entries);
        long miniStreamBytes = (long)totalMiniSectors * MiniSectorSize;
        long miniFatSectors = totalMiniSectors == 0 ? 0 : CeilDiv(totalMiniSectors, entriesPerSector);
        long miniStreamSectors = CeilDiv(miniStreamBytes, sectorSize);
        long directorySectors = CeilDiv(entries.Count, directoriesPerSector);

        // Assign regular sector indices: directory, mini-FAT, mini-stream, then each large stream.
        long next = 0;
        long directoryStart = next;
        next += directorySectors;
        uint miniFatStart = miniFatSectors > 0 ? (uint)next : CfbHeader.EndOfChain;
        next += miniFatSectors;
        uint miniStreamStart = miniStreamSectors > 0 ? (uint)next : CfbHeader.EndOfChain;
        next += miniStreamSectors;

        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
            {
                entry.StartSector = (uint)next;
                next += CeilDiv(entry.Size, sectorSize);
            }
        }

        long dataSectorCount = next;

        // The root entry owns the mini stream as its regular-sector chain.
        Entry rootEntry = entries[0];
        rootEntry.StartSector = miniStreamStart;
        rootEntry.Size = miniStreamBytes;

        // Resolve the FAT/DIFAT counts to a fixed point (the FAT describes its own and the DIFAT's sectors, and the
        // range lock sector when the file reaches it).
        ResolveFatCounts(dataSectorCount, entriesPerSector, isVersion4, out long fatSectors, out long difatSectors, out bool reservesRangeLock);
        long fatStart = dataSectorCount;
        long difatStart = dataSectorCount + fatSectors;
        long totalSectors = dataSectorCount + fatSectors + difatSectors + (reservesRangeLock ? 1 : 0);
        if (totalSectors >= CfbHeader.DifatSector)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Op_Invalid_CompoundBuilderTooLarge);

        // MS-CFB 2.9: a file of 512-byte sectors, the header sector included, is no greater than 2 GB.
        if (!isVersion4 && (totalSectors + 1) * sectorSize > MaxVersion3FileSize)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Op_Invalid_CompoundV3FileTooLarge);

        return new EmissionPlan(
            entries,
            sectorSize,
            entriesPerSector,
            isVersion4,
            directoryStart,
            directorySectors,
            miniFatStart,
            miniFatSectors,
            miniStreamStart,
            miniStreamSectors,
            totalMiniSectors,
            dataSectorCount,
            fatStart,
            fatSectors,
            difatStart,
            difatSectors,
            reservesRangeLock);
    }

    /// <summary>
    /// Flattens the tree into a stream-identifier-indexed entry list and assigns each storage's red-black child links.
    /// </summary>
    /// <param name="root">The root storage.</param>
    /// <param name="maxDepth">The maximum permitted nesting depth.</param>
    /// <returns>The entry list with the root at index 0.</returns>
    private static List<Entry> Flatten(CompoundStorageBuilder root, int maxDepth)
    {
        List<Entry> entries = new() { Entry.FromNode(root, CompoundEntryType.RootStorage) };
        AssignChildren(root, 0, 1, maxDepth, entries);
        return entries;
    }

    /// <summary>
    /// Assigns stream identifiers to a storage's children and builds its red-black child tree.
    /// </summary>
    /// <param name="storage">The storage whose children are processed.</param>
    /// <param name="sid">The stream identifier of <paramref name="storage" />.</param>
    /// <param name="depth">The depth of the children being assigned (root children are depth 1).</param>
    /// <param name="maxDepth">The maximum permitted nesting depth.</param>
    /// <param name="entries">The entry list being populated.</param>
    /// <exception cref="CompoundFileSerializationException">Thrown when the nesting depth is exceeded.</exception>
    private static void AssignChildren(CompoundStorageBuilder storage, int sid, int depth, int maxDepth, List<Entry> entries)
    {
        var children = storage.Values.ToList();
        if (children.Count == 0)
            return;

        if (depth > maxDepth)
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundBuilderMaxDepthExceeded, maxDepth));
        }

        children.Sort(static (a, b) => CompoundNameComparer.Instance.Compare(a.Name, b.Name));

        List<int> childSids = new(children.Count);
        foreach (CompoundEntryBuilder child in children)
        {
            CompoundEntryType type = child is CompoundStorageBuilder ? CompoundEntryType.Storage : CompoundEntryType.Stream;
            entries.Add(Entry.FromNode(child, type));
            childSids.Add(entries.Count - 1);
        }

        entries[sid].ChildId = BuildTree(entries, childSids, 0, childSids.Count - 1);

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is CompoundStorageBuilder childStorage)
                AssignChildren(childStorage, childSids[i], depth + 1, maxDepth, entries);
        }
    }

    /// <summary>
    /// Builds a balanced binary search tree over the sorted child identifiers, returning the subtree root.
    /// </summary>
    /// <param name="entries">The entry list.</param>
    /// <param name="sids">The sorted child stream identifiers.</param>
    /// <param name="lo">The inclusive lower bound.</param>
    /// <param name="hi">The inclusive upper bound.</param>
    /// <returns>The stream identifier of the subtree root, or <see cref="CfbHeader.NoStream" /> when empty.</returns>
    private static uint BuildTree(List<Entry> entries, List<int> sids, int lo, int hi)
    {
        if (lo > hi)
            return CfbHeader.NoStream;

        int mid = (lo + hi) / 2;
        int sid = sids[mid];
        entries[sid].LeftSibling = BuildTree(entries, sids, lo, mid - 1);
        entries[sid].RightSibling = BuildTree(entries, sids, mid + 1, hi);
        return (uint)sid;
    }

    /// <summary>
    /// Assigns each mini-stream member its starting mini-sector index, returning the total mini-sector count.
    /// </summary>
    /// <param name="entries">The entry list.</param>
    /// <returns>The total number of mini sectors.</returns>
    private static int AssignMiniStream(List<Entry> entries)
    {
        uint miniSector = 0;
        foreach (Entry entry in entries)
        {
            if (!entry.IsMiniStream)
                continue;

            entry.StartSector = miniSector;
            miniSector += (uint)CeilDiv(entry.Size, MiniSectorSize);
        }

        return (int)miniSector;
    }

    /// <summary>
    /// Resolves the FAT and DIFAT sector counts to a fixed point, with whether the file reaches the range lock sector.
    /// </summary>
    /// <param name="dataSectorCount">The number of non-FAT, non-DIFAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <param name="hasRangeLockSector">
    /// Whether the file has 4096-byte sectors, the only ones [MS-CFB] 2.8 gives a range lock sector.
    /// </param>
    /// <param name="fatSectors">The resolved FAT sector count.</param>
    /// <param name="difatSectors">The resolved DIFAT sector count.</param>
    /// <param name="reservesRangeLock">
    /// Whether the sectors would reach the range lock sector, which is then reserved and counted among them.
    /// </param>
    private static void ResolveFatCounts(long dataSectorCount, int entriesPerSector, bool hasRangeLockSector, out long fatSectors, out long difatSectors, out bool reservesRangeLock)
    {
        fatSectors = 0;
        difatSectors = 0;
        while (true)
        {
            // The sectors are laid out one after another; once they would reach the range lock sector, it is skipped,
            // and so takes one more entry of the FAT.
            long sequential = dataSectorCount + fatSectors + difatSectors;
            reservesRangeLock = hasRangeLockSector && sequential > RangeLockSector;
            long total = sequential + (reservesRangeLock ? 1 : 0);
            long newFat = CeilDiv(total, entriesPerSector);
            long newDifat = newFat <= CfbHeader.HeaderDifatCount
                ? 0
                : CeilDiv(newFat - CfbHeader.HeaderDifatCount, entriesPerSector - 1);

            if (newFat == fatSectors && newDifat == difatSectors)
                return;

            fatSectors = newFat;
            difatSectors = newDifat;
        }
    }

    /// <summary>
    /// Builds the 512-byte header from the plan into the supplied buffer.
    /// </summary>
    /// <param name="plan">The emission plan.</param>
    /// <param name="h">The 512-byte destination span.</param>
    private static void BuildHeader(EmissionPlan plan, Span<byte> h)
    {
        h.Clear();
        CfbHeader.Signature.CopyTo(h);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(24), MinorVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(26), (ushort)(plan.IsVersion4 ? 4 : 3));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(28), ByteOrderMarker);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(30), (ushort)(plan.IsVersion4 ? 12 : 9));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(32), 6);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(40), plan.IsVersion4 ? (uint)plan.DirectorySectors : 0);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(44), (uint)plan.FatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(48), plan.ToFileSector(plan.DirectoryStart));
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(56), MiniStreamCutoff);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(60), plan.MiniFatSectors > 0 ? plan.ToFileSector(plan.MiniFatStart) : CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(64), (uint)plan.MiniFatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(68), plan.DifatSectors > 0 ? plan.ToFileSector(plan.DifatStart) : CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(72), (uint)plan.DifatSectors);

        long inlineCount = Math.Min(plan.FatSectors, CfbHeader.HeaderDifatCount);
        for (int i = 0; i < CfbHeader.HeaderDifatCount; i++)
        {
            uint value = i < inlineCount ? plan.ToFileSector(plan.FatStart + i) : CfbHeader.FreeSector;
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(76 + (i * 4)), value);
        }
    }

    /// <summary>
    /// Emits the header sector: the 512-byte header followed by zero padding to the sector boundary.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    private static void EmitHeader(SectorWriter writer, EmissionPlan plan)
    {
        Span<byte> h = stackalloc byte[512];
        BuildHeader(plan, h);

        writer.WriteBytes(h);
        writer.PadToSector();
    }

    /// <summary>
    /// Emits the directory sectors, encoding each entry and filling the rest of the final sector with free entries.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan, whose entries are in stream-identifier order.</param>
    private static void EmitDirectory(SectorWriter writer, EmissionPlan plan)
    {
        Span<byte> record = stackalloc byte[DirectoryEntrySize];
        foreach (Entry entry in plan.Entries)
        {
            record.Clear();
            entry.Encode(record, plan);
            writer.WriteBytes(record);
        }

        EncodeFreeEntry(record);
        for (long slot = plan.Entries.Count; slot < DirectorySlots(plan); slot++)
            writer.WriteBytes(record);
    }

    /// <summary>
    /// Returns the number of entry slots the directory sectors hold.
    /// </summary>
    /// <param name="plan">The emission plan.</param>
    /// <returns>The directory sector count times the entries a sector holds.</returns>
    private static long DirectorySlots(EmissionPlan plan) =>
        plan.DirectorySectors * (plan.SectorSize / DirectoryEntrySize);

    /// <summary>
    /// Encodes a free directory entry, which [MS-CFB] 2.6.3 defines as all zeroes except the left sibling, right
    /// sibling and child links, which are NOSTREAM.
    /// </summary>
    /// <param name="record">The 128-byte entry to fill.</param>
    private static void EncodeFreeEntry(Span<byte> record)
    {
        record.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(68), CfbHeader.NoStream);
        BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(72), CfbHeader.NoStream);
        BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(76), CfbHeader.NoStream);
    }

    /// <summary>
    /// Emits the mini-FAT sectors, chaining each mini-stream member and padding with free markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    private static void EmitMiniFat(SectorWriter writer, EmissionPlan plan)
    {
        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsMiniStream)
                WriteChain(writer, entry.StartSector, CeilDiv(entry.Size, MiniSectorSize));
        }

        WriteFree(writer, (plan.MiniFatSectors * plan.EntriesPerSector) - plan.TotalMiniSectors);
    }

    /// <summary>
    /// Emits the mini-stream content, padding each member to a mini-sector boundary.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries.</param>
    private static void EmitMiniStream(SectorWriter writer, List<Entry> entries)
    {
        foreach (Entry entry in entries)
        {
            if (!entry.IsMiniStream)
                continue;

            CopyContent(writer, entry);
            writer.WriteZeros((CeilDiv(entry.Size, MiniSectorSize) * MiniSectorSize) - entry.Size);
        }
    }

    /// <summary>
    /// Emits the FAT sectors, chaining every sector run, marking the FAT and DIFAT sectors and any range lock sector,
    /// and padding with free markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    private static void EmitFat(SectorWriter writer, EmissionPlan plan)
    {
        foreach (uint entry in EnumerateFat(plan))
            writer.WriteUInt32(entry);
    }

    /// <summary>
    /// Emits the extended DIFAT sectors, each carrying FAT-sector indices and a chain pointer.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="plan">The emission plan.</param>
    private static void EmitDifat(SectorWriter writer, EmissionPlan plan)
    {
        foreach (uint entry in EnumerateDifat(plan))
            writer.WriteUInt32(entry);
    }

    /// <summary>
    /// Returns the entries of the FAT sectors in the order of the file's sectors.
    /// </summary>
    /// <param name="plan">The emission plan.</param>
    /// <returns>
    /// The entry of each sector of the file, with the range lock sector marked ENDOFCHAIN when the plan reserves it,
    /// followed by the free markers that fill the last FAT sector.
    /// </returns>
    private static IEnumerable<uint> EnumerateFat(EmissionPlan plan)
    {
        long sector = 0;
        foreach (uint entry in EnumerateSequentialFat(plan))
        {
            // [MS-CFB] 2.8: the range lock sector is allocated and marked ENDOFCHAIN, but no chain reaches it.
            if (plan.ReservesRangeLock && sector == RangeLockSector)
            {
                yield return CfbHeader.EndOfChain;
                sector++;
            }

            yield return entry;
            sector++;
        }

        for (; sector < plan.FatSectors * plan.EntriesPerSector; sector++)
            yield return CfbHeader.FreeSector;
    }

    /// <summary>
    /// Returns the FAT entries of the sectors in the order the plan lays them out, the range lock sector aside.
    /// </summary>
    /// <param name="plan">The emission plan.</param>
    /// <returns>
    /// The entry of each directory, mini FAT, mini stream and stream sector, linking it to the next sector of its run,
    /// placed past the range lock sector, or ending its chain, then the markers of the FAT and DIFAT sectors.
    /// </returns>
    private static IEnumerable<uint> EnumerateSequentialFat(EmissionPlan plan)
    {
        List<(long Start, long Count)> runs = [(plan.DirectoryStart, plan.DirectorySectors)];
        if (plan.MiniFatSectors > 0)
            runs.Add((plan.MiniFatStart, plan.MiniFatSectors));

        if (plan.MiniStreamSectors > 0)
            runs.Add((plan.MiniStreamStart, plan.MiniStreamSectors));

        foreach (Entry entry in plan.Entries)
        {
            if (entry.IsRegularStream)
                runs.Add((entry.StartSector, CeilDiv(entry.Size, plan.SectorSize)));
        }

        foreach ((long start, long count) in runs)
        {
            for (long i = 0; i < count; i++)
                yield return i == count - 1 ? CfbHeader.EndOfChain : plan.ToFileSector(start + i + 1);
        }

        for (long i = 0; i < plan.FatSectors; i++)
            yield return CfbHeader.FatSector;

        for (long i = 0; i < plan.DifatSectors; i++)
            yield return CfbHeader.DifatSector;
    }

    /// <summary>
    /// Returns the entries of the extended DIFAT sectors.
    /// </summary>
    /// <param name="plan">The emission plan.</param>
    /// <returns>
    /// The FAT sectors the header's DIFAT array does not hold, padded with free markers, with each DIFAT sector's last
    /// entry pointing to the next or ending the chain.
    /// </returns>
    private static IEnumerable<uint> EnumerateDifat(EmissionPlan plan)
    {
        int slotsPerSector = plan.EntriesPerSector - 1;
        long fatIndex = CfbHeader.HeaderDifatCount;
        for (long s = 0; s < plan.DifatSectors; s++)
        {
            for (int j = 0; j < slotsPerSector; j++)
            {
                yield return fatIndex < plan.FatSectors ? plan.ToFileSector(plan.FatStart + fatIndex) : CfbHeader.FreeSector;
                fatIndex++;
            }

            yield return s == plan.DifatSectors - 1 ? CfbHeader.EndOfChain : plan.ToFileSector(plan.DifatStart + s + 1);
        }
    }

    /// <summary>
    /// Writes a sequential mini sector chain to the mini FAT, terminating with the end-of-chain marker.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="start">The first mini sector of the run.</param>
    /// <param name="count">The number of mini sectors in the run.</param>
    private static void WriteChain(SectorWriter writer, uint start, long count)
    {
        for (long i = 0; i < count; i++)
            writer.WriteUInt32(i == count - 1 ? CfbHeader.EndOfChain : (uint)(start + i + 1));
    }

    /// <summary>
    /// Writes a run of free-sector markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="count">The number of markers to write.</param>
    private static void WriteFree(SectorWriter writer, long count)
    {
        for (long i = 0; i < count; i++)
            writer.WriteUInt32(CfbHeader.FreeSector);
    }

    /// <summary>
    /// Copies a stream entry's payload to the writer, from memory or a deferred source.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entry">The stream entry to copy.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when a deferred source is shorter than its declared length.
    /// </exception>
    private static void CopyContent(SectorWriter writer, Entry entry)
    {
        CompoundStreamBuilder node = entry.StreamNode!;
        if (!node.IsDeferred)
        {
            writer.WriteBytes(node.Content.Span);
            return;
        }

        long remaining = entry.Size;
        using Stream source = node.OpenContentStream();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = source.Read(buffer, 0, toRead);
                if (read <= 0)
                {
                    throw new CompoundFileSerializationException(
                        string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundBuilderStreamLength, node.Name, entry.Size));
                }

                writer.WriteBytes(buffer.AsSpan(0, read));
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Rounds a value up to the next multiple of <paramref name="divisor" />, then divides.
    /// </summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The ceiling of the division.</returns>
    private static long CeilDiv(long value, int divisor) =>
        value <= 0 ? 0 : (value + divisor - 1) / divisor;

    /// <summary>
    /// Buffers output one sector at a time and flushes whole sectors to a destination stream, writing the range lock
    /// sector as zeros when the plan reserves it.
    /// </summary>
    private sealed partial class SectorWriter
    {
        /// <summary>The destination stream.</summary>
        private readonly Stream _destination;

        /// <summary>The reusable single-sector buffer.</summary>
        private readonly byte[] _buffer;

        /// <summary>Whether the range lock sector is reserved, and so written as zeros when the output reaches it.</summary>
        private readonly bool _reservesRangeLock;

        /// <summary>The number of bytes currently buffered in the active sector.</summary>
        private int _position;

        /// <summary>The sector of the file the active sector is flushed to.</summary>
        private long _sector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorWriter" /> class.
        /// </summary>
        /// <param name="destination">The destination stream.</param>
        /// <param name="plan">
        /// The emission plan, which gives the sector size and whether the range lock sector is reserved.
        /// </param>
        /// <param name="firstSector">The sector of the file the first sector written occupies.</param>
        public SectorWriter(Stream destination, EmissionPlan plan, long firstSector)
        {
            _destination = destination;
            _buffer = new byte[plan.SectorSize];
            _reservesRangeLock = plan.ReservesRangeLock;
            _sector = firstSector;
        }

        /// <summary>
        /// Writes raw bytes, flushing whole sectors as they fill.
        /// </summary>
        /// <param name="data">The bytes to write.</param>
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                int n = Math.Min(_buffer.Length - _position, data.Length);
                data.Slice(0, n).CopyTo(_buffer.AsSpan(_position));
                _position += n;
                data = data.Slice(n);
                if (_position == _buffer.Length)
                    FlushSector();
            }
        }

        /// <summary>
        /// Writes a little-endian 32-bit value. The caller must keep the position four-byte aligned.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
            if (_position == _buffer.Length)
                FlushSector();
        }

        /// <summary>
        /// Writes a run of zero bytes, flushing whole sectors as they fill.
        /// </summary>
        /// <param name="count">The number of zero bytes to write.</param>
        public void WriteZeros(long count)
        {
            while (count > 0)
            {
                int n = (int)Math.Min(_buffer.Length - _position, count);
                _buffer.AsSpan(_position, n).Clear();
                _position += n;
                count -= n;
                if (_position == _buffer.Length)
                    FlushSector();
            }
        }

        /// <summary>
        /// Zero-fills the remainder of the active sector and flushes it, leaving the writer sector-aligned.
        /// </summary>
        public void PadToSector()
        {
            if (_position > 0)
            {
                _buffer.AsSpan(_position).Clear();
                FlushSector();
            }
        }

        /// <summary>
        /// Writes the active sector buffer to the destination and resets the position, writing the range lock sector
        /// first when the active sector would take its place.
        /// </summary>
        private void FlushSector()
        {
            if (TakeRangeLockSector())
                _destination.Write(new byte[_buffer.Length], 0, _buffer.Length);

            _destination.Write(_buffer, 0, _buffer.Length);
            _position = 0;
            _sector++;
        }

        /// <summary>
        /// Determines whether the active sector would be flushed to the reserved range lock sector, and if so moves it
        /// to the sector after.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the caller must write the range lock sector, as zeros, before the active
        /// sector; otherwise <see langword="false" />.
        /// </returns>
        private bool TakeRangeLockSector()
        {
            if (!_reservesRangeLock || _sector != RangeLockSector)
                return false;

            _sector++;
            return true;
        }
    }

    /// <summary>
    /// Represents a directory entry under construction during serialization.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>The earliest instant representable as a Windows FILETIME.</summary>
        private static readonly DateTime s_fileTimeEpoch = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Gets or sets the entry name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entry type.
        /// </summary>
        public CompoundEntryType Type { get; set; }

        /// <summary>
        /// Gets or sets the class identifier.
        /// </summary>
        public Guid ClassId { get; set; }

        /// <summary>
        /// Gets or sets the user-defined state bits.
        /// </summary>
        public int StateBits { get; set; }

        /// <summary>
        /// Gets or sets the raw creation FILETIME.
        /// </summary>
        public long CreationFileTime { get; set; }

        /// <summary>
        /// Gets or sets the raw modified FILETIME.
        /// </summary>
        public long ModifiedFileTime { get; set; }

        /// <summary>
        /// Gets or sets the source stream node for a stream entry.
        /// </summary>
        public CompoundStreamBuilder? StreamNode { get; set; }

        /// <summary>
        /// Gets or sets the payload size, in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the starting sector or mini-sector identifier.
        /// </summary>
        public uint StartSector { get; set; } = CfbHeader.EndOfChain;

        /// <summary>
        /// Gets or sets the left-sibling stream identifier.
        /// </summary>
        public uint LeftSibling { get; set; } = CfbHeader.NoStream;

        /// <summary>
        /// Gets or sets the right-sibling stream identifier.
        /// </summary>
        public uint RightSibling { get; set; } = CfbHeader.NoStream;

        /// <summary>
        /// Gets or sets the child-tree root stream identifier.
        /// </summary>
        public uint ChildId { get; set; } = CfbHeader.NoStream;

        /// <summary>
        /// Gets a value indicating whether the entry is a stream stored in the mini stream.
        /// </summary>
        public bool IsMiniStream => Type == CompoundEntryType.Stream && Size is > 0 and < MiniStreamCutoff;

        /// <summary>
        /// Gets a value indicating whether the entry is a stream stored in the regular sectors.
        /// </summary>
        public bool IsRegularStream => Type == CompoundEntryType.Stream && Size >= MiniStreamCutoff;

        /// <summary>
        /// Creates a build entry from a node.
        /// </summary>
        /// <param name="node">The source node.</param>
        /// <param name="type">The directory entry type to assign.</param>
        /// <returns>The build entry.</returns>
        public static Entry FromNode(CompoundEntryBuilder node, CompoundEntryType type)
        {
            // MS-CFB §2.6.1: a stream object's CLSID and creation/modified times MUST be zero and its state bits
            // SHOULD be zero; only storage and root-storage entries carry that metadata.
            bool isStream = type == CompoundEntryType.Stream;

            var entry = new Entry
            {
                Name = node.Name,
                Type = type,
                ClassId = isStream ? Guid.Empty : node.ClassId,
                StateBits = isStream ? 0 : node.StateBits,
                CreationFileTime = isStream ? 0 : ToFileTime(node.CreationTime),
                ModifiedFileTime = isStream ? 0 : ToFileTime(node.ModifiedTime),
            };

            if (node is CompoundStreamBuilder stream)
            {
                entry.StreamNode = stream;
                entry.Size = stream.Length;
            }

            return entry;
        }

        /// <summary>
        /// Encodes this entry into a 128-byte directory record.
        /// </summary>
        /// <param name="record">The 128-byte destination span.</param>
        /// <param name="plan">The emission plan, which places a regular sector past the range lock sector.</param>
        public void Encode(Span<byte> record, EmissionPlan plan)
        {
            // A regular stream and the root's mini stream start in a regular sector; a small stream, in a mini sector.
            uint startSector = IsRegularStream || (Type == CompoundEntryType.RootStorage && Size > 0)
                ? plan.ToFileSector(StartSector)
                : StartSector;

            int charCount = Name.Length;
            Encoding.Unicode.GetBytes(Name, record.Slice(0, charCount * 2));
            BinaryPrimitives.WriteUInt16LittleEndian(record.Slice(64), (ushort)((charCount + 1) * 2));
            record[66] = (byte)Type;
            record[67] = (byte)CompoundEntryColor.Black;
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(68), LeftSibling);
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(72), RightSibling);
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(76), ChildId);
            _ = ClassId.TryWriteBytes(record.Slice(80, 16));
            BinaryPrimitives.WriteInt32LittleEndian(record.Slice(96), StateBits);
            BinaryPrimitives.WriteInt64LittleEndian(record.Slice(100), CreationFileTime);
            BinaryPrimitives.WriteInt64LittleEndian(record.Slice(108), ModifiedFileTime);
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(116), Type == CompoundEntryType.Storage ? 0 : startSector);
            BinaryPrimitives.WriteUInt64LittleEndian(record.Slice(120), Type == CompoundEntryType.Storage ? 0 : (ulong)Size);
        }

        /// <summary>
        /// Converts an optional time to a Windows FILETIME, using zero for absent or out-of-range values.
        /// </summary>
        /// <param name="value">The time to convert.</param>
        /// <returns>The Windows FILETIME, or <c>0</c>.</returns>
        private static long ToFileTime(DateTimeOffset? value)
        {
            if (value is not { } time || time.UtcDateTime < s_fileTimeEpoch)
                return 0;

            return time.ToFileTime();
        }
    }
}
