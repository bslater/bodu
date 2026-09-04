// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFixtureBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Builds small, structurally valid PST files in memory for the container tests: a header, a block B-tree and a node
/// B-tree over the blocks and nodes the test declares, with every checksum, signature, and content encoding applied
/// as a real writer would.
/// </summary>
/// <remarks>
/// <para>
/// The builder writes the Unicode layout by default and the ANSI layout when <see cref="Format" /> is set; every width
/// and offset comes from the corresponding <see cref="PstLayout" />, so the two variants share one code path. The
/// parameterless offset helpers (<see cref="ReadNodeTreeRootOffset(byte[])" /> and friends) keep their Unicode meaning
/// for the many tests that patch absolute offsets; the layout-taking overloads serve the ANSI tests.
/// </para>
/// <para>
/// Blocks are appended in declaration order at 64-byte alignment; pages are appended at 512-byte alignment after the
/// blocks, block tree first. Content is placed after a fixed lead-in so the header never overlaps it.
/// </para>
/// </remarks>
internal sealed class PstFixtureBuilder
{
    /// <summary>The block alignment.</summary>
    private const int BlockAlignment = 64;

    /// <summary>The largest single-block payload of the Unicode layout; see <see cref="PstLayout.MaxBlockPayload" /> for the layout-specific value.</summary>
    internal const int MaxBlockPayload = 8176;

    /// <summary>The first content offset, clear of either header.</summary>
    private const int ContentStart = 1024;

    /// <summary>The file bytes under construction.</summary>
    private readonly List<byte> _file = [.. new byte[ContentStart]];

    /// <summary>The blocks written so far.</summary>
    private readonly List<BlockRow> _blocks = [];

    /// <summary>The nodes declared so far.</summary>
    private readonly List<PstNbtEntry> _nodes = [];

    /// <summary>The next block or page identifier.</summary>
    private ulong _nextBlockId = 4;

    /// <summary>The explicit header version, when a test overrides the format's default.</summary>
    private ushort? _version;

    /// <summary>
    /// Gets or sets the format the file is written in.
    /// </summary>
    /// <value><see cref="PstFileFormat.Unicode" /> by default.</value>
    internal PstFileFormat Format { get; set; } = PstFileFormat.Unicode;

    /// <summary>
    /// Gets the layout the file is written in.
    /// </summary>
    /// <value>The layout matching <see cref="Format" />.</value>
    internal PstLayout Layout =>
        Format == PstFileFormat.Ansi ? PstLayout.Ansi : PstLayout.Unicode;

    /// <summary>
    /// Gets or sets the content encoding applied to external blocks.
    /// </summary>
    /// <value><see cref="PstCryptMethod.None" /> by default.</value>
    internal PstCryptMethod CryptMethod { get; set; } = PstCryptMethod.None;

