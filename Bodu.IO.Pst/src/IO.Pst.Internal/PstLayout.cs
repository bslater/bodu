// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Describes the on-disk widths and offsets of one PST format variant: the Unicode layout (<c>wVer</c> 23, 64-bit block
/// identifiers and offsets) or the ANSI layout (<c>wVer</c> 14 and 15, 32-bit identifiers and offsets).
/// </summary>
/// <remarks>
/// <para>
/// MS-PST defines the same structures for both variants; they differ only in field widths and the offsets that follow
/// from them. Every NDB reader and the test fixture writer consult one of the two instances rather than literal
/// offsets, so the two variants cannot drift apart. The LTP layer (heap-on-node, BTree-on-heap, property and table
/// contexts) is identical in both variants apart from the usable block payload, which the row matrix depends on.
/// </para>
/// <para>
/// Offsets were verified against the reference corpus (<c>ansi/sample2.pst</c>) rather than transcribed from the
/// specification text: the ANSI <c>ROOT</c> record sits at 164, its <c>ibFileEof</c> is a 32-bit value at 168, and the
/// ANSI page and block trailers place the block identifier before the checksum, the reverse of the Unicode order.
/// </para>
/// </remarks>
internal sealed class PstLayout
{
    /// <summary>The page size shared by both variants.</summary>
    internal const int PageSize = 512;

    /// <summary>The largest block on disk, trailer included, shared by both variants.</summary>
    internal const int MaxBlockSize = 8192;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstLayout" /> class.
    /// </summary>
    /// <param name="format">The format the layout describes.</param>
    /// <param name="idWidth">The width of a block identifier or file offset.</param>
    /// <param name="headerSize">The header size.</param>
    /// <param name="fileLengthOffset">The offset of <c>ibFileEof</c>.</param>
    /// <param name="nbtRootOffset">The offset of the node B-tree root <c>BREF</c>.</param>
    /// <param name="bbtRootOffset">The offset of the block B-tree root <c>BREF</c>.</param>
    /// <param name="sentinelOffset">The offset of <c>bSentinel</c>.</param>
    /// <param name="pageEntryArea">The number of page bytes available to entries.</param>
    /// <param name="pageTrailerOffset">The offset of the page trailer.</param>
    /// <param name="pageBlockIdOffset">The offset of the page trailer's block identifier.</param>
    /// <param name="pageCrcOffset">The offset of the page trailer's checksum.</param>
    /// <param name="blockTrailerSize">The block trailer size.</param>
    /// <param name="blockTrailerBlockIdOffset">The offset of the block identifier within the block trailer.</param>
    /// <param name="blockTrailerCrcOffset">The offset of the checksum within the block trailer.</param>
    private PstLayout(
        PstFileFormat format,
        int idWidth,
        int headerSize,
        int fileLengthOffset,
        int nbtRootOffset,
        int bbtRootOffset,
        int sentinelOffset,
        int pageEntryArea,
        int pageTrailerOffset,
        int pageBlockIdOffset,
        int pageCrcOffset,
        int blockTrailerSize,
        int blockTrailerBlockIdOffset,
        int blockTrailerCrcOffset)
    {
        Format = format;
        IdWidth = idWidth;
        HeaderSize = headerSize;
        FileLengthOffset = fileLengthOffset;
        NbtRootOffset = nbtRootOffset;
        BbtRootOffset = bbtRootOffset;
        SentinelOffset = sentinelOffset;
        PageEntryArea = pageEntryArea;
        PageTrailerOffset = pageTrailerOffset;
        PageBlockIdOffset = pageBlockIdOffset;
        PageCrcOffset = pageCrcOffset;
        BlockTrailerSize = blockTrailerSize;
        BlockTrailerBlockIdOffset = blockTrailerBlockIdOffset;
        BlockTrailerCrcOffset = blockTrailerCrcOffset;
    }

