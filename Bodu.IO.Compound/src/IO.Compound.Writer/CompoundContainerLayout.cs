// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Compound.Nodes;

namespace Bodu.IO.Compound.Writer;

/// <summary>
/// Serializes a mutable compound-file object model into the OLE2 / Compound File Binary byte layout.
/// </summary>
/// <remarks>
/// The layout is computed in a single allocate-then-back-patch pass: streams are partitioned into the mini stream and
/// the regular sectors, the directory is encoded as a red-black tree per storage, the file-allocation table (FAT) and
/// double-indirect FAT (DIFAT) sector counts are resolved to a fixed point, and finally the header and every sector are
/// written. The result is byte-compatible with the reader and with other conforming parsers.
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

    /// <summary>
    /// Serializes the supplied root storage into a compound-file byte array.
    /// </summary>
    /// <param name="root">The root storage to serialize.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <returns>The complete compound-file content.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the model cannot be represented.</exception>
    internal static byte[] Write(CompoundStorageNode root, CompoundWriterOptions options)
    {
        int sectorSize = options.SectorSize;
        int entriesPerSector = sectorSize / 4;
        int directoriesPerSector = sectorSize / DirectoryEntrySize;
        bool isVersion4 = options.Version == CompoundFileVersion.V4;

        List<Entry> entries = Flatten(root, options.EffectiveMaxDepth);

        // Partition streams into the mini stream (small) and the regular sectors (large).
        BuildMiniStream(entries, out byte[] miniStreamBytes, out uint[] miniFat, entriesPerSector);

        // Encode the directory entries (the red-black links were assigned during flatten).
        int directorySectors = CeilDiv(entries.Count, directoriesPerSector);

        int miniFatSectors = miniFat.Length / entriesPerSector;
        int miniStreamSectors = CeilDiv(miniStreamBytes.Length, sectorSize);

        // Assign regular sector indices: directory, mini-FAT, mini-stream, then each large stream.
        int next = 0;
        int directoryStart = next;
        next += directorySectors;
        uint miniFatStart = miniFatSectors > 0 ? (uint)next : CompoundFileHeader.EndOfChain;
        next += miniFatSectors;
        uint miniStreamStart = miniStreamSectors > 0 ? (uint)next : CompoundFileHeader.EndOfChain;
        next += miniStreamSectors;

        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
            {
                entry.StartSector = (uint)next;
                next += CeilDiv((int)entry.Size, sectorSize);
            }
        }

        int dataSectorCount = next;

        // The root entry owns the mini stream as its regular-sector chain.
        Entry rootEntry = entries[0];
        rootEntry.StartSector = miniStreamStart;
        rootEntry.Size = miniStreamBytes.Length;

        // Resolve the FAT/DIFAT counts to a fixed point (the FAT describes its own and the DIFAT's sectors).
        ResolveFatCounts(dataSectorCount, entriesPerSector, out int fatSectors, out int difatSectors);
        int fatStart = dataSectorCount;
        int difatStart = dataSectorCount + fatSectors;
        int totalSectors = dataSectorCount + fatSectors + difatSectors;

        uint[] fat = BuildFat(
            entries,
            fatStart,
            fatSectors,
            difatStart,
            difatSectors,
            directoryStart,
            directorySectors,
            miniFatStart,
            miniFatSectors,
            miniStreamStart,
            miniStreamSectors,
            sectorSize,
            entriesPerSector);

        uint[] inlineDifat = BuildDifat(fatStart, fatSectors, difatStart, difatSectors, entriesPerSector, out uint[] difatSectorData);

        byte[] file = new byte[(long)(totalSectors + 1) * sectorSize];
        WriteHeader(
            file,
            isVersion4,
            fatSectors,
            directoryStart,
            directorySectors,
            miniFatStart,
            miniFatSectors,
            difatStart,
            difatSectors,
            inlineDifat);

        WriteDirectory(file, entries, directoryStart, sectorSize);
        WriteSectorData(file, UIntsToBytes(miniFat), miniFatStart, sectorSize);
        WriteSectorData(file, miniStreamBytes, miniStreamStart, sectorSize);
        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
                WriteSectorData(file, entry.Content.Span, entry.StartSector, sectorSize);
        }

        WriteSectorData(file, UIntsToBytes(fat), (uint)fatStart, sectorSize);
        if (difatSectors > 0)
            WriteSectorData(file, UIntsToBytes(difatSectorData), (uint)difatStart, sectorSize);

        return file;
    }

    /// <summary>
    /// Flattens the tree into a stream-identifier-indexed entry list and assigns each storage's red-black child links.
    /// </summary>
    /// <param name="root">The root storage.</param>
    /// <param name="maxDepth">The maximum permitted nesting depth.</param>
    /// <returns>The entry list with the root at index 0.</returns>
    private static List<Entry> Flatten(CompoundStorageNode root, int maxDepth)
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
    private static void AssignChildren(CompoundStorageNode storage, int sid, int depth, int maxDepth, List<Entry> entries)
    {
        List<CompoundNode> children = storage.Values.ToList();
        if (children.Count == 0)
            return;

        if (depth > maxDepth)
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundWriterMaxDepthExceeded, maxDepth));
        }

        children.Sort(static (a, b) => CompareNames(a.Name, b.Name));

        List<int> childSids = new(children.Count);
        foreach (CompoundNode child in children)
        {
            CompoundEntryType type = child is CompoundStorageNode ? CompoundEntryType.Storage : CompoundEntryType.Stream;
            entries.Add(Entry.FromNode(child, type));
            childSids.Add(entries.Count - 1);
        }

        entries[sid].ChildId = BuildTree(entries, childSids, 0, childSids.Count - 1);

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is CompoundStorageNode childStorage)
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
    /// <returns>
    /// The stream identifier of the subtree root, or <see cref="CompoundFileHeader.NoStream" /> when empty.
    /// </returns>
    private static uint BuildTree(List<Entry> entries, List<int> sids, int lo, int hi)
    {
        if (lo > hi)
            return CompoundFileHeader.NoStream;

        int mid = (lo + hi) / 2;
        int sid = sids[mid];
        entries[sid].LeftSibling = BuildTree(entries, sids, lo, mid - 1);
        entries[sid].RightSibling = BuildTree(entries, sids, mid + 1, hi);
        return (uint)sid;
    }

    /// <summary>
    /// Compares two entry names using the compound-file ordering: length first, then ordinal uppercase per code unit.
    /// </summary>
    /// <param name="a">The first name.</param>
    /// <param name="b">The second name.</param>
    /// <returns>A signed value describing the relative order of the names.</returns>
    private static int CompareNames(string a, string b)
    {
        if (a.Length != b.Length)
            return a.Length - b.Length;

        for (int i = 0; i < a.Length; i++)
        {
            int diff = char.ToUpperInvariant(a[i]) - char.ToUpperInvariant(b[i]);
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    /// <summary>
    /// Builds the mini stream and mini-FAT from the small streams in the entry list.
    /// </summary>
    /// <param name="entries">The entry list; mini-stream members receive their starting mini-sector index.</param>
    /// <param name="miniStreamBytes">The concatenated, padded mini-stream bytes.</param>
    /// <param name="miniFat">The mini-FAT, padded to a whole number of sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    private static void BuildMiniStream(List<Entry> entries, out byte[] miniStreamBytes, out uint[] miniFat, int entriesPerSector)
    {
        using MemoryStream mini = new();
        List<uint> chains = new();
        uint miniSector = 0;

        foreach (Entry entry in entries)
        {
            if (!entry.IsMiniStream)
                continue;

            int length = (int)entry.Size;
            int sectors = CeilDiv(length, MiniSectorSize);
            entry.StartSector = miniSector;

            mini.Write(entry.Content.Span);
            int padding = (sectors * MiniSectorSize) - length;
            for (int i = 0; i < padding; i++)
                mini.WriteByte(0);

            for (int i = 0; i < sectors; i++)
                chains.Add(i == sectors - 1 ? CompoundFileHeader.EndOfChain : miniSector + (uint)i + 1);

            miniSector += (uint)sectors;
        }

        miniStreamBytes = mini.ToArray();

        // Pad the mini-FAT up to a whole number of sectors with free markers.
        int miniFatLength = chains.Count == 0 ? 0 : CeilDiv(chains.Count, entriesPerSector) * entriesPerSector;
        miniFat = new uint[miniFatLength];
        for (int i = 0; i < miniFat.Length; i++)
            miniFat[i] = i < chains.Count ? chains[i] : CompoundFileHeader.FreeSector;
    }

    /// <summary>
    /// Resolves the FAT and DIFAT sector counts to a fixed point.
    /// </summary>
    /// <param name="dataSectorCount">The number of non-FAT, non-DIFAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <param name="fatSectors">The resolved FAT sector count.</param>
    /// <param name="difatSectors">The resolved DIFAT sector count.</param>
    private static void ResolveFatCounts(int dataSectorCount, int entriesPerSector, out int fatSectors, out int difatSectors)
    {
        fatSectors = 0;
        difatSectors = 0;
        while (true)
        {
            int total = dataSectorCount + fatSectors + difatSectors;
            int newFat = CeilDiv(total, entriesPerSector);
            int newDifat = newFat <= CompoundFileHeader.HeaderDifatCount
                ? 0
                : CeilDiv(newFat - CompoundFileHeader.HeaderDifatCount, entriesPerSector - 1);

            if (newFat == fatSectors && newDifat == difatSectors)
                return;

            fatSectors = newFat;
            difatSectors = newDifat;
        }
    }

    /// <summary>
    /// Builds the regular file-allocation table, chaining every sector run and marking FAT and DIFAT sectors.
    /// </summary>
    /// <param name="entries">The directory entries; regular streams contribute their sector runs.</param>
    /// <param name="fatStart">The index of the first FAT sector.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="difatStart">The index of the first DIFAT sector.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="directoryStart">The index of the first directory sector.</param>
    /// <param name="directorySectors">The number of directory sectors.</param>
    /// <param name="miniFatStart">The index of the first mini-FAT sector.</param>
    /// <param name="miniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="miniStreamStart">The index of the first mini-stream sector.</param>
    /// <param name="miniStreamSectors">The number of mini-stream sectors.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <returns>The FAT, padded to a whole number of FAT sectors.</returns>
    private static uint[] BuildFat(
        List<Entry> entries,
        int fatStart,
        int fatSectors,
        int difatStart,
        int difatSectors,
        int directoryStart,
        int directorySectors,
        uint miniFatStart,
        int miniFatSectors,
        uint miniStreamStart,
        int miniStreamSectors,
        int sectorSize,
        int entriesPerSector)
    {
        uint[] fat = new uint[fatSectors * entriesPerSector];
        for (int i = 0; i < fat.Length; i++)
            fat[i] = CompoundFileHeader.FreeSector;

        ChainRun(fat, directoryStart, directorySectors);
        if (miniFatSectors > 0)
            ChainRun(fat, (int)miniFatStart, miniFatSectors);
        if (miniStreamSectors > 0)
            ChainRun(fat, (int)miniStreamStart, miniStreamSectors);

        foreach (Entry entry in entries)
        {
            if (entry.IsRegularStream)
                ChainRun(fat, (int)entry.StartSector, CeilDiv((int)entry.Size, sectorSize));
        }

        for (int i = 0; i < fatSectors; i++)
            fat[fatStart + i] = CompoundFileHeader.FatSector;

        for (int i = 0; i < difatSectors; i++)
            fat[difatStart + i] = CompoundFileHeader.DifatSector;

        return fat;
    }

    /// <summary>
    /// Writes a sequential sector chain into the FAT, terminating with the end-of-chain marker.
    /// </summary>
    /// <param name="fat">The FAT being populated.</param>
    /// <param name="start">The first sector of the run.</param>
    /// <param name="count">The number of sectors in the run.</param>
    private static void ChainRun(uint[] fat, int start, int count)
    {
        for (int i = 0; i < count; i++)
            fat[start + i] = i == count - 1 ? CompoundFileHeader.EndOfChain : (uint)(start + i + 1);
    }

    /// <summary>
    /// Builds the inline header DIFAT and any extended DIFAT sectors.
    /// </summary>
    /// <param name="fatStart">The index of the first FAT sector.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="difatStart">The index of the first DIFAT sector.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="entriesPerSector">The number of 32-bit entries per regular sector.</param>
    /// <param name="difatSectorData">The little-endian content of the extended DIFAT sectors, if any.</param>
    /// <returns>The 109-entry inline DIFAT for the header.</returns>
    private static uint[] BuildDifat(int fatStart, int fatSectors, int difatStart, int difatSectors, int entriesPerSector, out uint[] difatSectorData)
    {
        uint[] inline = new uint[CompoundFileHeader.HeaderDifatCount];
        int inlineCount = Math.Min(fatSectors, CompoundFileHeader.HeaderDifatCount);
        for (int i = 0; i < CompoundFileHeader.HeaderDifatCount; i++)
            inline[i] = i < inlineCount ? (uint)(fatStart + i) : CompoundFileHeader.FreeSector;

        if (difatSectors == 0)
        {
            difatSectorData = [];
            return inline;
        }

        int slotsPerSector = entriesPerSector - 1;
        difatSectorData = new uint[difatSectors * entriesPerSector];
        for (int i = 0; i < difatSectorData.Length; i++)
            difatSectorData[i] = CompoundFileHeader.FreeSector;

        int fatIndex = CompoundFileHeader.HeaderDifatCount;
        for (int s = 0; s < difatSectors; s++)
        {
            int baseSlot = s * entriesPerSector;
            for (int j = 0; j < slotsPerSector; j++)
            {
                difatSectorData[baseSlot + j] = fatIndex < fatSectors
                    ? (uint)(fatStart + fatIndex)
                    : CompoundFileHeader.FreeSector;
                fatIndex++;
            }

            difatSectorData[baseSlot + slotsPerSector] = s == difatSectors - 1
                ? CompoundFileHeader.EndOfChain
                : (uint)(difatStart + s + 1);
        }

        return inline;
    }

    /// <summary>
    /// Writes the 512-byte compound-file header into the file buffer.
    /// </summary>
    /// <param name="file">The file buffer.</param>
    /// <param name="isVersion4">Whether the file uses the version-4 (4096-byte sector) layout.</param>
    /// <param name="fatSectors">The number of FAT sectors.</param>
    /// <param name="directoryStart">The index of the first directory sector.</param>
    /// <param name="directorySectors">The number of directory sectors.</param>
    /// <param name="miniFatStart">The index of the first mini-FAT sector.</param>
    /// <param name="miniFatSectors">The number of mini-FAT sectors.</param>
    /// <param name="difatStart">The index of the first DIFAT sector.</param>
    /// <param name="difatSectors">The number of DIFAT sectors.</param>
    /// <param name="inlineDifat">The 109-entry inline DIFAT.</param>
    private static void WriteHeader(
        byte[] file,
        bool isVersion4,
        int fatSectors,
        int directoryStart,
        int directorySectors,
        uint miniFatStart,
        int miniFatSectors,
        int difatStart,
        int difatSectors,
        uint[] inlineDifat)
    {
        Span<byte> h = file.AsSpan(0, 512);
        CompoundFileHeader.Signature.CopyTo(h);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(24), MinorVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(26), (ushort)(isVersion4 ? 4 : 3));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(28), ByteOrderMarker);
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(30), (ushort)(isVersion4 ? 12 : 9));
        BinaryPrimitives.WriteUInt16LittleEndian(h.Slice(32), 6);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(40), isVersion4 ? (uint)directorySectors : 0);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(44), (uint)fatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(48), (uint)directoryStart);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(56), MiniStreamCutoff);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(60), miniFatSectors > 0 ? miniFatStart : CompoundFileHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(64), (uint)miniFatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(68), difatSectors > 0 ? (uint)difatStart : CompoundFileHeader.EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(72), (uint)difatSectors);

        for (int i = 0; i < CompoundFileHeader.HeaderDifatCount; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(76 + (i * 4)), inlineDifat[i]);
    }

    /// <summary>
    /// Encodes and writes the directory entries into their sectors.
    /// </summary>
    /// <param name="file">The file buffer.</param>
    /// <param name="entries">The directory entries, in stream-identifier order.</param>
    /// <param name="directoryStart">The index of the first directory sector.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    private static void WriteDirectory(byte[] file, List<Entry> entries, int directoryStart, int sectorSize)
    {
        int baseOffset = (directoryStart + 1) * sectorSize;
        for (int i = 0; i < entries.Count; i++)
            entries[i].Encode(file.AsSpan(baseOffset + (i * DirectoryEntrySize), DirectoryEntrySize));
    }

    /// <summary>
    /// Writes a payload into a sector run, zero-padding to the sector boundary.
    /// </summary>
    /// <param name="file">The file buffer.</param>
    /// <param name="data">The payload bytes.</param>
    /// <param name="startSector">
    /// The first sector of the run, or <see cref="CompoundFileHeader.EndOfChain" /> for no run.
    /// </param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    private static void WriteSectorData(byte[] file, ReadOnlySpan<byte> data, uint startSector, int sectorSize)
    {
        if (data.Length == 0 || startSector == CompoundFileHeader.EndOfChain)
            return;

        data.CopyTo(file.AsSpan((int)((startSector + 1) * sectorSize)));
    }

    /// <summary>
    /// Rounds a value up to the next multiple of <paramref name="divisor" />, then divides.
    /// </summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The ceiling of the division.</returns>
    private static int CeilDiv(int value, int divisor) =>
        value <= 0 ? 0 : (value + divisor - 1) / divisor;

    /// <summary>
    /// Converts a 32-bit array to its little-endian byte form.
    /// </summary>
    /// <param name="values">The values to convert.</param>
    /// <returns>The little-endian bytes.</returns>
    private static byte[] UIntsToBytes(uint[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);

        return bytes;
    }

    /// <summary>
    /// Represents a directory entry under construction during serialization.
    /// </summary>
    private sealed class Entry
    {
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
        /// Gets or sets the payload for a stream entry.
        /// </summary>
        public ReadOnlyMemory<byte> Content { get; set; }

        /// <summary>
        /// Gets or sets the payload size, in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the starting sector or mini-sector identifier.
        /// </summary>
        public uint StartSector { get; set; } = CompoundFileHeader.EndOfChain;

        /// <summary>
        /// Gets or sets the left-sibling stream identifier.
        /// </summary>
        public uint LeftSibling { get; set; } = CompoundFileHeader.NoStream;

        /// <summary>
        /// Gets or sets the right-sibling stream identifier.
        /// </summary>
        public uint RightSibling { get; set; } = CompoundFileHeader.NoStream;

        /// <summary>
        /// Gets or sets the child-tree root stream identifier.
        /// </summary>
        public uint ChildId { get; set; } = CompoundFileHeader.NoStream;

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
        public static Entry FromNode(CompoundNode node, CompoundEntryType type)
        {
            var entry = new Entry
            {
                Name = node.Name,
                Type = type,
                ClassId = node.ClassId,
                StateBits = node.StateBits,
                CreationFileTime = ToFileTime(node.CreationTime),
                ModifiedFileTime = ToFileTime(node.ModifiedTime),
            };

            if (node is CompoundStreamNode stream)
            {
                entry.Content = stream.Content;
                entry.Size = stream.Content.Length;
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
            if (value is not { } time || time.UtcDateTime < s_fileTimeEpoch)
                return 0;

            return time.ToFileTime();
        }

        /// <summary>The earliest instant representable as a Windows FILETIME.</summary>
        private static readonly DateTime s_fileTimeEpoch = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
