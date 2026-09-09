// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Biff;

/// <summary>
/// Provides a forward-only, allocation-free reader over a BIFF5 or BIFF8 record stream. The reader is a
/// <see langword="ref struct" /> over the supplied bytes: each call to <see cref="Read" /> advances to the next
/// physical record and exposes its identifier, length, and payload slice, and typed accessors decode the records the
/// codec names.
/// </summary>
/// <remarks>
/// <para>
/// The reader walks physical records only. A <c>CONTINUE</c> record is reported as a record like any other; the one
/// structure whose logical value routinely spans continuation records, the BIFF8 shared string table, is decoded by
/// <see cref="BiffSstReader" />, and <see cref="TryReadContinuation" /> lets a caller consume a following
/// <c>CONTINUE</c> record for any other structure.
/// </para>
/// <para>
/// The BIFF version is established from the first <c>BOF</c> record (or supplied through
/// <see cref="BiffReaderOptions.Version" />) and the code page for byte strings from the <c>CODEPAGE</c> record (or
/// <see cref="BiffReaderOptions.CodePage" />); the reader keeps no other state, and in particular no workbook, sheet,
/// or cell model. A record whose identifier the codec does not name is never an error — it remains readable through
/// <see cref="RecordId" /> and <see cref="ValueSpan" />.
/// </para>
/// <para>
/// To read incrementally, capture <see cref="CurrentState" /> and <see cref="BytesConsumed" /> after a pass, then
/// construct the next reader over the remaining bytes with that state. With <c>isFinalBlock</c> set to
/// <see langword="false" />, an incomplete trailing record makes <see cref="Read" /> return <see langword="false" />
/// without consuming it so the caller can supply more data; with <see langword="true" />, the same condition is a
/// format error.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var reader = new BiffReader(workbookStreamBytes);
/// while (reader.Read())
/// {
///     switch (reader.RecordType)
///     {
///         case BiffRecordType.Bof:
///             Console.WriteLine($"{reader.Version} {reader.GetBof().SubstreamType}");
///             break;
///         case BiffRecordType.Number:
///             BiffNumberRecord number = reader.GetNumber();
///             Console.WriteLine($"R{number.Row}C{number.Column} = {number.Value}");
///             break;
///         default:
///             // Unknown records stay accessible: reader.RecordId, reader.ValueSpan.
///             break;
///     }
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public ref partial struct BiffReader
{
    /// <summary>The BIFF5 <c>BOF</c> version marker.</summary>
    private const ushort Biff5VersionMarker = 0x0500;

    /// <summary>The BIFF8 <c>BOF</c> version marker.</summary>
    private const ushort Biff8VersionMarker = 0x0600;

    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>Whether the source holds the end of the stream.</summary>
    private readonly bool _isFinalBlock;

    /// <summary>The resumable state: version and code page.</summary>
    private BiffReaderState _state;

    /// <summary>The offset of the next record header.</summary>
    private int _position;

    /// <summary>The offset of the current record's header.</summary>
    private int _recordStart;

    /// <summary>The offset of the current record's payload.</summary>
    private int _payloadStart;

    /// <summary>The length of the current record's payload.</summary>
    private int _payloadLength;

    /// <summary>The identifier of the current record.</summary>
    private ushort _recordId;

    /// <summary>Whether a record is current.</summary>
    private bool _hasRecord;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffReader" /> struct over the supplied bytes, which hold the
    /// complete stream.
    /// </summary>
    /// <param name="data">The BIFF record bytes.</param>
    public BiffReader(ReadOnlySpan<byte> data)
        : this(data, isFinalBlock: true, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffReader" /> struct over the supplied bytes using the supplied
    /// options.
    /// </summary>
    /// <param name="data">The BIFF record bytes.</param>
    /// <param name="options">The options seeding the version and code page.</param>
    public BiffReader(ReadOnlySpan<byte> data, BiffReaderOptions options)
        : this(data, isFinalBlock: true, new BiffReaderState(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffReader" /> struct over a block of a stream, continuing from a
    /// previously captured state.
    /// </summary>
    /// <param name="data">The BIFF record bytes, beginning at a record header.</param>
    /// <param name="isFinalBlock">
    /// Whether <paramref name="data" /> holds the end of the stream. When <see langword="false" />, an incomplete
    /// trailing record is left unconsumed rather than rejected.
    /// </param>
    /// <param name="state">The state captured from the reader that processed the preceding bytes.</param>
    public BiffReader(ReadOnlySpan<byte> data, bool isFinalBlock, BiffReaderState state)
    {
        _data = data;
        _isFinalBlock = isFinalBlock;
        _state = state;
        _position = 0;
        _recordStart = 0;
        _payloadStart = 0;
        _payloadLength = 0;
        _recordId = 0;
        _hasRecord = false;
    }

    /// <summary>
    /// Gets the number of bytes consumed so far: the offset of the next record header.
    /// </summary>
    /// <value>The read position within the supplied bytes.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the state needed to continue the stream in a new reader.
    /// </summary>
    /// <value>The established version and code page.</value>
    public readonly BiffReaderState CurrentState => _state;

    /// <summary>
    /// Gets a value indicating whether the supplied bytes hold the end of the stream.
    /// </summary>
    /// <value><see langword="true" /> when an incomplete trailing record is a format error.</value>
    public readonly bool IsFinalBlock => _isFinalBlock;

    /// <summary>
    /// Gets the BIFF version of the stream.
    /// </summary>
    /// <value>
    /// The version established from the first <c>BOF</c> record or the options, or <see cref="BiffVersion.Unknown" />
    /// before either.
    /// </value>
    public readonly BiffVersion Version => _state._version;

    /// <summary>
    /// Gets the code page in effect for byte strings.
    /// </summary>
    /// <value>
    /// The code page from the most recent <c>CODEPAGE</c> record or the options, or
    /// <see cref="BiffLimits.DefaultCodePage" />.
    /// </value>
    public readonly int CodePage => _state.CodePage;

    /// <summary>
    /// Gets a value indicating whether a record is current.
    /// </summary>
    /// <value><see langword="true" /> after a successful <see cref="Read" /> until the stream ends.</value>
    public readonly bool HasRecord => _hasRecord;

    /// <summary>
    /// Gets the 16-bit identifier of the current record.
    /// </summary>
    /// <value>The raw identifier, or zero when no record is current.</value>
    public readonly ushort RecordId => _recordId;

    /// <summary>
    /// Gets the identifier of the current record as a <see cref="BiffRecordType" />.
    /// </summary>
    /// <value>
    /// The typed identifier, <see cref="BiffRecordType.None" /> when no record is current. The value may not correspond
    /// to a defined member when the record is one the codec does not name.
    /// </value>
    public readonly BiffRecordType RecordType => (BiffRecordType)_recordId;

    /// <summary>
    /// Gets the length of the current record's payload, in bytes.
    /// </summary>
    /// <value>The payload length, or zero when no record is current.</value>
    public readonly int RecordLength => _payloadLength;

    /// <summary>
    /// Gets the byte offset of the current record's header within the supplied bytes.
    /// </summary>
    /// <value>The header offset.</value>
    public readonly int RecordStartIndex => _recordStart;

    /// <summary>
    /// Gets the header of the current record.
    /// </summary>
    /// <value>The identifier and declared payload length.</value>
    public readonly BiffRecordHeader Header => new(_recordId, (ushort)_payloadLength);

    /// <summary>
    /// Gets the payload of the current record as a slice of the supplied bytes.
    /// </summary>
    /// <value>The payload, excluding the four-byte header; empty when no record is current.</value>
    public readonly ReadOnlySpan<byte> ValueSpan => _data.Slice(_payloadStart, _payloadLength);

    /// <summary>
    /// Gets a value indicating whether the current record is a <c>CONTINUE</c> record carrying the overflow of the
    /// record before it.
    /// </summary>
    /// <value><see langword="true" /> when the current record is <see cref="BiffRecordType.Continue" />.</value>
    public readonly bool IsContinuation => _hasRecord && _recordId == (ushort)BiffRecordType.Continue;

    /// <summary>
    /// Advances to the next physical record.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a record was read; <see langword="false" /> at the clean end of the supplied bytes,
    /// or — when <see cref="IsFinalBlock" /> is <see langword="false" /> — when the next record is incomplete and more
    /// data is needed.
    /// </returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the stream holds the end of the data and a trailing fragment is too short to form a record header, a
    /// record's declared payload runs past the end of the data, or a <c>BOF</c> record disagrees with the established
    /// version.
    /// </exception>
    /// <exception cref="BiffUnsupportedVersionException">
    /// Thrown when the first <c>BOF</c> record declares a version other than BIFF5 or BIFF8, or the stream opens with a
    /// BIFF2, BIFF3, or BIFF4 beginning-of-file record.
    /// </exception>
    public bool Read()
    {
        if (!TryFrame(out ushort id, out int payloadStart, out int payloadLength))
        {
            ClearCurrent();
            return false;
        }

        Commit(id, payloadStart, payloadLength);
        return true;
    }

    /// <summary>
    /// Consumes the record that follows the current one when, and only when, it is a <c>CONTINUE</c> record, making it
    /// current and exposing its payload.
    /// </summary>
    /// <param name="payload">When this method returns, the continuation payload when one was consumed.</param>
    /// <returns>
    /// <see langword="true" /> when a <c>CONTINUE</c> record was consumed; <see langword="false" /> when the next
    /// record is any other kind, the stream has ended, or more data is needed.
    /// </returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the stream holds the end of the data and the continuation record's declared payload runs past it.
    /// </exception>
    public bool TryReadContinuation(out ReadOnlySpan<byte> payload)
    {
        if (!BiffRecordHeader.TryParse(_data.Slice(_position), out BiffRecordHeader header)
            || header.Id != (ushort)BiffRecordType.Continue
            || !TryFrame(out ushort id, out int payloadStart, out int payloadLength))
        {
            payload = default;
            return false;
        }

        Commit(id, payloadStart, payloadLength);
        payload = ValueSpan;
        return true;
    }

    /// <summary>
    /// Locates the next record without committing to it.
    /// </summary>
    /// <param name="id">When this method returns, the record identifier.</param>
    /// <param name="payloadStart">When this method returns, the payload offset.</param>
    /// <param name="payloadLength">When this method returns, the payload length.</param>
    /// <returns><see langword="true" /> when a complete record is available.</returns>
    /// <exception cref="BiffFormatException">Thrown when the block is final and the record is truncated.</exception>
    private readonly bool TryFrame(out ushort id, out int payloadStart, out int payloadLength)
    {
        id = 0;
        payloadStart = 0;
        payloadLength = 0;

        int remaining = _data.Length - _position;
        if (remaining == 0)
            return false;

        if (remaining < BiffLimits.RecordHeaderSize)
        {
            if (!_isFinalBlock)
                return false;

            throw new BiffFormatException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffTrailingBytes, remaining),
                _position);
        }

        id = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position));
        payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position + 2));
        payloadStart = _position + BiffLimits.RecordHeaderSize;
        if (payloadStart + payloadLength > _data.Length)
        {
            if (!_isFinalBlock)
                return false;

            throw new BiffFormatException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffRecordOverrun, id, _position, payloadLength),
                _position);
        }

        return true;
    }

    /// <summary>
    /// Leaves the reader with no current record, keeping its position.
    /// </summary>
    private void ClearCurrent()
    {
        _hasRecord = false;
        _recordId = 0;
        _recordStart = _position;
        _payloadStart = _position;
        _payloadLength = 0;
    }

    /// <summary>
    /// Makes a framed record current and folds any state it carries into the reader.
    /// </summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="payloadStart">The payload offset.</param>
    /// <param name="payloadLength">The payload length.</param>
    /// <exception cref="BiffFormatException">
    /// Thrown when a <c>BOF</c> record disagrees with the established version.
    /// </exception>
    /// <exception cref="BiffUnsupportedVersionException">Thrown when the version is not BIFF5 or BIFF8.</exception>
    private void Commit(ushort id, int payloadStart, int payloadLength)
    {
        _recordStart = _position;
        _recordId = id;
        _payloadStart = payloadStart;
        _payloadLength = payloadLength;
        _hasRecord = true;
        _position = payloadStart + payloadLength;

        switch ((BiffRecordType)id)
        {
            case BiffRecordType.Bof:
                EstablishVersion();
                break;

            case BiffRecordType.CodePage when payloadLength >= 2:
                _state._codePage = BiffTextEncoding.Normalize(BinaryPrimitives.ReadUInt16LittleEndian(ValueSpan));
                break;

            case BiffRecordType.Biff2Bof or BiffRecordType.Biff3Bof or BiffRecordType.Biff4Bof when _state._version == BiffVersion.Unknown:
                throw new BiffUnsupportedVersionException(
                    string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Op_NotSupported_BiffLegacyBof, id),
                    id);

            default:
                break;
        }
    }

    /// <summary>
    /// Establishes or verifies the stream version from the current <c>BOF</c> record.
    /// </summary>
    /// <exception cref="BiffFormatException">
    /// Thrown when the record is too short to carry a version, or disagrees with the established version.
    /// </exception>
    /// <exception cref="BiffUnsupportedVersionException">Thrown when the version is not BIFF5 or BIFF8.</exception>
    private void EstablishVersion()
    {
        ushort marker = BiffPayload.ReadUInt16(ValueSpan, 0, BiffRecordType.Bof);
        BiffVersion declared = marker switch
        {
            Biff5VersionMarker => BiffVersion.Biff5,
            Biff8VersionMarker => BiffVersion.Biff8,
            _ => throw new BiffUnsupportedVersionException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Op_NotSupported_BiffVersion, marker),
                marker),
        };

        if (_state._version == BiffVersion.Unknown)
        {
            _state._version = declared;
            return;
        }

        if (_state._version != declared)
        {
            throw new BiffFormatException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffBofVersionMismatch, marker, _state._version),
                _recordStart);
        }
    }

    /// <summary>
    /// Ensures the current record has the expected type before a typed accessor decodes it.
    /// </summary>
    /// <param name="expected">The record type the accessor decodes.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no record is current or the current record is of a different type.
    /// </exception>
    private readonly void RequireRecord(BiffRecordType expected)
    {
        if (!_hasRecord)
            throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffNoCurrentRecord);

        if (_recordId != (ushort)expected)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Op_Invalid_BiffWrongRecord, RecordType, expected));
        }
    }

    /// <summary>
    /// Ensures the version has been established before a version-dependent accessor decodes a record.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the version is unknown.</exception>
    private readonly void RequireVersion()
    {
        if (_state._version == BiffVersion.Unknown)
            throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffVersionUnknown);
    }
}