    /// <summary>Gets the Unicode layout (<c>wVer</c> 23).</summary>
    internal static PstLayout Unicode { get; } = new(
        PstFileFormat.Unicode,
        idWidth: 8,
        headerSize: 564,
        fileLengthOffset: 184,
        nbtRootOffset: 216,
        bbtRootOffset: 232,
        sentinelOffset: 512,
        pageEntryArea: 488,
        pageTrailerOffset: 496,
        pageBlockIdOffset: 504,
        pageCrcOffset: 500,
        blockTrailerSize: 16,
        blockTrailerBlockIdOffset: 8,
        blockTrailerCrcOffset: 4);

    /// <summary>Gets the ANSI layout (<c>wVer</c> 14 and 15).</summary>
    internal static PstLayout Ansi { get; } = new(
        PstFileFormat.Ansi,
        idWidth: 4,
        headerSize: 512,
        fileLengthOffset: 168,
        nbtRootOffset: 184,
        bbtRootOffset: 192,
        sentinelOffset: 460,
        pageEntryArea: 496,
        pageTrailerOffset: 500,
        pageBlockIdOffset: 504,
        pageCrcOffset: 508,
        blockTrailerSize: 12,
        blockTrailerBlockIdOffset: 4,
        blockTrailerCrcOffset: 8);

    /// <summary>Gets the format the layout describes.</summary>
    internal PstFileFormat Format { get; }

    /// <summary>Gets the width of a block identifier or file offset: 8 for Unicode, 4 for ANSI.</summary>
    internal int IdWidth { get; }

    /// <summary>Gets the size of a <c>BREF</c> (a block identifier followed by an offset).</summary>
    internal int BrefSize =>
        IdWidth * 2;

    /// <summary>Gets the header size.</summary>
    internal int HeaderSize { get; }

    /// <summary>Gets the offset of <c>ibFileEof</c>, which is <see cref="IdWidth" /> bytes wide.</summary>
    internal int FileLengthOffset { get; }

    /// <summary>Gets the offset of the node B-tree root <c>BREF</c>.</summary>
    internal int NbtRootOffset { get; }

    /// <summary>Gets the offset of the block B-tree root <c>BREF</c>.</summary>
    internal int BbtRootOffset { get; }

    /// <summary>Gets the offset of <c>bSentinel</c>; <c>bCryptMethod</c> follows it.</summary>
    internal int SentinelOffset { get; }

    /// <summary>Gets the offset of <c>bCryptMethod</c>.</summary>
    internal int CryptMethodOffset =>
        SentinelOffset + 1;

    /// <summary>Gets the number of page bytes available to B-tree entries.</summary>
    internal int PageEntryArea { get; }

    /// <summary>Gets the offset of the page's entry count (<c>cEnt</c>).</summary>
    internal int PageEntryCountOffset =>
        PageEntryArea;

    /// <summary>Gets the offset of the page's entry capacity (<c>cEntMax</c>).</summary>
    internal int PageEntryCapacityOffset =>
        PageEntryArea + 1;

    /// <summary>Gets the offset of the page's entry stride (<c>cbEnt</c>).</summary>
    internal int PageEntryStrideOffset =>
        PageEntryArea + 2;

    /// <summary>Gets the offset of the page's level (<c>cLevel</c>).</summary>
    internal int PageLevelOffset =>
        PageEntryArea + 3;

    /// <summary>Gets the offset of the page trailer, whose first two bytes are the page type repeated.</summary>
    internal int PageTrailerOffset { get; }

    /// <summary>Gets the offset of the page trailer's signature.</summary>
    internal int PageSignatureOffset =>
        PageTrailerOffset + 2;

    /// <summary>Gets the offset of the page trailer's block identifier.</summary>
    internal int PageBlockIdOffset { get; }

    /// <summary>Gets the offset of the page trailer's checksum.</summary>
    internal int PageCrcOffset { get; }

    /// <summary>Gets the number of leading page bytes the page checksum covers.</summary>
    internal int PageCrcLength =>
        PageTrailerOffset;

    /// <summary>Gets the block trailer size.</summary>
    internal int BlockTrailerSize { get; }

