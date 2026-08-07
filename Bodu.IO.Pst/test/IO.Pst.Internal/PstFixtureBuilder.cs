// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFixtureBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Authors a minimal but structurally valid Unicode-format PST container in memory: a header, a block B-tree over
/// blocks the caller supplies, and a node B-tree over nodes the caller declares.
/// </summary>
/// <remarks>
/// <para>
/// The reference corpus is four small writer-produced files. They exercise the common path well, but every payload in
/// them fits a single block, so the multi-block data trees - <c>XBLOCK</c> and <c>XXBLOCK</c>, the layout every node
/// larger than roughly 8&#160;KB uses - had never been read by a test, and neither had the cyclic content encoding, the
/// subnode index block, or any of the malformed-geometry rejections. Producing those with a real writer is not
/// practical; authoring the container is.
/// </para>
/// <para>
/// Everything the reader validates is emitted correctly, including the trailer checksums and signatures that only
/// <see cref="PstValidationLevel.Strict" /> inspects, so a fixture can be opened at any validation level and a test
/// that wants a rejection has to introduce the defect deliberately. The layout is deliberately simple rather than
/// writer-faithful: blocks and pages are appended in allocation order and never reused.
/// </para>
/// </remarks>
internal sealed class PstFixtureBuilder
{
    /// <summary>The page size and the B-tree page alignment.</summary>
    private const int PageSize = 512;

    /// <summary>The block alignment and trailer-inclusive block granularity.</summary>
    private const int BlockAlignment = 64;

    /// <summary>The block trailer size.</summary>
    private const int BlockTrailerSize = 16;

    /// <summary>The largest payload that still fits a single block once padded and trailered.</summary>
    internal const int MaxBlockPayload = 8176;

    /// <summary>The offset at which block and page content begins, clear of the 564-byte header.</summary>
    private const int ContentStart = 1024;

    /// <summary>The stride of a leaf block-tree entry.</summary>
    private const int BbtEntryStride = 24;

    /// <summary>The stride of a leaf node-tree entry.</summary>
    private const int NbtEntryStride = 32;

    /// <summary>The stride of an intermediate entry (key plus reference).</summary>
    private const int BranchEntryStride = 24;

    /// <summary>The bytes of the file being assembled, from offset zero.</summary>
    private readonly List<byte> _file = [.. new byte[ContentStart]];

    /// <summary>The block-tree leaf rows, in allocation order.</summary>
    private readonly List<BlockRow> _blocks = [];

    /// <summary>The node-tree leaf rows, in declaration order.</summary>
    private readonly List<PstNbtEntry> _nodes = [];

    /// <summary>The next block identifier to hand out.</summary>
    private ulong _nextBlockId = 4;

    /// <summary>
    /// Gets or sets the content encoding the header declares and the builder applies to external blocks.
    /// </summary>
    /// <value>The encoding method; <see cref="PstCryptMethod.None" /> by default.</value>
    internal PstCryptMethod CryptMethod { get; set; } = PstCryptMethod.None;

