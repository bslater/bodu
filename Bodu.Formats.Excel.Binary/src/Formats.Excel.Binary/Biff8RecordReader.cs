// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8RecordReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Splits a workbook stream into its sequence of BIFF records, each a type identifier and a payload slice.
/// </summary>
/// <remarks>
/// Each record is encoded as a two-byte little-endian type, a two-byte little-endian payload length, and the payload.
/// The reader does not copy payloads; each <see cref="Biff8Record.Payload" /> is a slice of the supplied memory.
/// </remarks>
internal sealed class Biff8RecordReader
{
    /// <summary>
    /// The workbook stream bytes.
    /// </summary>
    private readonly ReadOnlyMemory<byte> _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8RecordReader" /> class.
    /// </summary>
    /// <param name="data">The workbook stream bytes to split into records.</param>
    internal Biff8RecordReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    /// <summary>
    /// Reads every record from the workbook stream, in order.
    /// </summary>
    /// <returns>The ordered list of records.</returns>
    /// <exception cref="Biff8FormatException">
    /// Thrown when a record declares a length that runs past the end of the stream.
    /// </exception>
    internal List<Biff8Record> ReadAll()
    {
        List<Biff8Record> records = new();
        ReadOnlySpan<byte> span = _data.Span;
        var position = 0;

        while (position + 4 <= span.Length)
        {
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position));
            int length = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position + 2));
            var payloadStart = position + 4;

            if (payloadStart + length > span.Length)
                throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

            records.Add(new Biff8Record(id, _data.Slice(payloadStart, length)));
            position = payloadStart + length;
        }

        return records;
    }
}
