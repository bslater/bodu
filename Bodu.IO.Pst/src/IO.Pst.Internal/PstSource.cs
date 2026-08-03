// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Provides validated random-access reads over an open PST source: raw ranges, 512-byte pages with their trailers, and
/// blocks with padding stripped, trailers verified, and external data decoded per the file's content encoding.
/// </summary>
/// <remarks>
/// Reads seek the underlying stream directly — the file is never buffered whole. The session is single-threaded,
/// matching the public surface's documented contract.
/// </remarks>
internal sealed class PstSource
{
    /// <summary>The page size of the Unicode format.</summary>
    internal const int PageSize = 512;

    /// <summary>The block trailer size of the Unicode format.</summary>
    private const int BlockTrailerSize = 16;

    /// <summary>The maximum on-disk block size, including padding and trailer.</summary>
    private const int MaxBlockSize = 8192;

    /// <summary>The source stream.</summary>
    private readonly Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstSource" /> class.
    /// </summary>
    /// <param name="stream">The readable, seekable source stream.</param>
    /// <param name="header">The parsed file header.</param>
    /// <param name="validationLevel">The validation level governing checksum and signature verification.</param>
    internal PstSource(Stream stream, PstHeader header, PstValidationLevel validationLevel)
    {
        _stream = stream;
        Header = header;
        ValidationLevel = validationLevel;
    }

    /// <summary>
    /// Gets the parsed file header.
    /// </summary>
    /// <value>The header.</value>
    internal PstHeader Header { get; }

    /// <summary>
    /// Gets the validation level the source was opened with.
    /// </summary>
    /// <value>The validation level.</value>
    internal PstValidationLevel ValidationLevel { get; }

    /// <summary>
    /// Reads an exact byte range from the file.
    /// </summary>
    /// <param name="offset">The absolute offset.</param>
    /// <param name="buffer">The destination buffer; filled completely.</param>
    /// <exception cref="PstFileFormatException">The range escapes the stream.</exception>
    internal void ReadAt(long offset, Span<byte> buffer)
    {
        if (offset < 0 || offset + buffer.Length > _stream.Length)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, offset));
        }

        _stream.Position = offset;
        _stream.ReadExactly(buffer);
    }

    /// <summary>
    /// Reads a 512-byte B-tree page and validates its trailer.
    /// </summary>
    /// <param name="bref">The page reference.</param>
    /// <param name="expectedType">
    /// The expected page type: <c>0x80</c> for the block tree, <c>0x81</c> for the node tree.
    /// </param>
    /// <returns>The page bytes.</returns>
    /// <exception cref="PstFileFormatException">
    /// The trailer's type, identifier, checksum, or signature is wrong.
    /// </exception>
    internal byte[] ReadPage(PstBref bref, byte expectedType)
    {
        var page = new byte[PageSize];
        ReadAt((long)bref.Offset, page);

        // PAGETRAILER at 496: ptype, ptypeRepeat, wSig, dwCRC, bid.
        byte pageType = page[496];
        if (pageType != expectedType || page[497] != pageType)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPage, bref.Offset));
        }

        if (ValidationLevel == PstValidationLevel.Strict)
        {
            ulong recordedId = BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(504));
            uint recordedCrc = BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(500));
            ushort recordedSignature = BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(498));
            if (recordedId != bref.BlockId
                || recordedCrc != PstCrc.Compute(page.AsSpan(0, 496))
                || recordedSignature != ComputeSignature(bref.Offset, bref.BlockId))
            {
                throw new PstFileFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPageTrailer, bref.Offset));
            }
        }

        return page;
    }

    /// <summary>
    /// Reads a block's payload, validating its trailer and decoding external data per the file's content encoding.
    /// </summary>
    /// <param name="entry">The block's B-tree entry.</param>
    /// <returns>The payload bytes, padding and trailer stripped.</returns>
    /// <exception cref="PstFileFormatException">The block geometry or trailer is invalid.</exception>
    internal byte[] ReadBlock(PstBbtEntry entry)
    {
        int payloadLength = entry.Length;
        int diskLength = (payloadLength + BlockTrailerSize + 63) & ~63;
        if (payloadLength == 0 || diskLength > MaxBlockSize)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset));
        }

        var block = new byte[diskLength];
        ReadAt((long)entry.Bref.Offset, block);

        // BLOCKTRAILER at the end: cb, wSig, dwCRC, bid.
        ReadOnlySpan<byte> trailer = block.AsSpan(diskLength - BlockTrailerSize);
        if (BinaryPrimitives.ReadUInt16LittleEndian(trailer) != payloadLength)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset));
        }

        if (ValidationLevel == PstValidationLevel.Strict)
        {
            if (BinaryPrimitives.ReadUInt64LittleEndian(trailer.Slice(8)) != entry.Bref.BlockId
                || BinaryPrimitives.ReadUInt32LittleEndian(trailer.Slice(4)) != PstCrc.Compute(block.AsSpan(0, payloadLength))
                || BinaryPrimitives.ReadUInt16LittleEndian(trailer.Slice(2)) != ComputeSignature(entry.Bref.Offset, entry.Bref.BlockId))
            {
                throw new PstFileFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset));
            }
        }

        var payload = block.AsSpan(0, payloadLength).ToArray();
        if (!entry.Bref.IsInternal)
        {
            // Only external (leaf data) blocks are encoded; tree metadata is stored verbatim.
            switch (Header.CryptMethod)
            {
                case PstCryptMethod.Permute:
                    PstCrypt.PermuteDecode(payload);
                    break;
                case PstCryptMethod.Cyclic:
                    PstCrypt.Cyclic(payload, (uint)entry.Bref.BlockId);
                    break;
            }
        }

        return payload;
    }

    /// <summary>
    /// Computes the MS-PST §5.5 block/page signature of an offset and identifier.
    /// </summary>
    /// <param name="offset">The absolute offset.</param>
    /// <param name="blockId">The block identifier.</param>
    /// <returns>The 16-bit signature.</returns>
    internal static ushort ComputeSignature(ulong offset, ulong blockId)
    {
        ulong value = offset ^ blockId;
        return (ushort)((ushort)(value >> 16) ^ (ushort)value);
    }
}
