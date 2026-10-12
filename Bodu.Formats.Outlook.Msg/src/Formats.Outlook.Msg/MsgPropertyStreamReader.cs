// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Parses the <c>__properties_version1.0</c> stream of an MS-OXMSG storage into its header and fixed 16-byte property
/// records.
/// </summary>
internal static class MsgPropertyStreamReader
{
    /// <summary>The size of one property record, in bytes.</summary>
    private const int EntrySize = 16;

    /// <summary>
    /// Parses a property stream.
    /// </summary>
    /// <param name="data">The complete stream payload.</param>
    /// <param name="kind">The storage kind, which determines the header size.</param>
    /// <param name="header">When this method returns, the decoded header fields (zeros for kinds without them).</param>
    /// <returns>
    /// The property records in stream order. A record whose 16 bytes are all zero is padding, not a property (tag 0
    /// names none), and is skipped at every validation level.
    /// </returns>
    /// <exception cref="OutlookMsgFormatException">
    /// The payload is shorter than the header for <paramref name="kind" />, or the bytes after the header are not a
    /// whole number of 16-byte records.
    /// </exception>
    internal static MsgPropertyEntry[] Read(ReadOnlySpan<byte> data, MsgPropertyStreamKind kind, out MsgPropertyStreamHeader header)
    {
        int headerSize = GetHeaderSize(kind);
        if (data.Length < headerSize || (data.Length - headerSize) % EntrySize != 0)
        {
            throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgPropertyStream, data.Length));
        }

        header = kind == MsgPropertyStreamKind.RecipientOrAttachment
            ? default
            : new MsgPropertyStreamHeader(
                BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8)),
                BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(12)),
                BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(16)),
                BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(20)));

        ReadOnlySpan<byte> records = data.Slice(headerSize);
        int count = records.Length / EntrySize;
        int padding = 0;
        for (int i = 0; i < count; i++)
        {
            if (IsPadding(records.Slice(i * EntrySize, EntrySize)))
                padding++;
        }

        var entries = new MsgPropertyEntry[count - padding];
        int next = 0;
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = records.Slice(i * EntrySize, EntrySize);
            if (IsPadding(record))
                continue;

            entries[next++] = new MsgPropertyEntry(
                BinaryPrimitives.ReadUInt32LittleEndian(record),
                BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(4)),
                BinaryPrimitives.ReadUInt64LittleEndian(record.Slice(8)));
        }

        return entries;
    }

    /// <summary>
    /// Determines whether a property record is padding.
    /// </summary>
    /// <param name="record">The 16-byte record.</param>
    /// <returns>
    /// <see langword="true" /> when every byte is zero: tag 0 names no property, so a record of zeros carries none. A
    /// tag-0 record with nonzero flags or value is decoded like any other record.
    /// </returns>
    private static bool IsPadding(ReadOnlySpan<byte> record) =>
        !record.ContainsAnyExcept((byte)0);

    /// <summary>
    /// Returns the header size for a storage kind.
    /// </summary>
    /// <param name="kind">The storage kind.</param>
    /// <returns>The header size in bytes: 32 for the root, 24 for an embedded message, 8 otherwise.</returns>
    internal static int GetHeaderSize(MsgPropertyStreamKind kind) =>
        kind switch
        {
            MsgPropertyStreamKind.Root => 32,
            MsgPropertyStreamKind.EmbeddedMessage => 24,
            _ => 8,
        };
}
