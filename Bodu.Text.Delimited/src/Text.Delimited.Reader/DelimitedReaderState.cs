// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Carries the resumable state of a <see cref="Utf8DelimitedReader" /> between input blocks: an opaque snapshot that,
/// together with the unconsumed bytes, lets a new reader continue exactly where the previous block's reader stopped.
/// </summary>
/// <remarks>
/// <para>
/// The multi-block protocol is as follows: construct a reader over the available bytes with <c>isFinalBlock: false</c>;
/// when <see cref="Utf8DelimitedReader.Read" /> returns <see langword="false" />, capture
/// <see cref="Utf8DelimitedReader.CurrentState" />, carry the bytes from
/// <see cref="Utf8DelimitedReader.BytesConsumed" /> onward into the next buffer together with the newly arrived data,
/// and construct the next reader from that buffer and this state. The reader reads a record whole before it reports the
/// record's tokens, so a record never spans reader instances, and the buffer must eventually hold the largest single
/// record.
/// </para>
/// <para>
/// The state preserves the header names, the line counter and the offset of the resume point in the whole input, so
/// that <see cref="Utf8DelimitedReader.LineNumber" /> and the positions of a <see cref="DelimitedFormatException" />
/// count from the start of the whole input across blocks; <see cref="Utf8DelimitedReader.BytesConsumed" /> and
/// <see cref="Utf8DelimitedReader.ValueSpan" /> stay relative to the current block. A state captured while the tokens
/// of a record are still being reported also carries the fields left to report, with a copy of their source bytes for
/// <see cref="Utf8DelimitedReader.ValueSpan" />, so the next reader reports them before it reads on.
/// </para>
/// </remarks>
public struct DelimitedReaderState
{
    /// <summary>The reader options the state was created with.</summary>
    private readonly DelimitedReaderOptions _options;

    /// <summary>The number of lines completed before the resume point, so that the zero of a default-initialized state means line one.</summary>
    internal int _linesRead;

    /// <summary>The offset, in the whole input, of the resume point: the bytes the readers before it consumed.</summary>
    internal long _bytesConsumed;

    /// <summary>Whether the reader has moved past the start of the input, where a byte-order mark may be skipped. Stored inverted so that a default-initialized state describes the start of the input.</summary>
    internal bool _pastStart;

    /// <summary>The reader lifecycle stage the next read resumes in.</summary>
    internal Utf8DelimitedReader.Stage _stage;

    /// <summary>The header names, once the header row has been read; otherwise <see langword="null" />.</summary>
    internal List<string>? _headers;

    /// <summary>The 1-based line number on which the last record read starts.</summary>
    internal int _recordLine;

    /// <summary>The offset, in the whole input, at which the last record read starts.</summary>
    internal long _recordOffset;

    /// <summary>The decoded fields of a record whose tokens were partly reported when the state was captured; otherwise <see langword="null" />.</summary>
    internal string[]? _pendingFields;

    /// <summary>The raw source ranges of <see cref="_pendingFields" />, indexing <see cref="_pendingSource" />.</summary>
    internal (int Start, int Length)[]? _pendingRanges;

    /// <summary>The copied source bytes of the record whose fields <see cref="_pendingFields" /> holds.</summary>
    internal byte[]? _pendingSource;

    /// <summary>The index of the next field of the pending record to report.</summary>
    internal int _fieldIndex;

    /// <summary>Whether the next token of a pending header-mode record is a value rather than a property name.</summary>
    internal bool _emittingValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedReaderState" /> struct describing the start of an input.
    /// </summary>
    /// <param name="options">The reader options that the readers built from the state apply.</param>
    /// <remarks>
    /// A default-initialized <see cref="DelimitedReaderState" /> equally describes the start of an input read with
    /// <see cref="DelimitedReaderOptions.Default" />. The options are validated when a reader is constructed from the
    /// state.
    /// </remarks>
    public DelimitedReaderState(DelimitedReaderOptions options = default)
    {
        _options = options;
        _linesRead = 0;
        _bytesConsumed = 0;
        _pastStart = false;
        _stage = Utf8DelimitedReader.Stage.Start;
        _headers = null;
        _recordLine = 0;
        _recordOffset = 0;
        _pendingFields = null;
        _pendingRanges = null;
        _pendingSource = null;
        _fieldIndex = 0;
        _emittingValue = false;
    }

    /// <summary>
    /// Gets the reader options the state carries.
    /// </summary>
    /// <value>The options supplied when the state was created.</value>
    public readonly DelimitedReaderOptions Options => _options;
}