    /// <summary>
    /// Gets or sets the maximum number of leaf entries placed on one B-tree page.
    /// </summary>
    /// <value>
    /// The per-page entry cap. Left unbounded the trees are a single leaf page; lowering it forces the builder to emit
    /// a branch level, which is how a test reaches the descent path.
    /// </value>
    internal int MaxEntriesPerPage { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the format version the header records.
    /// </summary>
    /// <value>The <c>wVer</c> value; <c>23</c> (Unicode) by default.</value>
    internal ushort Version { get; set; } = 23;

    /// <summary>
    /// Gets or sets the value written to the header's sentinel byte at offset 512.
    /// </summary>
    /// <value>The sentinel; <c>0x80</c> by default.</value>
    internal byte Sentinel { get; set; } = 0x80;

    /// <summary>
    /// Gets or sets the raw content-encoding byte the header records.
    /// </summary>
    /// <value>
    /// The <c>bCryptMethod</c> value, or <see langword="null" /> to derive it from <see cref="CryptMethod" />.
    /// </value>
    internal byte? RawCryptMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header's partial checksum is written correctly.
    /// </summary>
    /// <value><see langword="true" /> to write the correct checksum; <see langword="false" /> to corrupt it.</value>
    internal bool WriteValidHeaderCrc { get; set; } = true;

    /// <summary>
    /// Gets the absolute offset at which each block was written, keyed by identifier.
    /// </summary>
    /// <value>The block offsets, populated as blocks are added.</value>
    internal IReadOnlyDictionary<ulong, long> BlockOffsets =>
        _blocks.ToDictionary(static b => b.BlockId, static b => (long)b.Offset);

    /// <summary>
    /// Computes the on-disk length of a block, including its padding and trailer.
    /// </summary>
    /// <param name="payloadLength">The payload length.</param>
    /// <returns>The on-disk length.</returns>
    internal static int BlockDiskLength(int payloadLength) =>
        (payloadLength + BlockTrailerSize + BlockAlignment - 1) & ~(BlockAlignment - 1);

    /// <summary>
    /// Reads the absolute offset of the block B-tree root page from an assembled container.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <returns>The root page offset.</returns>
    internal static long ReadBlockTreeRootOffset(byte[] file) =>
        BinaryPrimitives.ReadInt64LittleEndian(file.AsSpan(240));

    /// <summary>
    /// Reads the absolute offset of the node B-tree root page from an assembled container.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <returns>The root page offset.</returns>
    internal static long ReadNodeTreeRootOffset(byte[] file) =>
        BinaryPrimitives.ReadInt64LittleEndian(file.AsSpan(224));

    /// <summary>
    /// Recomputes the header's partial checksum over an assembled container.
    /// </summary>
    /// <param name="file">The container bytes, modified in place.</param>
    /// <remarks>
    /// The checksum covers the 471 bytes from the client magic, which includes both B-tree root references. A test
    /// that patches one of those must repair the checksum, or the header is refused before the patched reference is
    /// ever followed and the test proves nothing about the guard it was aimed at.
    /// </remarks>
    internal static void RepairHeaderChecksum(byte[] file) =>
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), PstCrc.Compute(file.AsSpan(8, 471)));

    /// <summary>
    /// Adds an external data block holding the supplied payload.
    /// </summary>
    /// <param name="payload">The block payload, before encoding.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddDataBlock(byte[] payload) =>
        AddBlock(payload, isInternal: false);

    /// <summary>
    /// Adds an <c>XBLOCK</c> listing the supplied data blocks.
    /// </summary>
    /// <param name="totalLength">The logical payload length the block header records.</param>
    /// <param name="childIds">The data-block identifiers, in order.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddXBlock(uint totalLength, params ulong[] childIds) =>
        AddBlock(BuildTreeBlock(level: 1, totalLength, childIds), isInternal: true);

    /// <summary>
    /// Adds an <c>XXBLOCK</c> listing the supplied <c>XBLOCK</c>s.
    /// </summary>
    /// <param name="totalLength">The logical payload length the block header records.</param>
    /// <param name="childIds">The <c>XBLOCK</c> identifiers, in order.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddXXBlock(uint totalLength, params ulong[] childIds) =>
        AddBlock(BuildTreeBlock(level: 2, totalLength, childIds), isInternal: true);

    /// <summary>
    /// Adds an <c>SLBLOCK</c> holding the supplied subnode rows directly.
    /// </summary>
    /// <param name="entries">The subnode rows: node identifier, data-block identifier, nested subnode identifier.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddSubnodeLeafBlock(params (uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)[] entries)
    {
        var block = new byte[8 + (entries.Length * 24)];
        block[0] = 0x02;
        block[1] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)entries.Length);

        for (int i = 0; i < entries.Length; i++)
        {
            Span<byte> row = block.AsSpan(8 + (i * 24), 24);
            BinaryPrimitives.WriteUInt64LittleEndian(row, entries[i].NodeId);
            BinaryPrimitives.WriteUInt64LittleEndian(row.Slice(8), entries[i].DataBlockId);
            BinaryPrimitives.WriteUInt64LittleEndian(row.Slice(16), entries[i].SubnodeBlockId);
        }

        return AddBlock(block, isInternal: true);
    }

    /// <summary>
    /// Adds an <c>SIBLOCK</c> listing the supplied <c>SLBLOCK</c>s.
    /// </summary>
    /// <param name="childIds">The <c>SLBLOCK</c> identifiers, in order.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddSubnodeIndexBlock(params ulong[] childIds)
    {
        var block = new byte[8 + (childIds.Length * 16)];
        block[0] = 0x02;
        block[1] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)childIds.Length);

        for (int i = 0; i < childIds.Length; i++)
        {
            Span<byte> row = block.AsSpan(8 + (i * 16), 16);
            BinaryPrimitives.WriteUInt64LittleEndian(row, childIds[i]);
            BinaryPrimitives.WriteUInt64LittleEndian(row.Slice(8), childIds[i]);
        }

        return AddBlock(block, isInternal: true);
    }

    /// <summary>
    /// Adds a block whose bytes are supplied verbatim, for fixtures that need a malformed payload.
    /// </summary>
    /// <param name="payload">The block payload.</param>
    /// <param name="isInternal">Whether the identifier carries the internal bit.</param>
    /// <returns>The identifier of the new block.</returns>
    internal ulong AddRawBlock(byte[] payload, bool isInternal) =>
        AddBlock(payload, isInternal);

    /// <summary>
    /// Declares a node in the node B-tree.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="dataBlockId">The data-block identifier, or <c>0</c> for no data.</param>
    /// <param name="subnodeBlockId">The subnode-tree block identifier, or <c>0</c> for none.</param>
    /// <param name="parentNodeId">The parent node identifier.</param>
    /// <returns>This builder, for chaining.</returns>
    internal PstFixtureBuilder AddNode(uint nodeId, ulong dataBlockId, ulong subnodeBlockId = 0, uint parentNodeId = 0)
    {
        _nodes.Add(new PstNbtEntry(nodeId, dataBlockId, subnodeBlockId, parentNodeId));

        return this;
    }

    /// <summary>
    /// Assembles the container and returns its bytes.
    /// </summary>
    /// <returns>The complete file.</returns>
    internal byte[] Build()
    {
        PstBref blockRoot = WriteTree(
            [.. _blocks.OrderBy(static b => b.BlockId)],
            PstBTree.BlockPageType,
            BbtEntryStride,
            static b => b.BlockId,
            WriteBbtEntry);

        PstBref nodeRoot = WriteTree(
            [.. _nodes.OrderBy(static n => n.NodeId)],
            PstBTree.NodePageType,
            NbtEntryStride,
            static n => n.NodeId,
            WriteNbtEntry);

        byte[] file = [.. _file];
        WriteHeader(file, nodeRoot, blockRoot);

        return file;
    }

    /// <summary>
    /// Assembles the container and returns it as a seekable stream positioned at its start.
    /// </summary>
    /// <returns>The container stream.</returns>
    internal MemoryStream BuildStream() =>
        new(Build(), writable: false);

    /// <summary>
    /// Builds an <c>XBLOCK</c> or <c>XXBLOCK</c> payload.
    /// </summary>
    /// <param name="level">The tree level: <c>1</c> for an <c>XBLOCK</c>, <c>2</c> for an <c>XXBLOCK</c>.</param>
    /// <param name="totalLength">The logical payload length recorded at offset 4.</param>
    /// <param name="childIds">The child identifiers.</param>
    /// <returns>The block payload.</returns>
    private static byte[] BuildTreeBlock(byte level, uint totalLength, ulong[] childIds)
    {
        var block = new byte[8 + (childIds.Length * 8)];
        block[0] = 0x01;
        block[1] = level;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), (ushort)childIds.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), totalLength);

        for (int i = 0; i < childIds.Length; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(8 + (i * 8)), childIds[i]);

        return block;
    }

    /// <summary>
    /// Writes a leaf block-tree entry.
    /// </summary>
    /// <param name="destination">The entry bytes.</param>
    /// <param name="row">The block row.</param>
    private static void WriteBbtEntry(Span<byte> destination, BlockRow row)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, row.BlockId);
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), row.Offset);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(16), row.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(18), 1);
    }

    /// <summary>
    /// Writes a leaf node-tree entry.
    /// </summary>
    /// <param name="destination">The entry bytes.</param>
    /// <param name="entry">The node entry.</param>
    private static void WriteNbtEntry(Span<byte> destination, PstNbtEntry entry)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, entry.NodeId);
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), entry.DataBlockId);
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(16), entry.SubnodeBlockId);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(24), entry.ParentNodeId);
    }

    /// <summary>
    /// Derives the encoding permutation by inverting the decoding one, so the builder needs no table of its own.
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
    /// Rounds an offset up to an alignment boundary.
    /// </summary>
    /// <param name="value">The offset.</param>
    /// <param name="alignment">The alignment, a power of two.</param>
    /// <returns>The aligned offset.</returns>
    private static long AlignUp(long value, int alignment) =>
        (value + alignment - 1) & ~((long)alignment - 1);

    /// <summary>
    /// Appends a block, emitting its trailer and recording its block-tree row.
    /// </summary>
    /// <param name="payload">The payload, before encoding.</param>
    /// <param name="isInternal">Whether the identifier carries the internal bit.</param>
    /// <returns>The identifier of the new block.</returns>
    private ulong AddBlock(byte[] payload, bool isInternal)
    {
        ulong blockId = _nextBlockId;
        _nextBlockId += 4;
        if (isInternal)
            blockId |= 0x2;

        long offset = Reserve(BlockAlignment);
        int diskLength = (payload.Length + BlockTrailerSize + BlockAlignment - 1) & ~(BlockAlignment - 1);
        var disk = new byte[diskLength];
        payload.CopyTo(disk, 0);

        // Only external blocks carry the content encoding; tree metadata is stored verbatim.
        if (!isInternal)
            Encode(disk.AsSpan(0, payload.Length), blockId);

        Span<byte> trailer = disk.AsSpan(diskLength - BlockTrailerSize);
        BinaryPrimitives.WriteUInt16LittleEndian(trailer, (ushort)payload.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(trailer.Slice(2), PstSource.ComputeSignature((ulong)offset, blockId));
        BinaryPrimitives.WriteUInt32LittleEndian(trailer.Slice(4), PstCrc.Compute(disk.AsSpan(0, payload.Length)));
        BinaryPrimitives.WriteUInt64LittleEndian(trailer.Slice(8), blockId);

        Append(disk);
        _blocks.Add(new BlockRow(blockId, (ulong)offset, (ushort)payload.Length));

        return blockId;
    }

    /// <summary>
    /// Applies the declared content encoding to a block's bytes in place.
    /// </summary>
    /// <param name="data">The payload to encode.</param>
    /// <param name="blockId">The block identifier, which keys the cyclic substitution.</param>
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
    /// Advances the file to an alignment boundary and returns the offset reserved for the next append.
    /// </summary>
    /// <param name="alignment">The alignment required.</param>
    /// <returns>The aligned offset.</returns>
    private long Reserve(int alignment)
    {
        long offset = AlignUp(_file.Count, alignment);
        while (_file.Count < offset)
            _file.Add(0);

        return offset;
    }

    /// <summary>
    /// Appends bytes at the current end of the file.
    /// </summary>
    /// <param name="bytes">The bytes to append.</param>
    private void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
            _file.Add(b);
    }

    /// <summary>
    /// Writes a B-tree over the supplied leaf entries and returns a reference to its root page.
    /// </summary>
    /// <typeparam name="TEntry">The leaf entry type.</typeparam>
    /// <param name="entries">The leaf entries, in key order.</param>
    /// <param name="pageType">The page type byte.</param>
    /// <param name="stride">The leaf entry stride.</param>
    /// <param name="keyOf">Extracts an entry's key.</param>
    /// <param name="writeEntry">Writes an entry into its slot.</param>
    /// <returns>The root page reference.</returns>
    private PstBref WriteTree<TEntry>(
        TEntry[] entries,
        byte pageType,
        int stride,
        Func<TEntry, ulong> keyOf,
        SpanAction<byte, TEntry> writeEntry)
    {
        int perPage = Math.Min(MaxEntriesPerPage, 488 / stride);
        var leaves = new List<(ulong Key, PstBref Reference)>();

        for (int start = 0; start < entries.Length || start == 0; start += perPage)
        {
            int count = Math.Min(perPage, entries.Length - start);
            var page = new byte[PageSize];
            for (int i = 0; i < count; i++)
                writeEntry(page.AsSpan(i * stride, stride), entries[start + i]);

            leaves.Add((count == 0 ? 0 : keyOf(entries[start]), WritePage(page, count, stride, level: 0, pageType)));
            if (count <= 0)
                break;
        }

        if (leaves.Count == 1)
            return leaves[0].Reference;

        // More leaves than one page: emit a single branch level over them. The fixtures never need a third.
        var branch = new byte[PageSize];
        for (int i = 0; i < leaves.Count; i++)
        {
            Span<byte> slot = branch.AsSpan(i * BranchEntryStride, BranchEntryStride);
            BinaryPrimitives.WriteUInt64LittleEndian(slot, leaves[i].Key);
            BinaryPrimitives.WriteUInt64LittleEndian(slot.Slice(8), leaves[i].Reference.BlockId);
            BinaryPrimitives.WriteUInt64LittleEndian(slot.Slice(16), leaves[i].Reference.Offset);
        }

        return WritePage(branch, leaves.Count, BranchEntryStride, level: 1, pageType);
    }

    /// <summary>
    /// Appends a B-tree page, completing its geometry fields and trailer.
    /// </summary>
    /// <param name="page">The 512-byte page, with its entries already written.</param>
    /// <param name="count">The entry count.</param>
    /// <param name="stride">The entry stride.</param>
    /// <param name="level">The tree level.</param>
    /// <param name="pageType">The page type byte.</param>
    /// <returns>The reference to the written page.</returns>
    private PstBref WritePage(byte[] page, int count, int stride, byte level, byte pageType)
    {
        ulong blockId = _nextBlockId;
        _nextBlockId += 4;

        long offset = Reserve(PageSize);

        page[488] = (byte)count;
        page[489] = (byte)(488 / stride);
        page[490] = (byte)stride;
        page[491] = level;
        page[496] = pageType;
        page[497] = pageType;
        BinaryPrimitives.WriteUInt16LittleEndian(page.AsSpan(498), PstSource.ComputeSignature((ulong)offset, blockId));
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(500), PstCrc.Compute(page.AsSpan(0, 496)));
        BinaryPrimitives.WriteUInt64LittleEndian(page.AsSpan(504), blockId);

        Append(page);

        return new PstBref(blockId, (ulong)offset);
    }

    /// <summary>
    /// Writes the file header over the assembled content.
    /// </summary>
    /// <param name="file">The complete file bytes.</param>
    /// <param name="nodeRoot">The node B-tree root reference.</param>
    /// <param name="blockRoot">The block B-tree root reference.</param>
    private void WriteHeader(byte[] file, PstBref nodeRoot, PstBref blockRoot)
    {
        Span<byte> header = file.AsSpan(0, PstHeader.UnicodeHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x4E444221);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(8), 0x4D53);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(10), Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(12), 19);
        header[14] = 0x01;
        header[15] = 0x01;

        BinaryPrimitives.WriteInt64LittleEndian(header.Slice(184), file.Length);
        WriteBref(header.Slice(216), nodeRoot);
        WriteBref(header.Slice(232), blockRoot);

        header[512] = Sentinel;
        header[513] = RawCryptMethod ?? (byte)CryptMethod;

        uint crc = PstCrc.Compute(header.Slice(8, 471));
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4), WriteValidHeaderCrc ? crc : crc ^ 0xFFFFFFFF);
    }

    /// <summary>
    /// Writes a Unicode <c>BREF</c>.
    /// </summary>
    /// <param name="destination">The 16 bytes of the reference.</param>
    /// <param name="bref">The reference to write.</param>
    private static void WriteBref(Span<byte> destination, PstBref bref)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, bref.BlockId);
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), bref.Offset);
    }

    /// <summary>
    /// A block's block-tree row: its identifier, its offset, and its payload length.
    /// </summary>
    /// <param name="BlockId">The block identifier.</param>
    /// <param name="Offset">The absolute offset of the block.</param>
    /// <param name="Length">The payload length.</param>
    private readonly record struct BlockRow(ulong BlockId, ulong Offset, ushort Length);
}
