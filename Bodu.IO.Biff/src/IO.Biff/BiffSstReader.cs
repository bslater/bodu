// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Reads the strings of a BIFF8 shared string table (<c>SST</c>) one at a time, pulling the <c>CONTINUE</c> records
/// that carry the table's overflow from the parent <see cref="BiffReader" /> as they are needed.
/// </summary>
/// <remarks>
/// <para>
/// The reader is created while the parent is positioned on the <c>SST</c> record; each call to
/// <see cref="Read(ref BiffReader)" /> is passed the parent by reference and advances it past every <c>CONTINUE</c>
/// record it consumes, so the parent's <see cref="BiffReader.BytesConsumed" /> stays truthful and the next
/// <see cref="BiffReader.Read" /> resumes after the table. A <see langword="ref struct" /> cannot hold a reference to
/// another, which is why the parent is supplied per call rather than captured. It follows the BIFF continuation rules
/// for strings: a string's header never straddles a boundary, character data may, and a continued run restarts with its
/// own option-flags byte selecting 8-bit or 16-bit characters.
/// </para>
/// <para>
/// A string whose characters lie within one record is exposed as a span-backed <see cref="Current" /> view. A string
/// whose characters straddle a boundary (<see cref="IsFragmented" />) has no single span; it is decoded into a reusable
/// scratch buffer during <see cref="Read(ref BiffReader)" />, and <see cref="GetString" /> and <see cref="CopyTo" />
/// serve both kinds uniformly. Formatting-run and extended-data trailers that straddle a boundary are skipped and
/// reported as empty.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// while (reader.Read())
/// {
///     if (reader.RecordType != BiffRecordType.Sst)
///         continue;
///
///     var strings = new BiffSstReader(ref reader);
///     while (strings.Read(ref reader))
///         Console.WriteLine($"[{strings.Index}] {strings.GetString()}");
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public ref struct BiffSstReader
{
    /// <summary>The initial scratch capacity for fragmented strings.</summary>
    private const int InitialScratchLength = 256;

    /// <summary>The counts from the table header.</summary>
    private readonly BiffSstHeader _header;

    /// <summary>The payload of the record the cursor is in: the <c>SST</c> record or a <c>CONTINUE</c>.</summary>
    private ReadOnlySpan<byte> _block;

    /// <summary>The cursor offset within <see cref="_block" />.</summary>
    private int _offset;

    /// <summary>The number of strings read so far.</summary>
    private uint _read;

    /// <summary>Whether a string is current.</summary>
    private bool _hasCurrent;

    /// <summary>The contiguous view of the current string, when it has one.</summary>
    private BiffString _current;

    /// <summary>Whether the current string's characters straddled a continuation boundary.</summary>
    private bool _fragmented;

    /// <summary>The declared character count of the current string.</summary>
    private int _length;

    /// <summary>The rich-run count of the current string.</summary>
    private int _richRunCount;

    /// <summary>Whether the current string declared extended data.</summary>
    private bool _hasExtendedData;

    /// <summary>The scratch buffer holding a fragmented string's decoded characters.</summary>
    private char[]? _scratch;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffSstReader" /> struct over the <c>SST</c> record the parent
    /// reader is positioned on.
    /// </summary>
    /// <param name="reader">The parent reader, positioned on an <c>SST</c> record.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="reader" /> is not positioned on an <c>SST</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the record is too short to hold the table header.</exception>
    public BiffSstReader(scoped ref BiffReader reader)
    {
        if (!reader.HasRecord || reader.RecordType != BiffRecordType.Sst)
            throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffSstNotAtRecord);

        _header = BiffSstHeader.Read(reader.ValueSpan);
        _block = reader.ValueSpan;
        _offset = BiffSstHeader.Length;
        _read = 0;
        _hasCurrent = false;
        _current = default;
        _fragmented = false;
        _length = 0;
        _richRunCount = 0;
        _hasExtendedData = false;
        _scratch = null;
    }

    /// <summary>
    /// Gets the counts from the table header.
    /// </summary>
    /// <value>The total reference count and the unique string count.</value>
    public readonly BiffSstHeader Header => _header;

    /// <summary>
    /// Gets the zero-based index of the current string within the table.
    /// </summary>
    /// <value>The index, or −1 before the first string has been read.</value>
    public readonly int Index => (int)_read - 1;

    /// <summary>
    /// Gets a value indicating whether a string is current.
    /// </summary>
    /// <value>
    /// <see langword="true" /> after a successful <see cref="Read(ref BiffReader)" /> until the table is exhausted.
    /// </value>
    public readonly bool HasCurrent => _hasCurrent;

    /// <summary>
    /// Gets the declared character count of the current string.
    /// </summary>
    /// <value>The UTF-16 code-unit count.</value>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    public readonly int Length
    {
        get
        {
            RequireCurrent();

            return _length;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current string's characters straddled a continuation boundary and therefore
    /// have no contiguous view.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="Current" /> is unavailable and <see cref="GetString" /> must be used.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    public readonly bool IsFragmented
    {
        get
        {
            RequireCurrent();

            return _fragmented;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current string carries rich-text formatting runs.
    /// </summary>
    /// <value><see langword="true" /> when the string header declared runs.</value>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    public readonly bool HasRichRuns
    {
        get
        {
            RequireCurrent();

            return _richRunCount > 0;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current string carries extended (phonetic) data.
    /// </summary>
    /// <value><see langword="true" /> when the string header declared extended data.</value>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    public readonly bool HasExtendedData
    {
        get
        {
            RequireCurrent();

            return _hasExtendedData;
        }
    }

    /// <summary>
    /// Gets the span-backed view of the current string.
    /// </summary>
    /// <value>The view over the string's characters and trailers within their record.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no string is current, or the current string is fragmented.
    /// </exception>
    public readonly BiffString Current
    {
        get
        {
            RequireCurrent();
            if (_fragmented)
                throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffSstFragmented);

            return _current;
        }
    }

    /// <summary>
    /// Advances to the next string in the table, consuming continuation records from the parent reader as needed.
    /// </summary>
    /// <param name="reader">The parent reader the table was created from.</param>
    /// <returns>
    /// <see langword="true" /> when a string was read; <see langword="false" /> once every declared string has been
    /// read.
    /// </returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the table ends before the declared strings do, a string header or its characters are truncated, or a
    /// continuation record is empty where character data was expected.
    /// </exception>
    public bool Read(scoped ref BiffReader reader)
    {
        if (_read >= _header.UniqueCount)
        {
            _hasCurrent = false;
            _current = default;
            return false;
        }

        AdvancePastBlockEnd(ref reader);

        int headerStart = _offset;
        int length = ReadUInt16();
        byte flags = ReadByte();
        bool highByte = (flags & 0x01) != 0;
        bool hasExtended = (flags & 0x04) != 0;
        bool hasRich = (flags & 0x08) != 0;

        int runCount = hasRich ? ReadUInt16() : 0;
        int extendedLength = 0;
        if (hasExtended)
        {
            uint declared = ReadUInt32();
            if (declared > int.MaxValue)
                throw Malformed();

            extendedLength = (int)declared;
        }

        _length = length;
        _richRunCount = runCount;
        _hasExtendedData = hasExtended;

        int byteCount = highByte ? length * 2 : length;
        if (_offset + byteCount <= _block.Length)
        {
            ReadOnlySpan<byte> characters = _block.Slice(_offset, byteCount);
            _offset += byteCount;
            int trailerLength = (runCount * 4) + extendedLength;

            ReadOnlySpan<byte> runs = default;
            ReadOnlySpan<byte> extended = default;
            if (_offset + trailerLength <= _block.Length)
            {
                runs = _block.Slice(_offset, runCount * 4);
                extended = _block.Slice(_offset + (runCount * 4), extendedLength);
                _offset += trailerLength;
            }
            else
            {
                SkipBytes(ref reader, trailerLength);
            }

            _fragmented = false;
            _current = BiffString.FromUnicodeCharacters(characters, length, highByte, runs, runCount, extended, hasExtended, _offset - headerStart);
        }
        else
        {
            ReadFragmentedCharacters(ref reader, length, highByte);
            SkipBytes(ref reader, (runCount * 4) + extendedLength);
            _fragmented = true;
            _current = default;
        }

        _read++;
        _hasCurrent = true;
        return true;
    }

    /// <summary>
    /// Decodes the current string into a new <see cref="string" />, whether or not it is fragmented.
    /// </summary>
    /// <returns>The decoded text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    public readonly string GetString()
    {
        RequireCurrent();

        if (!_fragmented)
            return _current.GetString();

        return _length == 0 ? string.Empty : new string(_scratch!, 0, _length);
    }

    /// <summary>
    /// Decodes the current string into the supplied destination, whether or not it is fragmented.
    /// </summary>
    /// <param name="destination">The buffer that receives the decoded characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than <see cref="Length" />.
    /// </exception>
    public readonly int CopyTo(Span<char> destination)
    {
        RequireCurrent();

        if (!_fragmented)
            return _current.CopyTo(destination);

        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, _length);
        _scratch.AsSpan(0, _length).CopyTo(destination);
        return _length;
    }

    /// <summary>
    /// Creates the malformed-table exception.
    /// </summary>
    /// <returns>A <see cref="BiffFormatException" /> describing the failure.</returns>
    private static BiffFormatException Malformed() =>
        new(BiffResourceStrings.Format_Invalid_BiffSharedStringTable);

    /// <summary>
    /// Ensures a string is current.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no string is current.</exception>
    private readonly void RequireCurrent()
    {
        if (!_hasCurrent)
            throw new InvalidOperationException(BiffResourceStrings.Op_Invalid_BiffSstNoCurrent);
    }

    /// <summary>
    /// Moves the cursor into the next continuation record when it has reached the end of the current block.
    /// </summary>
    /// <param name="reader">The parent reader.</param>
    /// <exception cref="BiffFormatException">Thrown when no continuation record follows.</exception>
    private void AdvancePastBlockEnd(scoped ref BiffReader reader)
    {
        while (_offset >= _block.Length)
            NextBlock(ref reader);
    }

    /// <summary>
    /// Consumes the next continuation record from the parent reader and makes it the current block.
    /// </summary>
    /// <param name="reader">The parent reader.</param>
    /// <exception cref="BiffFormatException">Thrown when no continuation record follows.</exception>
    private void NextBlock(scoped ref BiffReader reader)
    {
        if (!reader.TryReadContinuation(out ReadOnlySpan<byte> next))
            throw Malformed();

        _block = next;
        _offset = 0;
    }

    /// <summary>
    /// Reads a byte at the cursor.
    /// </summary>
    /// <returns>The byte.</returns>
    /// <exception cref="BiffFormatException">Thrown when the block ends first.</exception>
    private byte ReadByte()
    {
        if (_offset >= _block.Length)
            throw Malformed();

        return _block[_offset++];
    }

    /// <summary>
    /// Reads a little-endian 16-bit integer at the cursor.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="BiffFormatException">Thrown when the block ends first.</exception>
    private ushort ReadUInt16()
    {
        if (_offset + 2 > _block.Length)
            throw Malformed();

        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_block.Slice(_offset));
        _offset += 2;
        return value;
    }

    /// <summary>
    /// Reads a little-endian 32-bit integer at the cursor.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="BiffFormatException">Thrown when the block ends first.</exception>
    private uint ReadUInt32()
    {
        if (_offset + 4 > _block.Length)
            throw Malformed();

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_block.Slice(_offset));
        _offset += 4;
        return value;
    }

    /// <summary>
    /// Decodes characters that straddle one or more continuation boundaries into the scratch buffer, re-reading the
    /// option-flags byte at the start of each continued segment.
    /// </summary>
    /// <param name="reader">The parent reader.</param>
    /// <param name="length">The number of characters to decode.</param>
    /// <param name="highByte">Whether the first segment uses 16-bit characters.</param>
    /// <exception cref="BiffFormatException">
    /// Thrown when the data runs out, or a continued segment is empty.
    /// </exception>
    private void ReadFragmentedCharacters(scoped ref BiffReader reader, int length, bool highByte)
    {
        if (_scratch is null || _scratch.Length < length)
            _scratch = new char[Math.Max(length, InitialScratchLength)];

        Span<char> output = _scratch.AsSpan(0, length);
        int written = 0;
        bool high = highByte;

        while (written < length)
        {
            if (_offset >= _block.Length)
            {
                // A continued run restarts with its own flags byte; an empty continuation cannot carry one.
                NextBlock(ref reader);
                if (_block.IsEmpty)
                    throw Malformed();

                high = (_block[0] & 0x01) != 0;
                _offset = 1;
            }

            int remaining = length - written;
            if (high)
            {
                int available = (_block.Length - _offset) / 2;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw Malformed();

                Encoding.Unicode.GetChars(_block.Slice(_offset, take * 2), output.Slice(written, take));
                _offset += take * 2;
                written += take;
            }
            else
            {
                int available = _block.Length - _offset;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw Malformed();

                for (int i = 0; i < take; i++)
                    output[written + i] = (char)_block[_offset + i];

                _offset += take;
                written += take;
            }
        }
    }

    /// <summary>
    /// Skips trailer bytes (formatting runs and extended data), crossing continuation boundaries without re-reading a
    /// flags byte.
    /// </summary>
    /// <param name="reader">The parent reader.</param>
    /// <param name="count">The number of bytes to skip.</param>
    /// <exception cref="BiffFormatException">Thrown when the data runs out.</exception>
    private void SkipBytes(scoped ref BiffReader reader, int count)
    {
        int remaining = count;
        while (remaining > 0)
        {
            if (_offset >= _block.Length)
            {
                NextBlock(ref reader);
                continue;
            }

            int take = Math.Min(_block.Length - _offset, remaining);
            _offset += take;
            remaining -= take;
        }
    }
}
