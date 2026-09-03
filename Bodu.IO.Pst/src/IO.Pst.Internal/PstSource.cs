// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

using Bodu.Collections.Generic;

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

    /// <summary>The position of the container's first byte within the stream.</summary>
    private readonly long _baseOffset;

    /// <summary>The optional LRU cache of validated pages, keyed by page block identifier; <see langword="null" /> when caching is disabled.</summary>
    private readonly EvictingDictionary<ulong, byte[]>? _pageCache;

    /// <summary>The optional LRU cache of decoded block payloads, keyed by block identifier; <see langword="null" /> when caching is disabled.</summary>
    private readonly EvictingDictionary<ulong, byte[]>? _blockCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstSource" /> class.
    /// </summary>
    /// <param name="stream">The readable, seekable source stream.</param>
    /// <param name="header">The parsed file header.</param>
    /// <param name="options">The session options: validation level, cache budget, and resource limits.</param>
    /// <param name="baseOffset">The position of the container's first byte within the stream.</param>
    internal PstSource(Stream stream, PstHeader header, PstFileOptions options, long baseOffset)
    {
        _stream = stream;
        _baseOffset = baseOffset;
        Header = header;
        ValidationLevel = options.ValidationLevel;
        MaxNodeDataLength = options.MaxNodeDataLength;
        MaxDataTreeLeaves = options.MaxDataTreeLeaves;
        if (options.BlockCacheSize > 0)
        {
            // Pages and blocks live in separate caches: a block identifier can carry any bit pattern, so no key
            // transformation keeps the two identifier spaces apart.
            _pageCache = new EvictingDictionary<ulong, byte[]>(options.BlockCacheSize, EvictingDictionaryPolicy.LeastRecentlyUsed);
            _blockCache = new EvictingDictionary<ulong, byte[]>(options.BlockCacheSize, EvictingDictionaryPolicy.LeastRecentlyUsed);
        }
    }

    /// <summary>
    /// Gets the largest node payload, in bytes, a buffered read may materialize.
    /// </summary>
    /// <value>The configured <see cref="PstFileOptions.MaxNodeDataLength" />.</value>
    internal long MaxNodeDataLength { get; }

    /// <summary>
    /// Gets the largest number of leaf blocks a data tree may reference.
    /// </summary>
    /// <value>The configured <see cref="PstFileOptions.MaxDataTreeLeaves" />.</value>
    internal int MaxDataTreeLeaves { get; }

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
        // Subtract rather than add: an offset near the top of the range would wrap a sum negative and slip past.
        if (offset < 0 || buffer.Length > _stream.Length - _baseOffset - offset)
            throw InvalidRead(offset);

        try
        {
            _stream.Position = _baseOffset + offset;
            _stream.ReadExactly(buffer);
        }
        catch (EndOfStreamException ex)
        {
            throw InvalidRead(offset, ex);
        }
        catch (IOException ex)
        {
            throw InvalidRead(offset, ex);
        }
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
        // A cached page was fully validated when it was filled; only the (cheap) expected-type check depends on the
        // caller, so it is re-run on every hit. A shape mismatch falls through to a fresh, fully validated read.
        if (_pageCache is not null
            && _pageCache.TryGetValue(bref.BlockId, out byte[] cachedPage)
            && cachedPage.Length == PageSize
            && cachedPage[496] == expectedType)
        {
            return cachedPage;
        }

        var page = new byte[PageSize];
        ReadAt((long)bref.Offset, page);

        // PAGETRAILER at 496: ptype, ptypeRepeat, wSig, dwCRC, bid.
        byte pageType = page[496];
        if (pageType != expectedType || page[497] != pageType)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPage, bref.Offset), PstFileError.InvalidPage);
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
                    CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPageTrailer, bref.Offset), PstFileError.InvalidPage);
            }
        }

        if (_pageCache is not null)
            _pageCache[bref.BlockId] = page;
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
        if (_blockCache is not null && _blockCache.TryGetValue(entry.Bref.BlockId, out byte[] cachedPayload))
            return cachedPayload;

        int payloadLength = entry.Length;
        int diskLength = (payloadLength + BlockTrailerSize + 63) & ~63;
        if (payloadLength == 0 || diskLength > MaxBlockSize)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset), PstFileError.InvalidBlock);
        }

        var block = new byte[diskLength];
        ReadAt((long)entry.Bref.Offset, block);

        // BLOCKTRAILER at the end: cb, wSig, dwCRC, bid.
        ReadOnlySpan<byte> trailer = block.AsSpan(diskLength - BlockTrailerSize);
        if (BinaryPrimitives.ReadUInt16LittleEndian(trailer) != payloadLength)
        {
            throw new PstFileFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset), PstFileError.InvalidBlock);
        }

        if (ValidationLevel == PstValidationLevel.Strict)
        {
            if (BinaryPrimitives.ReadUInt64LittleEndian(trailer.Slice(8)) != entry.Bref.BlockId
                || BinaryPrimitives.ReadUInt32LittleEndian(trailer.Slice(4)) != PstCrc.Compute(block.AsSpan(0, payloadLength))
                || BinaryPrimitives.ReadUInt16LittleEndian(trailer.Slice(2)) != ComputeSignature(entry.Bref.Offset, entry.Bref.BlockId))
            {
                throw new PstFileFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, entry.Bref.Offset), PstFileError.InvalidBlock);
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

        if (_blockCache is not null)
            _blockCache[entry.Bref.BlockId] = payload;
        return payload;
    }

    /// <summary>
    /// Creates the exception for a read that falls outside the stream or fails at the I/O layer.
    /// </summary>
    /// <param name="offset">The container-relative offset of the read.</param>
    /// <param name="inner">The I/O failure, when one occurred.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException InvalidRead(long offset, Exception? inner = null) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstBlock, offset), PstFileError.InvalidBlock, inner);

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
