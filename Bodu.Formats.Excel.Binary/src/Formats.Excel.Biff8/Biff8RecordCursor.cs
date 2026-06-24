// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8RecordCursor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Walks a buffer of BIFF records forward, yielding each record's type and payload slice without allocating.
/// </summary>
/// <remarks>
/// Each record is encoded as a two-byte little-endian type, a two-byte little-endian payload length, and the payload.
/// The cursor never copies payloads; each <see cref="Biff8Record.Payload" /> is a slice of the supplied buffer. A
/// record whose declared length runs past the end of the buffer, or a trailing fragment too short to form a record
/// header, is rejected as malformed.
/// </remarks>
internal ref struct Biff8RecordCursor
{
    /// <summary>The buffer of records being walked.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The current read position within the buffer.</summary>
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8RecordCursor" /> struct.
    /// </summary>
    /// <param name="data">The buffer of BIFF records to walk.</param>
    internal Biff8RecordCursor(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    /// Gets the current read position within the buffer, in bytes from its start.
    /// </summary>
    /// <value>The byte offset of the next record header.</value>
    public readonly int Position => _position;

    /// <summary>
    /// Attempts to read the next record from the buffer.
    /// </summary>
    /// <param name="record">When this method returns, the record that was read when one is available.</param>
    /// <returns>
    /// <see langword="true" /> when a record was read; <see langword="false" /> at the clean end of the buffer.
    /// </returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when a record declares a length that runs past the end of the buffer, or a trailing fragment of one to
    /// three bytes is too short to form a record header.
    /// </exception>
    public bool TryRead(out Biff8Record record)
    {
        int remaining = _data.Length - _position;
        if (remaining == 0)
        {
            record = default;
            return false;
        }

        if (remaining < 4)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8TrailingBytes, remaining));
        }

        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position));
        int length = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position + 2));
        int payloadStart = _position + 4;
        if (payloadStart + length > _data.Length)
            throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        record = new Biff8Record(id, _data.Slice(payloadStart, length));
        _position = payloadStart + length;
        return true;
    }
}
