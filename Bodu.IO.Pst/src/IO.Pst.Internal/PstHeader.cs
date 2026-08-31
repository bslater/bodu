// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents the decoded PST file header (MS-PST §2.2.2.6, Unicode layout) — the format discriminator, the
/// content-encoding method, and the root references of the two B-trees.
/// </summary>
internal sealed class PstHeader
{
    /// <summary>The header size of the Unicode layout, in bytes.</summary>
    internal const int UnicodeHeaderSize = 564;

    /// <summary>The <c>!BDN</c> magic.</summary>
    private const uint Magic = 0x4E444221;

    /// <summary>The <c>SM</c> client magic.</summary>
    private const ushort MagicClient = 0x4D53;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstHeader" /> class.
    /// </summary>
    /// <param name="format">The recognized format variant.</param>
    /// <param name="cryptMethod">The content-encoding method.</param>
    /// <param name="fileLength">The recorded end-of-file offset.</param>
    /// <param name="nbtRoot">The node B-tree root reference.</param>
    /// <param name="bbtRoot">The block B-tree root reference.</param>
    private PstHeader(PstFileFormat format, PstCryptMethod cryptMethod, long fileLength, PstBref nbtRoot, PstBref bbtRoot)
    {
        Format = format;
        CryptMethod = cryptMethod;
        FileLength = fileLength;
        NbtRoot = nbtRoot;
        BbtRoot = bbtRoot;
    }

    /// <summary>
    /// Gets the recognized format variant.
    /// </summary>
    /// <value>Always <see cref="PstFileFormat.Unicode" /> for a parsed header; other variants throw at parse.</value>
    internal PstFileFormat Format { get; }

    /// <summary>
    /// Gets the content-encoding method applied to external block data.
    /// </summary>
    /// <value>The declared method.</value>
    internal PstCryptMethod CryptMethod { get; }

    /// <summary>
    /// Gets the recorded end-of-file offset (<c>ibFileEof</c>).
    /// </summary>
    /// <value>The file length the writer recorded.</value>
    internal long FileLength { get; }

    /// <summary>
    /// Gets the node B-tree root reference.
    /// </summary>
    /// <value>The root page reference.</value>
    internal PstBref NbtRoot { get; }

    /// <summary>
    /// Gets the block B-tree root reference.
    /// </summary>
    /// <value>The root page reference.</value>
    internal PstBref BbtRoot { get; }

    /// <summary>
    /// Determines whether a buffer begins with the PST magics, without validating further.
    /// </summary>
    /// <param name="data">The first bytes of a candidate file; at least 12 bytes.</param>
    /// <returns><see langword="true" /> when the <c>!BDN</c> and <c>SM</c> magics are present.</returns>
    internal static bool IsPstHeader(ReadOnlySpan<byte> data) =>
        data.Length >= 12
            && BinaryPrimitives.ReadUInt32LittleEndian(data) == Magic
            && BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)) == MagicClient;

    /// <summary>
    /// Parses and validates a Unicode-format header.
    /// </summary>
    /// <param name="data">The first <see cref="UnicodeHeaderSize" /> bytes of the file.</param>
    /// <param name="validationLevel">The validation level governing checksum verification.</param>
    /// <returns>The decoded header.</returns>
    /// <exception cref="PstFileFormatException">
    /// The magics are absent, the version is unknown, the sentinel is wrong, or a checksum fails.
    /// </exception>
    /// <exception cref="PstUnsupportedFormatException">
    /// The file is a recognized ANSI or 4&#160;KiB-page variant, or uses an unsupported content encoding.
    /// </exception>
    internal static PstHeader Parse(ReadOnlySpan<byte> data, PstValidationLevel validationLevel)
    {
        if (!IsPstHeader(data) || data.Length < UnicodeHeaderSize)
            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10));
        switch (version)
        {
            case 14 or 15:
                throw new PstUnsupportedFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Op_NotSupported_PstFormat, PstFileFormat.Ansi));
            case >= 36:
                throw new PstUnsupportedFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Op_NotSupported_PstFormat, PstFileFormat.Ost4K));
            case not 23:
                throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);
        }

        // dwCRCPartial covers the 471 bytes from wMagicClient; verified except under Minimal validation.
        if (validationLevel != PstValidationLevel.Minimal)
        {
            uint recordedCrc = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));
            if (PstCrc.Compute(data.Slice(8, 471)) != recordedCrc)
                throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeaderCrc, PstFileError.InvalidHeader);
        }

        if (data[512] != 0x80)
            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);

        byte cryptMethod = data[513];
        if (cryptMethod is not(0x00 or 0x01 or 0x02))
        {
            throw new PstUnsupportedFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Op_NotSupported_PstCryptMethod, cryptMethod));
        }

        // The ROOT record sits at offset 180; its BREFs at relative offsets 36 (NBT) and 52 (BBT).
        long fileLength = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(184));
        var nbtRoot = ReadBref(data.Slice(216));
        var bbtRoot = ReadBref(data.Slice(232));

        return new PstHeader(PstFileFormat.Unicode, (PstCryptMethod)cryptMethod, fileLength, nbtRoot, bbtRoot);
    }

    /// <summary>
    /// Reads a Unicode <c>BREF</c>: a block identifier followed by an absolute offset.
    /// </summary>
    /// <param name="data">The 16 bytes of the reference.</param>
    /// <returns>The decoded reference.</returns>
    private static PstBref ReadBref(ReadOnlySpan<byte> data) =>
        new(BinaryPrimitives.ReadUInt64LittleEndian(data), BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(8)));
}