    /// <summary>
    /// Gets or sets the largest number of entries written to one B-tree page, to force a branch level in small trees.
    /// </summary>
    /// <value>Unbounded by default (the page capacity applies).</value>
    internal int MaxEntriesPerPage { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the header version (<c>wVer</c>).
    /// </summary>
    /// <value>23 for the Unicode format and 14 for the ANSI format unless overridden.</value>
    internal ushort Version
    {
        get => _version ?? (Format == PstFileFormat.Ansi ? (ushort)14 : (ushort)23);
        set => _version = value;
    }

    /// <summary>
    /// Gets or sets the header sentinel byte.
    /// </summary>
    /// <value><c>0x80</c> by default.</value>
    internal byte Sentinel { get; set; } = 0x80;

    /// <summary>
    /// Gets or sets a raw crypt-method byte written verbatim, overriding <see cref="CryptMethod" />.
    /// </summary>
    /// <value><see langword="null" /> to write <see cref="CryptMethod" />.</value>
    internal byte? RawCryptMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header checksum is written correctly.
    /// </summary>
    /// <value><see langword="true" /> by default.</value>
    internal bool WriteValidHeaderCrc { get; set; } = true;

    /// <summary>
    /// Gets the file offset of every block written so far, by block identifier.
    /// </summary>
    /// <value>A snapshot dictionary.</value>
    internal IReadOnlyDictionary<ulong, long> BlockOffsets =>
        _blocks.ToDictionary(static b => b.BlockId, static b => (long)b.Offset);

    /// <summary>
    /// Computes the on-disk length of a Unicode block with a payload of the given length.
    /// </summary>
    /// <param name="payloadLength">The payload length.</param>
    /// <returns>The trailer-inclusive length rounded up to the block alignment.</returns>
    internal static int BlockDiskLength(int payloadLength) =>
        BlockDiskLength(payloadLength, PstLayout.Unicode);

    /// <summary>
    /// Computes the on-disk length of a block with a payload of the given length in a layout.
    /// </summary>
    /// <param name="payloadLength">The payload length.</param>
    /// <param name="layout">The layout.</param>
    /// <returns>The trailer-inclusive length rounded up to the block alignment.</returns>
    internal static int BlockDiskLength(int payloadLength, PstLayout layout) =>
        (payloadLength + layout.BlockTrailerSize + BlockAlignment - 1) & ~(BlockAlignment - 1);

    /// <summary>
    /// Reads the block B-tree root page offset from a built Unicode file.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <returns>The page offset.</returns>
    internal static long ReadBlockTreeRootOffset(byte[] file) =>
        ReadBlockTreeRootOffset(file, PstLayout.Unicode);

    /// <summary>
    /// Reads the block B-tree root page offset from a built file in a layout.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <param name="layout">The layout.</param>
    /// <returns>The page offset.</returns>
    internal static long ReadBlockTreeRootOffset(byte[] file, PstLayout layout) =>
        (long)layout.ReadId(file.AsSpan(layout.BbtRootOffset + layout.IdWidth));

    /// <summary>
    /// Reads the node B-tree root page offset from a built Unicode file.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <returns>The page offset.</returns>
    internal static long ReadNodeTreeRootOffset(byte[] file) =>
        ReadNodeTreeRootOffset(file, PstLayout.Unicode);

    /// <summary>
    /// Reads the node B-tree root page offset from a built file in a layout.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <param name="layout">The layout.</param>
    /// <returns>The page offset.</returns>
    internal static long ReadNodeTreeRootOffset(byte[] file, PstLayout layout) =>
        (long)layout.ReadId(file.AsSpan(layout.NbtRootOffset + layout.IdWidth));

    /// <summary>
    /// Recomputes the header checksum after a test patches header bytes.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <remarks>
    /// The checksum covers both B-tree roots (the same 471-byte range in both layouts), so a test that patches one
    /// must repair it or the header is refused before the patched reference is followed.
    /// </remarks>
    internal static void RepairHeaderChecksum(byte[] file) =>
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), PstCrc.Compute(file.AsSpan(8, 471)));

    /// <summary>
    /// Adds an external data block.
    /// </summary>
    /// <param name="payload">The block payload.</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddDataBlock(byte[] payload) =>
        AddBlock(payload, isInternal: false);

    /// <summary>
    /// Adds an <c>XBLOCK</c> over data blocks.
    /// </summary>
    /// <param name="totalLength">The declared total payload length.</param>
    /// <param name="childIds">The child block identifiers.</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddXBlock(uint totalLength, params ulong[] childIds) =>
        AddBlock(BuildTreeBlock(level: 1, totalLength, childIds), isInternal: true);

    /// <summary>
    /// Adds an <c>XXBLOCK</c> over <c>XBLOCK</c>s.
    /// </summary>
    /// <param name="totalLength">The declared total payload length.</param>
    /// <param name="childIds">The child block identifiers.</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddXXBlock(uint totalLength, params ulong[] childIds) =>
        AddBlock(BuildTreeBlock(level: 2, totalLength, childIds), isInternal: true);

    /// <summary>
    /// Adds a subnode leaf block (<c>SLBLOCK</c>).
    /// </summary>
    /// <param name="entries">The subnode entries.</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddSubnodeLeafBlock(params (uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)[] entries)
    {
        int entrySize = Layout.SubnodeLeafEntrySize;
        int header = Layout.SubnodeBlockHeaderSize;
        var block = new byte[header + (entries.Length * entrySize)];
        block[0] = 0x02;
        block[1] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)entries.Length);

        for (int i = 0; i < entries.Length; i++)
        {
            Span<byte> row = block.AsSpan(header + (i * entrySize), entrySize);
            Layout.WriteId(row, entries[i].NodeId);
            Layout.WriteId(row.Slice(Layout.IdWidth), entries[i].DataBlockId);
            Layout.WriteId(row.Slice(Layout.IdWidth * 2), entries[i].SubnodeBlockId);
        }

        return AddBlock(block, isInternal: true);
    }

    /// <summary>
    /// Adds a subnode index block (<c>SIBLOCK</c>) over subnode leaf blocks.
    /// </summary>
    /// <param name="childIds">The leaf block identifiers.</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddSubnodeIndexBlock(params ulong[] childIds)
    {
        int entrySize = Layout.SubnodeIndexEntrySize;
        int header = Layout.SubnodeBlockHeaderSize;
        var block = new byte[header + (childIds.Length * entrySize)];
        block[0] = 0x02;
        block[1] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)childIds.Length);

        for (int i = 0; i < childIds.Length; i++)
        {
            Span<byte> row = block.AsSpan(header + (i * entrySize), entrySize);
            Layout.WriteId(row, childIds[i]);
            Layout.WriteId(row.Slice(Layout.IdWidth), childIds[i]);
        }

        return AddBlock(block, isInternal: true);
    }

    /// <summary>
    /// Adds a block with a verbatim payload.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="isInternal">Whether the block is flagged internal (tree metadata, never encoded).</param>
    /// <returns>The block identifier.</returns>
    internal ulong AddRawBlock(byte[] payload, bool isInternal) =>
        AddBlock(payload, isInternal);

    /// <summary>
    /// Declares a node.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="dataBlockId">The data block identifier, or zero.</param>
    /// <param name="subnodeBlockId">The subnode block identifier, or zero.</param>
    /// <param name="parentNodeId">The parent node identifier, or zero.</param>
    /// <returns>This builder.</returns>
    internal PstFixtureBuilder AddNode(uint nodeId, ulong dataBlockId, ulong subnodeBlockId = 0, uint parentNodeId = 0)
    {
        _nodes.Add(new PstNbtEntry(nodeId, dataBlockId, subnodeBlockId, parentNodeId));

        return this;
    }

    /// <summary>
    /// Writes the B-trees and header and returns the file bytes.
    /// </summary>
    /// <returns>The complete file.</returns>
    internal byte[] Build()
    {
        PstLayout layout = Layout;
        PstBref blockRoot = WriteTree(
            [.. _blocks.OrderBy(static b => b.BlockId)],
            PstBTree.BlockPageType,
            layout.BbtLeafStride,
            static b => b.BlockId,
            (destination, row) => WriteBbtEntry(layout, destination, row));

        PstBref nodeRoot = WriteTree(
            [.. _nodes.OrderBy(static n => n.NodeId)],
            PstBTree.NodePageType,
            layout.NbtLeafStride,
            static n => n.NodeId,
            (destination, entry) => WriteNbtEntry(layout, destination, entry));

        byte[] file = [.. _file];
        WriteHeader(file, nodeRoot, blockRoot);

        return file;
    }

    /// <summary>
    /// Builds the file and wraps it in a read-only stream.
    /// </summary>
    /// <returns>The stream, positioned at the start.</returns>
    internal MemoryStream BuildStream() =>
        new(Build(), writable: false);

    /// <summary>
    /// Builds an <c>XBLOCK</c> or <c>XXBLOCK</c> payload.
    /// </summary>
    /// <param name="level">1 for an <c>XBLOCK</c>, 2 for an <c>XXBLOCK</c>.</param>
    /// <param name="totalLength">The declared total payload length.</param>
    /// <param name="childIds">The child block identifiers.</param>
    /// <returns>The block payload.</returns>
    private byte[] BuildTreeBlock(byte level, uint totalLength, ulong[] childIds)
    {
        int idWidth = Layout.IdWidth;
        var block = new byte[8 + (childIds.Length * idWidth)];
        block[0] = 0x01;
        block[1] = level;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)childIds.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), totalLength);

        for (int i = 0; i < childIds.Length; i++)
            Layout.WriteId(block.AsSpan(8 + (i * idWidth)), childIds[i]);

        return block;
    }

    /// <summary>
    /// Writes a block B-tree leaf entry.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="destination">The entry bytes.</param>
    /// <param name="row">The block.</param>
    private static void WriteBbtEntry(PstLayout layout, Span<byte> destination, BlockRow row)
    {
        layout.WriteBref(destination, new PstBref(row.BlockId, row.Offset));
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(layout.BrefSize), row.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(layout.BrefSize + 2), 1);
    }

    /// <summary>
    /// Writes a node B-tree leaf entry.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="destination">The entry bytes.</param>
    /// <param name="entry">The node.</param>
    private static void WriteNbtEntry(PstLayout layout, Span<byte> destination, PstNbtEntry entry)
    {
        layout.WriteId(destination, entry.NodeId);
        layout.WriteId(destination.Slice(layout.IdWidth), entry.DataBlockId);
        layout.WriteId(destination.Slice(layout.IdWidth * 2), entry.SubnodeBlockId);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(layout.IdWidth * 3), entry.ParentNodeId);
    }

    /// <summary>
    /// Derives the permute encoding table by inverting the reader's decode table.
    /// </summary>
    /// <returns>The 256-entry encode table.</returns>
    private static byte[] BuildPermuteEncodeTable()
    {
        byte[] decoded = [.. Enumerable.Range(0, 256).Select(static i => (byte)i)];
        PstCrypt.PermuteDecode(decoded);

        var encode = new byte[256];
        for (int i = 0; i < 256; i++)
            encode[decoded[i]] = (byte)i;

        return encode;
    }

    /// <summary>
    /// Rounds a value up to an alignment.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="alignment">The alignment, a power of two.</param>
    /// <returns>The aligned value.</returns>
    private static long AlignUp(long value, int alignment) =>
        (value + alignment - 1) & ~((long)alignment - 1);

    /// <summary>
    /// Appends a block with its trailer.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="isInternal">Whether the block is internal.</param>
    /// <returns>The block identifier.</returns>
    private ulong AddBlock(byte[] payload, bool isInternal)
    {
        PstLayout layout = Layout;
        ulong blockId = _nextBlockId;
        _nextBlockId += 4;
        if (isInternal)
            blockId |= 0x2;

        long offset = Reserve(BlockAlignment);
        int diskLength = BlockDiskLength(payload.Length, layout);
        var disk = new byte[diskLength];
        payload.CopyTo(disk, 0);

        // Only external blocks carry the content encoding; tree metadata is stored verbatim.
        if (!isInternal)
            Encode(disk.AsSpan(0, payload.Length), blockId);

        Span<byte> trailer = disk.AsSpan(diskLength - layout.BlockTrailerSize);
        BinaryPrimitives.WriteUInt16LittleEndian(trailer, (ushort)payload.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(trailer.Slice(2), PstSource.ComputeSignature((ulong)offset, blockId));
        BinaryPrimitives.WriteUInt32LittleEndian(trailer.Slice(layout.BlockTrailerCrcOffset), PstCrc.Compute(disk.AsSpan(0, payload.Length)));
        layout.WriteId(trailer.Slice(layout.BlockTrailerBlockIdOffset), blockId);

        Append(disk);
        _blocks.Add(new BlockRow(blockId, (ulong)offset, (ushort)payload.Length));

        return blockId;
    }

    /// <summary>
    /// Applies the configured content encoding in place.
    /// </summary>
    /// <param name="data">The payload bytes.</param>
    /// <param name="blockId">The block identifier the cyclic encoding keys on.</param>
    private void Encode(Span<byte> data, ulong blockId)
    {
        switch (CryptMethod)
        {
            case PstCryptMethod.Permute:
                byte[] table = BuildPermuteEncodeTable();
                for (int i = 0; i < data.Length; i++)
                    data[i] = table[data[i]];
                break;

            // The cyclic substitution is its own inverse, so encoding is the same call the reader makes.
            case PstCryptMethod.Cyclic:
                PstCrypt.Cyclic(data, (uint)blockId);
                break;
        }
    }

    /// <summary>
    /// Pads the file to an alignment and returns the aligned offset.
    /// </summary>
    /// <param name="alignment">The alignment.</param>
    /// <returns>The offset the next structure starts at.</returns>
    private long Reserve(int alignment)
    {
        long offset = AlignUp(_file.Count, alignment);
        while (_file.Count < offset)
            _file.Add(0);

        return offset;
    }

    /// <summary>
    /// Appends bytes to the file.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    private void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
            _file.Add(b);
    }

    /// <summary>
    /// Writes a B-tree over entries: leaf pages, and one branch level when they do not fit a single page.
    /// </summary>
    /// <typeparam name="TEntry">The entry type.</typeparam>
    /// <param name="entries">The entries in key order.</param>
    /// <param name="pageType">The page type byte.</param>
    /// <param name="stride">The leaf entry stride.</param>
    /// <param name="keyOf">Extracts an entry's key.</param>
    /// <param name="writeEntry">Writes one leaf entry.</param>
    /// <returns>The root page reference.</returns>
    private PstBref WriteTree<TEntry>(
        TEntry[] entries,
        byte pageType,
        int stride,
        Func<TEntry, ulong> keyOf,
        SpanAction<byte, TEntry> writeEntry)
    {
        PstLayout layout = Layout;
        int perPage = Math.Min(MaxEntriesPerPage, layout.PageEntryArea / stride);
        var leaves = new List<(ulong Key, PstBref Reference)>();

        for (int start = 0; start < entries.Length || start == 0; start += perPage)
        {
            int count = Math.Min(perPage, entries.Length - start);
            var page = new byte[PstLayout.PageSize];
            for (int i = 0; i < count; i++)
                writeEntry(page.AsSpan(i * stride, stride), entries[start + i]);

            leaves.Add((count == 0 ? 0 : keyOf(entries[start]), WritePage(page, count, stride, level: 0, pageType)));
            if (count <= 0)
                break;
        }

        if (leaves.Count == 1)
            return leaves[0].Reference;

        // More leaves than one page: emit a single branch level over them. The fixtures never need a third.
        int branchStride = layout.BranchEntryStride;
        var branch = new byte[PstLayout.PageSize];
        for (int i = 0; i < leaves.Count; i++)
        {
            Span<byte> slot = branch.AsSpan(i * branchStride, branchStride);
            layout.WriteId(slot, leaves[i].Key);
            layout.WriteBref(slot.Slice(layout.IdWidth), leaves[i].Reference);
        }

        return WritePage(branch, leaves.Count, branchStride, level: 1, pageType);
    }

    /// <summary>
    /// Appends a B-tree page with its header fields and trailer.
    /// </summary>
    /// <param name="page">The page bytes with entries already written.</param>
    /// <param name="count">The entry count.</param>
    /// <param name="stride">The entry stride.</param>
    /// <param name="level">The page level.</param>
    /// <param name="pageType">The page type byte.</param>
    /// <returns>The page reference.</returns>
    private PstBref WritePage(byte[] page, int count, int stride, byte level, byte pageType)
    {
        PstLayout layout = Layout;
        ulong blockId = _nextBlockId;
        _nextBlockId += 4;

        long offset = Reserve(PstLayout.PageSize);

        page[layout.PageEntryCountOffset] = (byte)count;
        page[layout.PageEntryCapacityOffset] = (byte)(layout.PageEntryArea / stride);
        page[layout.PageEntryStrideOffset] = (byte)stride;
        page[layout.PageLevelOffset] = level;
        page[layout.PageTrailerOffset] = pageType;
        page[layout.PageTrailerOffset + 1] = pageType;
        BinaryPrimitives.WriteUInt16LittleEndian(page.AsSpan(layout.PageSignatureOffset), PstSource.ComputeSignature((ulong)offset, blockId));
        layout.WriteId(page.AsSpan(layout.PageBlockIdOffset), blockId);
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(layout.PageCrcOffset), PstCrc.Compute(page.AsSpan(0, layout.PageCrcLength)));

        Append(page);

        return new PstBref(blockId, (ulong)offset);
    }

    /// <summary>
    /// Writes the header over the start of the file.
    /// </summary>
    /// <param name="file">The file bytes.</param>
    /// <param name="nodeRoot">The node B-tree root.</param>
    /// <param name="blockRoot">The block B-tree root.</param>
    private void WriteHeader(byte[] file, PstBref nodeRoot, PstBref blockRoot)
    {
        PstLayout layout = Layout;
        Span<byte> header = file.AsSpan(0, layout.HeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x4E444221);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(8), 0x4D53);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(10), Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(12), 19);
        header[14] = 0x01;
        header[15] = 0x01;

        layout.WriteId(header.Slice(layout.FileLengthOffset), (ulong)file.Length);
        layout.WriteBref(header.Slice(layout.NbtRootOffset), nodeRoot);
        layout.WriteBref(header.Slice(layout.BbtRootOffset), blockRoot);

        header[layout.SentinelOffset] = Sentinel;
        header[layout.CryptMethodOffset] = RawCryptMethod ?? (byte)CryptMethod;

        uint crc = PstCrc.Compute(header.Slice(8, 471));
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4), WriteValidHeaderCrc ? crc : crc ^ 0xFFFFFFFF);
    }

    /// <summary>
    /// A block written to the file.
    /// </summary>
    /// <param name="BlockId">The block identifier.</param>
    /// <param name="Offset">The file offset.</param>
    /// <param name="Length">The payload length.</param>
    private readonly record struct BlockRow(ulong BlockId, ulong Offset, ushort Length);
}
