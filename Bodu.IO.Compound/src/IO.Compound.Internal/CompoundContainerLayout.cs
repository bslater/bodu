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
/// is proportional to the number of directory entries plus a single sector — independent of the output size.
/// </para>
/// <para>
/// Streams are partitioned into the mini stream (small) and the regular sectors, the directory is encoded as a
/// red-black tree per storage, and the FAT/DIFAT sector counts are resolved to a fixed point. The result is
/// byte-compatible with the reader and with other conforming parsers.
/// </para>
/// </remarks>
internal static class CompoundContainerLayout
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

        // Resolve the FAT/DIFAT counts to a fixed point (the FAT describes its own and the DIFAT's sectors).
        ResolveFatCounts(dataSectorCount, entriesPerSector, out long fatSectors, out long difatSectors);
        long fatStart = dataSectorCount;
        long difatStart = dataSectorCount + fatSectors;
        long totalSectors = dataSectorCount + fatSectors + difatSectors;
        if (totalSectors >= CfbHeader.DifatSector)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Op_Invalid_CompoundBuilderTooLarge);

        var writer = new SectorWriter(destination, sectorSize);

        EmitHeader(writer, isVersion4, fatSectors, directoryStart, directorySectors, miniFatStart, miniFatSectors, difatStart, difatSectors, fatStart);
        EmitDirectory(writer, entries);
        if (miniFatSectors > 0)
            EmitMiniFat(writer, entries, totalMiniSectors, miniFatSectors, entriesPerSector);

        if (miniStreamSectors > 0)
        {
            EmitMiniStream(writer, entries);
            writer.PadToSector();
        }

        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
            {
                CopyContent(writer, entry);
                writer.WriteZeros((CeilDiv(entry.Size, sectorSize) * sectorSize) - entry.Size);
            }
        }

        EmitFat(
            writer,
            entries,
            dataSectorCount,
            fatStart,
            fatSectors,
            difatSectors,
            directoryStart,
            directorySectors,
            miniFatStart,
            miniFatSectors,
            miniStreamStart,
            miniStreamSectors,
            sectorSize,
            entriesPerSector);

        if (difatSectors > 0)
            EmitDifat(writer, fatStart, fatSectors, difatStart, difatSectors, entriesPerSector);
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
    /// Resolves the FAT and DIFAT sector counts to a fixed point.
    /// </summary>
    /// <param name="dataSectorCount">The number of non-FAT, non-DIFAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <param name="fatSectors">The resolved FAT sector count.</param>
    /// <param name="difatSectors">The resolved DIFAT sector count.</param>
    private static void ResolveFatCounts(long dataSectorCount, int entriesPerSector, out long fatSectors, out long difatSectors)
    {
        fatSectors = 0;
        difatSectors = 0;
        while (true)
        {
            long total = dataSectorCount + fatSectors + difatSectors;
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
    /// Emits the header sector: the 512-byte header followed by zero padding to the sector boundary.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="isVersion4">Whether the file uses the version-4 (4096-byte sector) layout.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="directoryStart">The index of the first directory sector.</param>
    /// <param name="directorySectors">The number of directory sectors.</param>
    /// <param name="miniFatStart">The index of the first mini-FAT sector.</param>
    /// <param name="miniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="difatStart">The index of the first DIFAT sector.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="fatStart">The index of the first FAT sector.</param>
    private static void EmitHeader(
        SectorWriter writer,
        bool isVersion4,
        long fatSectors,
        long directoryStart,
        long directorySectors,
        uint miniFatStart,
        long miniFatSectors,
        long difatStart,
        long difatSectors,
        long fatStart)
    {
        Span<byte> h = stackalloc byte[512];
        h.Clear();
        CfbHeader.Signature.CopyTo(h);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(24), MinorVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(26), (ushort)(isVersion4 ? 4 : 3));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(28), ByteOrderMarker);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(30), (ushort)(isVersion4 ? 12 : 9));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(32), 6);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(40), isVersion4 ? (uint)directorySectors : 0);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(44), (uint)fatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(48), (uint)directoryStart);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(56), MiniStreamCutoff);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(60), miniFatSectors > 0 ? miniFatStart : CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(64), (uint)miniFatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(68), difatSectors > 0 ? (uint)difatStart : CfbHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(72), (uint)difatSectors);

        long inlineCount = Math.Min(fatSectors, CfbHeader.HeaderDifatCount);
        for (int i = 0; i < CfbHeader.HeaderDifatCount; i++)
        {
            uint value = i < inlineCount ? (uint)(fatStart + i) : CfbHeader.FreeSector;
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(76 + (i * 4)), value);
        }

        writer.WriteBytes(h);
        writer.PadToSector();
    }

    /// <summary>
    /// Emits the directory sectors, encoding each entry and padding the final sector with zero.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries, in stream-identifier order.</param>
    private static void EmitDirectory(SectorWriter writer, List<Entry> entries)
    {
        Span<byte> record = stackalloc byte[DirectoryEntrySize];
        foreach (Entry entry in entries)
        {
            record.Clear();
            entry.Encode(record);
            writer.WriteBytes(record);
        }

        writer.PadToSector();
    }

    /// <summary>
    /// Emits the mini-FAT sectors, chaining each mini-stream member and padding with free markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries.</param>
    /// <param name="totalMiniSectors">The total number of mini sectors.</param>
    /// <param name="miniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    private static void EmitMiniFat(SectorWriter writer, List<Entry> entries, int totalMiniSectors, long miniFatSectors, int entriesPerSector)
    {
        foreach (Entry entry in entries)
        {
            if (entry.IsMiniStream)
                WriteChain(writer, entry.StartSector, CeilDiv(entry.Size, MiniSectorSize));
        }

        WriteFree(writer, (miniFatSectors * entriesPerSector) - totalMiniSectors);
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
    /// Emits the FAT sectors, chaining every sector run, marking the FAT and DIFAT sectors, and padding with free
    /// markers.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="entries">The directory entries.</param>
    /// <param name="dataSectorCount">The number of non-FAT, non-DIFAT sectors.</param>
    /// <param name="fatStart">The index of the first FAT sector.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="directoryStart">The index of the first directory sector.</param>
    /// <param name="directorySectors">The number of directory sectors.</param>
    /// <param name="miniFatStart">The index of the first mini-FAT sector.</param>
    /// <param name="miniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="miniStreamStart">The index of the first mini-stream sector.</param>
    /// <param name="miniStreamSectors">The number of mini-stream sectors.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    private static void EmitFat(
        SectorWriter writer,
        List<Entry> entries,
        long dataSectorCount,
        long fatStart,
        long fatSectors,
        long difatSectors,
        long directoryStart,
        long directorySectors,
        uint miniFatStart,
        long miniFatSectors,
        uint miniStreamStart,
        long miniStreamSectors,
        int sectorSize,
        int entriesPerSector)
    {
        WriteChain(writer, (uint)directoryStart, directorySectors);
        if (miniFatSectors > 0)
            WriteChain(writer, miniFatStart, miniFatSectors);

        if (miniStreamSectors > 0)
            WriteChain(writer, miniStreamStart, miniStreamSectors);

        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
                WriteChain(writer, entry.StartSector, CeilDiv(entry.Size, sectorSize));
        }

        for (long i = 0; i < fatSectors; i++)
            writer.WriteUInt32(CfbHeader.FatSector);

        for (long i = 0; i < difatSectors; i++)
            writer.WriteUInt32(CfbHeader.DifatSector);

        long totalSectors = dataSectorCount + fatSectors + difatSectors;
        WriteFree(writer, (fatSectors * entriesPerSector) - totalSectors);
    }

    /// <summary>
    /// Emits the extended DIFAT sectors, each carrying FAT-sector indices and a chain pointer.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="fatStart">The index of the first FAT sector.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="difatStart">The index of the first DIFAT sector.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    private static void EmitDifat(SectorWriter writer, long fatStart, long fatSectors, long difatStart, long difatSectors, int entriesPerSector)
    {
        int slotsPerSector = entriesPerSector - 1;
        long fatIndex = CfbHeader.HeaderDifatCount;
        for (long s = 0; s < difatSectors; s++)
        {
            for (int j = 0; j < slotsPerSector; j++)
            {
                writer.WriteUInt32(fatIndex < fatSectors ? (uint)(fatStart + fatIndex) : CfbHeader.FreeSector);
                fatIndex++;
            }

            writer.WriteUInt32(s == difatSectors - 1 ? CfbHeader.EndOfChain : (uint)(difatStart + s + 1));
        }
    }

    /// <summary>
    /// Writes a sequential sector chain to the FAT, terminating with the end-of-chain marker.
    /// </summary>
    /// <param name="writer">The destination sector writer.</param>
    /// <param name="start">The first sector of the run.</param>
    /// <param name="count">The number of sectors in the run.</param>
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
    /// Buffers output one sector at a time and flushes whole sectors to a destination stream.
    /// </summary>
    private sealed class SectorWriter
    {
        /// <summary>The destination stream.</summary>
        private readonly Stream _destination;

        /// <summary>The reusable single-sector buffer.</summary>
        private readonly byte[] _buffer;

        /// <summary>The number of bytes currently buffered in the active sector.</summary>
        private int _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorWriter" /> class.
        /// </summary>
        /// <param name="destination">The destination stream.</param>
        /// <param name="sectorSize">The sector size, in bytes.</param>
        public SectorWriter(Stream destination, int sectorSize)
        {
            _destination = destination;
            _buffer = new byte[sectorSize];
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
        /// Writes the active sector buffer to the destination and resets the position.
        /// </summary>
        private void FlushSector()
        {
            _destination.Write(_buffer, 0, _buffer.Length);
            _position = 0;
        }
    }

    /// <summary>
    /// Represents a directory entry under construction during serialization.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>The earliest instant representable as a Windows FILETIME.</summary>
        private static readonly DateTime FileTimeEpoch = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
        public void Encode(Span<byte> record)
        {
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
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(116), Type == CompoundEntryType.Storage ? 0 : StartSector);
            BinaryPrimitives.WriteUInt64LittleEndian(record.Slice(120), Type == CompoundEntryType.Storage ? 0 : (ulong)Size);
        }

        /// <summary>
        /// Converts an optional time to a Windows FILETIME, using zero for absent or out-of-range values.
        /// </summary>
        /// <param name="value">The time to convert.</param>
        /// <returns>The Windows FILETIME, or <c>0</c>.</returns>
        private static long ToFileTime(DateTimeOffset? value)
        {
            if (value is not { } time || time.UtcDateTime < FileTimeEpoch)
                return 0;

            return time.ToFileTime();
        }
    }
}