    /// <summary>Gets the offset of the block identifier within the block trailer.</summary>
    internal int BlockTrailerBlockIdOffset { get; }

    /// <summary>Gets the offset of the checksum within the block trailer.</summary>
    internal int BlockTrailerCrcOffset { get; }

    /// <summary>Gets the largest block payload: the block size less the trailer.</summary>
    internal int MaxBlockPayload =>
        MaxBlockSize - BlockTrailerSize;

    /// <summary>Gets the stride of an intermediate B-tree entry: a key followed by a <c>BREF</c>.</summary>
    internal int BranchEntryStride =>
        IdWidth + BrefSize;

    /// <summary>Gets the stride of a node B-tree leaf entry: <c>nid</c>, <c>bidData</c>, <c>bidSub</c>, <c>nidParent</c>.</summary>
    internal int NbtLeafStride =>
        (IdWidth * 3) + 4;

    /// <summary>Gets the stride of a block B-tree leaf entry: a <c>BREF</c>, <c>cb</c>, <c>cRef</c>.</summary>
    internal int BbtLeafStride =>
        BrefSize + 4;

    /// <summary>
    /// Gets the size of a subnode block header (<c>SLBLOCK</c> / <c>SIBLOCK</c>): <c>btype</c>, <c>cLevel</c>, <c>cEnt</c>,
    /// plus four bytes of padding in the Unicode layout only.
    /// </summary>
    internal int SubnodeBlockHeaderSize =>
        IdWidth == 8 ? 8 : 4;

    /// <summary>Gets the size of a subnode leaf entry (<c>SLENTRY</c>): <c>nid</c>, <c>bidData</c>, <c>bidSub</c>.</summary>
    internal int SubnodeLeafEntrySize =>
        IdWidth * 3;

    /// <summary>Gets the size of a subnode index entry (<c>SIENTRY</c>): <c>nid</c>, <c>bid</c>.</summary>
    internal int SubnodeIndexEntrySize =>
        IdWidth * 2;

    /// <summary>
    /// Selects the layout a header version declares.
    /// </summary>
    /// <param name="version">The header's <c>wVer</c>.</param>
    /// <returns>The layout, or <see langword="null" /> when the version is neither Unicode nor ANSI.</returns>
    internal static PstLayout? FromVersion(ushort version) =>
        version switch
        {
            14 or 15 => Ansi,
            23 => Unicode,
            _ => null,
        };

    /// <summary>
    /// Reads a block identifier or offset of this layout's width, widened to 64 bits.
    /// </summary>
    /// <param name="data">The bytes at the field.</param>
    /// <returns>The value.</returns>
    internal ulong ReadId(ReadOnlySpan<byte> data) =>
        IdWidth == 8
            ? BinaryPrimitives.ReadUInt64LittleEndian(data)
            : BinaryPrimitives.ReadUInt32LittleEndian(data);

    /// <summary>
    /// Writes a block identifier or offset in this layout's width.
    /// </summary>
    /// <param name="destination">The bytes at the field.</param>
    /// <param name="value">The value; must fit the width.</param>
    internal void WriteId(Span<byte> destination, ulong value)
    {
        if (IdWidth == 8)
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt32LittleEndian(destination, checked((uint)value));
    }

    /// <summary>
    /// Reads a <c>BREF</c> of this layout's width.
    /// </summary>
    /// <param name="data">The bytes at the record.</param>
    /// <returns>The block reference.</returns>
    internal PstBref ReadBref(ReadOnlySpan<byte> data) =>
        new(ReadId(data), ReadId(data.Slice(IdWidth)));

    /// <summary>
    /// Writes a <c>BREF</c> in this layout's width.
    /// </summary>
    /// <param name="destination">The bytes at the record.</param>
    /// <param name="bref">The block reference.</param>
    internal void WriteBref(Span<byte> destination, PstBref bref)
    {
        WriteId(destination, bref.BlockId);
        WriteId(destination.Slice(IdWidth), bref.Offset);
    }
}
