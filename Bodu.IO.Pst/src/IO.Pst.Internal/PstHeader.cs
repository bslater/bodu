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
    /// <summary>The Unicode header size, which is also the most a reader needs to buffer to parse either format.</summary>
    internal const int UnicodeHeaderSize = 564;

    /// <summary>The <c>!BDN</c> magic.</summary>
    private const uint Magic = 0x4E444221;

    /// <summary>The <c>SM</c> client magic.</summary>
    private const ushort MagicClient = 0x4D53;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstHeader" /> class.
    /// </summary>
    /// <param name="layout">The layout the header's version selects.</param>
    /// <param name="cryptMethod">The content encoding applied to external blocks.</param>
    /// <param name="fileLength">The file length the header records.</param>
    /// <param name="nbtRoot">The node B-tree root page reference.</param>
    /// <param name="bbtRoot">The block B-tree root page reference.</param>
    private PstHeader(PstLayout layout, PstCryptMethod cryptMethod, long fileLength, PstBref nbtRoot, PstBref bbtRoot)
    {
        Layout = layout;
        CryptMethod = cryptMethod;
        FileLength = fileLength;
        NbtRoot = nbtRoot;
        BbtRoot = bbtRoot;
    }

    /// <summary>
    /// Gets the file format the header declares.
    /// </summary>
    /// <value><see cref="PstFileFormat.Unicode" /> or <see cref="PstFileFormat.Ansi" />; the 4 KiB-page OST variant throws at parse.</value>
    internal PstFileFormat Format =>
        Layout.Format;

    /// <summary>
    /// Gets the on-disk layout (widths and offsets) every NDB reader uses for this file.
    /// </summary>
    /// <value>The layout selected by the header's <c>wVer</c>.</value>
    internal PstLayout Layout { get; }

    /// <summary>
    /// Gets the content encoding applied to external blocks.
    /// </summary>
    /// <value>The <c>bCryptMethod</c> value.</value>
    internal PstCryptMethod CryptMethod { get; }

    /// <summary>
    /// Gets the file length the header records (<c>ibFileEof</c>).
    /// </summary>
    /// <value>The length in bytes.</value>
    internal long FileLength { get; }

    /// <summary>
    /// Gets the node B-tree root page reference.
    /// </summary>
    /// <value>The <c>BREFNBT</c> value.</value>
    internal PstBref NbtRoot { get; }

    /// <summary>
    /// Gets the block B-tree root page reference.
    /// </summary>
    /// <value>The <c>BREFBBT</c> value.</value>
    internal PstBref BbtRoot { get; }

    /// <summary>
    /// Determines whether bytes begin with the PST magic values.
    /// </summary>
    /// <param name="data">The leading file bytes.</param>
    /// <returns><see langword="true" /> when <c>dwMagic</c> and <c>wMagicClient</c> match.</returns>
    internal static bool IsPstHeader(ReadOnlySpan<byte> data) =>
        data.Length >= 12
            && BinaryPrimitives.ReadUInt32LittleEndian(data) == Magic
            && BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)) == MagicClient;

    /// <summary>
    /// Parses and validates a header.
    /// </summary>
    /// <param name="data">The leading file bytes: at least <see cref="UnicodeHeaderSize" /> for a Unicode file, 512 for an ANSI file.</param>
    /// <param name="validationLevel">The validation level; the partial checksum is skipped under <see cref="PstValidationLevel.Minimal" />.</param>
    /// <returns>The parsed header.</returns>
    /// <exception cref="PstFileFormatException">The header is malformed, too short for its format, or fails its checksum.</exception>
    /// <exception cref="PstUnsupportedFormatException">The file is a 4 KiB-page OST or uses an unknown content encoding.</exception>
    /// <remarks>
    /// The version word at offset 10 selects the layout before anything else is read, so the length check, the
    /// sentinel and crypt-method bytes, and the <c>ROOT</c> record are all located per format. The 471-byte partial
    /// checksum range is the same in both formats.
    /// </remarks>
    internal static PstHeader Parse(ReadOnlySpan<byte> data, PstValidationLevel validationLevel)
    {
        if (!IsPstHeader(data))
            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10));
        PstLayout? layout = PstLayout.FromVersion(version);
        if (layout is null)
        {
            if (version >= 36)
            {
                throw new PstUnsupportedFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Op_NotSupported_PstFormat, PstFileFormat.Ost4K));
            }

            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);
        }

        if (data.Length < layout.HeaderSize)
            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);

        // dwCRCPartial covers the 471 bytes from wMagicClient in both formats; verified except under Minimal validation.
        if (validationLevel != PstValidationLevel.Minimal)
        {
            uint recordedCrc = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));
            if (PstCrc.Compute(data.Slice(8, 471)) != recordedCrc)
                throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeaderCrc, PstFileError.InvalidHeader);
        }

        if (data[layout.SentinelOffset] != 0x80)
            throw new PstFileFormatException(PstResourceStrings.Format_Invalid_PstHeader, PstFileError.InvalidHeader);

        byte cryptMethod = data[layout.CryptMethodOffset];
        if (cryptMethod is not(0x00 or 0x01 or 0x02))
        {
            throw new PstUnsupportedFormatException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.Op_NotSupported_PstCryptMethod, cryptMethod));
        }

        long fileLength = (long)layout.ReadId(data.Slice(layout.FileLengthOffset));
        PstBref nbtRoot = layout.ReadBref(data.Slice(layout.NbtRootOffset));
        PstBref bbtRoot = layout.ReadBref(data.Slice(layout.BbtRootOffset));

        return new PstHeader(layout, (PstCryptMethod)cryptMethod, fileLength, nbtRoot, bbtRoot);
    }

}
