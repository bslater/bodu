// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Provides a forward-only writer that emits BIFF5 or BIFF8 records to an <see cref="IBufferWriter{T}" />. Raw records
/// are written from an identifier and payload; the records the codec names are written from typed values in the layout
/// the selected version requires.
/// </summary>
/// <remarks>
/// <para>
/// The writer is created with an explicit <see cref="BiffVersion" /> and never mixes versions: a record that exists
/// only in BIFF8 (the shared string table, <c>LABELSST</c>) is rejected under BIFF5, and text is encoded as Unicode
/// under BIFF8 and in the configured code page under BIFF5. Every record is checked against the version's maximum
/// payload length; <see cref="WriteContinuedRecord(ushort, ReadOnlySpan{byte})" /> and
/// <see cref="WriteSst(ReadOnlySpan{string})" /> split oversized structures into <c>CONTINUE</c> records according to
/// the format's rules.
/// </para>
/// <para>
/// The writer serializes records, not workbooks: it does not order records, compute the sheet offsets a
/// <c>BOUNDSHEET</c> record carries, or maintain a cell model. <see cref="BytesCommitted" /> reports the stream
/// position so a caller assembling a workbook can compute those offsets. The only sequence rule enforced is that an
/// <c>EOF</c> closes a substream a <c>BOF</c> opened.
/// </para>
/// <para>
/// The writer is a <see langword="ref struct" /> whose mutable counters live in a shared heap object, so a copy taken
/// by value continues the same stream.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var output = new ArrayBufferWriter<byte>();
/// var writer = new BiffWriter(output, BiffVersion.Biff8);
///
/// writer.WriteBof(BiffSubstreamType.Worksheet);
/// writer.WriteNumber(row: 0, column: 0, xfIndex: 15, value: 42.5);
/// writer.WriteLabel(row: 0, column: 1, xfIndex: 15, "inline text");
/// writer.WriteEof();
///
/// // output.WrittenSpan now holds a minimal worksheet substream.
///]]>
/// </code>
/// </example>
/// </remarks>
public ref partial struct BiffWriter
{
    /// <summary>The BIFF5 <c>BOF</c> version marker.</summary>
    private const ushort Biff5VersionMarker = 0x0500;

    /// <summary>The BIFF8 <c>BOF</c> version marker.</summary>
    private const ushort Biff8VersionMarker = 0x0600;

    /// <summary>The destination that receives the records.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The version being emitted.</summary>
    private readonly BiffVersion _version;

    /// <summary>The code page for BIFF5 text.</summary>
    private readonly int _codePage;

    /// <summary>The largest payload a record may carry under the version.</summary>
    private readonly int _maxPayloadLength;

    /// <summary>The shared mutable counters, held on the heap so by-value copies observe the same stream state.</summary>
    private readonly State _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffWriter" /> struct emitting the specified version.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="version">The BIFF version to emit.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="version" /> is not <see cref="BiffVersion.Biff5" /> or
    /// <see cref="BiffVersion.Biff8" />.
    /// </exception>
    public BiffWriter(IBufferWriter<byte> output, BiffVersion version)
        : this(output, new BiffWriterOptions { Version = version })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffWriter" /> struct using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The options selecting the version and the BIFF5 code page.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the options select a version other than <see cref="BiffVersion.Biff5" /> or
    /// <see cref="BiffVersion.Biff8" />.
    /// </exception>
    public BiffWriter(IBufferWriter<byte> output, BiffWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);
        if (options.Version != BiffVersion.Biff5 && options.Version != BiffVersion.Biff8)
            throw new ArgumentOutOfRangeException(nameof(options), options.Version, BiffResourceStrings.Op_Invalid_BiffVersionUnknown);

        _output = output;
        _version = options.Version;
        _codePage = options.CodePage == 0 ? BiffLimits.DefaultCodePage : BiffTextEncoding.Normalize(options.CodePage);
        _maxPayloadLength = BiffLimits.GetMaxPayloadLength(options.Version);
        _state = new State();
    }

    /// <summary>
    /// Gets the BIFF version being emitted.
    /// </summary>
    /// <value>The version.</value>
    public readonly BiffVersion Version => _version;

    /// <summary>
    /// Gets the code page used to encode text under BIFF5.
    /// </summary>
    /// <value>The Windows code page number.</value>
    public readonly int CodePage => _codePage;

    /// <summary>
    /// Gets the largest payload a single record may carry under the version being emitted.
    /// </summary>
    /// <value>The maximum payload length in bytes.</value>
    public readonly int MaxPayloadLength => _maxPayloadLength;

    /// <summary>
    /// Gets the number of bytes written so far: the stream offset at which the next record's header will begin.
    /// </summary>
    /// <value>The byte count, useful for computing the substream offsets a <c>BOUNDSHEET</c> record carries.</value>
    public readonly long BytesCommitted => _state.BytesCommitted;

    /// <summary>
    /// Gets the number of substreams opened by a <c>BOF</c> and not yet closed by an <c>EOF</c>.
    /// </summary>
    /// <value>The open substream depth; zero when the stream is balanced.</value>
    public readonly int OpenSubstreamDepth => _state.OpenSubstreamDepth;

    /// <summary>
    /// Writes a record from its identifier and payload.
    /// </summary>
    /// <param name="recordId">The 16-bit record identifier.</param>
    /// <param name="payload">The record payload.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="payload" /> is longer than <see cref="MaxPayloadLength" />.
    /// </exception>
    public void WriteRecord(ushort recordId, scoped ReadOnlySpan<byte> payload)
    {
        if (payload.Length > _maxPayloadLength)
            throw PayloadTooLong(payload.Length, nameof(payload));

        Span<byte> destination = Begin(recordId, payload.Length);
        payload.CopyTo(destination);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a record of a named type from its payload.
    /// </summary>
    /// <param name="recordType">The record type.</param>
    /// <param name="payload">The record payload.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="payload" /> is longer than <see cref="MaxPayloadLength" />.
    /// </exception>
    public void WriteRecord(BiffRecordType recordType, scoped ReadOnlySpan<byte> payload) =>
        WriteRecord((ushort)recordType, payload);

    /// <summary>
    /// Writes a record whose payload may exceed the maximum record length, splitting the overflow into <c>CONTINUE</c>
    /// records at the maximum length.
    /// </summary>
    /// <param name="recordId">The 16-bit record identifier.</param>
    /// <param name="payload">The logical payload.</param>
    /// <remarks>
    /// The split is byte-oriented: it is correct for structures the format allows to continue at any byte (drawing and
    /// text objects, for example) but not for the shared string table, whose continuation must restart at a character
    /// boundary with a fresh flags byte — use <see cref="WriteSst(ReadOnlySpan{string})" /> for that.
    /// </remarks>
    public void WriteContinuedRecord(ushort recordId, scoped ReadOnlySpan<byte> payload)
    {
        int first = Math.Min(payload.Length, _maxPayloadLength);
        WriteRecord(recordId, payload.Slice(0, first));

        ReadOnlySpan<byte> remaining = payload.Slice(first);
        while (!remaining.IsEmpty)
        {
            int take = Math.Min(remaining.Length, _maxPayloadLength);
            WriteRecord(BiffRecordType.Continue, remaining.Slice(0, take));
            remaining = remaining.Slice(take);
        }
    }

    /// <summary>
    /// Writes a <c>BOF</c> record opening a substream of the specified kind, in the layout of the version being
    /// emitted.
    /// </summary>
    /// <param name="substreamType">The kind of substream.</param>
    /// <param name="build">The build identifier to record.</param>
    /// <param name="year">The build year to record.</param>
    public void WriteBof(BiffSubstreamType substreamType, ushort build = 0, ushort year = 0)
    {
        int length = _version == BiffVersion.Biff8 ? BiffBofRecord.Biff8Length : BiffBofRecord.Biff5Length;
        Span<byte> payload = Begin((ushort)BiffRecordType.Bof, length);
        payload.Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(payload, _version == BiffVersion.Biff8 ? Biff8VersionMarker : Biff5VersionMarker);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)substreamType);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), build);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(6), year);
        if (_version == BiffVersion.Biff8)
            BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(12), 0x00000006);

        Commit(length);
        _state.OpenSubstreamDepth++;
    }

    /// <summary>
    /// Writes an <c>EOF</c> record closing the innermost open substream.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no substream is open.</exception>
    public void WriteEof()
    {
        if (_state.OpenSubstreamDepth == 0)
            throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffEofWithoutBof);

        _ = Begin((ushort)BiffRecordType.Eof, 0);
        Commit(0);
        _state.OpenSubstreamDepth--;
    }

    /// <summary>
    /// Writes a <c>CODEPAGE</c> record.
    /// </summary>
    /// <param name="codePage">The code page value to record, as the format stores it.</param>
    public void WriteCodePage(ushort codePage)
    {
        Span<byte> payload = Begin((ushort)BiffRecordType.CodePage, BiffCodePageRecord.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(payload, codePage);
        Commit(BiffCodePageRecord.Length);
    }

    /// <summary>
    /// Writes a <c>DATEMODE</c> record.
    /// </summary>
    /// <param name="is1904">Whether the workbook uses the 1904 date system.</param>
    public void WriteDateMode(bool is1904)
    {
        Span<byte> payload = Begin((ushort)BiffRecordType.DateMode, BiffDateModeRecord.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)(is1904 ? 1 : 0));
        Commit(BiffDateModeRecord.Length);
    }

    /// <summary>
    /// Reserves space for a record's header and payload in the destination and writes the header.
    /// </summary>
    /// <param name="recordId">The record identifier.</param>
    /// <param name="payloadLength">The payload length.</param>
    /// <returns>The span that receives the payload.</returns>
    private readonly Span<byte> Begin(ushort recordId, int payloadLength)
    {
        Span<byte> span = _output.GetSpan(BiffLimits.RecordHeaderSize + payloadLength);
        new BiffRecordHeader(recordId, (ushort)payloadLength).WriteTo(span);
        return span.Slice(BiffLimits.RecordHeaderSize, payloadLength);
    }

    /// <summary>
    /// Advances the destination past a record begun with <see cref="Begin(ushort, int)" /> and updates the byte count.
    /// </summary>
    /// <param name="payloadLength">The payload length.</param>
    private readonly void Commit(int payloadLength)
    {
        _output.Advance(BiffLimits.RecordHeaderSize + payloadLength);
        _state.BytesCommitted += BiffLimits.RecordHeaderSize + payloadLength;
    }

    /// <summary>
    /// Creates the exception for a payload longer than the version's maximum.
    /// </summary>
    /// <param name="length">The offending length.</param>
    /// <param name="paramName">The name of the parameter that carried the oversized data.</param>
    /// <returns>The exception.</returns>
    private readonly ArgumentOutOfRangeException PayloadTooLong(int length, string paramName) =>
        new(
            paramName,
            length,
            string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Arg_OutOfRange_BiffPayloadTooLong, length, _version, _maxPayloadLength));

    /// <summary>
    /// Ensures a record exists in the version being emitted.
    /// </summary>
    /// <param name="recordType">The record type.</param>
    /// <exception cref="InvalidOperationException">Thrown under BIFF5 for a BIFF8-only record.</exception>
    private readonly void RequireBiff8(BiffRecordType recordType)
    {
        if (_version != BiffVersion.Biff8)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Op_Invalid_BiffRecordNotInVersion, recordType, _version));
        }
    }

    /// <summary>
    /// Validates that a row or column index fits the 16-bit field the cell records use.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is negative or above 65535.</exception>
    private static void RequireCellIndex(int index, string paramName)
    {
        if ((uint)index > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                index,
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Arg_OutOfRange_BiffCellIndex, index));
        }
    }

    /// <summary>
    /// Writes the row, column, and format prefix shared by the cell records.
    /// </summary>
    /// <param name="payload">The payload buffer.</param>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="xfIndex">The XF index.</param>
    private static void WriteCellPrefix(Span<byte> payload, int row, int column, ushort xfIndex)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)column);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), xfIndex);
    }

    /// <summary>
    /// Holds the counters shared by every by-value copy of the writer.
    /// </summary>
    private sealed class State
    {
        /// <summary>
        /// Gets or sets the number of bytes written.
        /// </summary>
        public long BytesCommitted { get; set; }

        /// <summary>
        /// Gets or sets the number of open substreams.
        /// </summary>
        public int OpenSubstreamDepth { get; set; }
    }
}
