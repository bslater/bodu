// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Carries the resumable state of a <see cref="Utf8DotEnvReader" /> between input blocks: an opaque snapshot that,
/// together with the unconsumed bytes, lets a new reader continue exactly where the previous block's reader stopped.
/// </summary>
/// <remarks>
/// <para>
/// The multi-block protocol is as follows: construct a reader over the available bytes with <c>isFinalBlock: false</c>;
/// when <see cref="Utf8DotEnvReader.Read" /> returns <see langword="false" />, capture
/// <see cref="Utf8DotEnvReader.CurrentState" />, carry the bytes from <see cref="Utf8DotEnvReader.BytesConsumed" />
/// onward into the next buffer together with the newly arrived data, and construct the next reader from that buffer and
/// this state. The reader reads an entry or a comment line whole before it reports its tokens, so neither spans reader
/// instances, and the buffer must eventually hold the largest single entry, a double-quoted value that runs across
/// lines included.
/// </para>
/// <para>
/// The state preserves the line counter, the byte position within the current line and the offset of the resume point
/// in the whole input, so that <see cref="Utf8DotEnvReader.LineNumber" /> and the positions of a
/// <see cref="DotEnvFormatException" /> count from the start of the whole input across blocks;
/// <see cref="Utf8DotEnvReader.BytesConsumed" /> and <see cref="Utf8DotEnvReader.ValueSpan" /> stay relative to the
/// current block. A state captured between a property name and its value also carries the value, with a copy of its
/// source bytes for <see cref="Utf8DotEnvReader.ValueSpan" />, so the next reader reports it first.
/// </para>
/// </remarks>
public struct DotEnvReaderState
{
    /// <summary>The reader options the state was created with.</summary>
    private readonly DotEnvReaderOptions _options;

    /// <summary>The number of lines completed before the resume point, so that the zero of a default-initialized state means line one.</summary>
    internal int _linesRead;

    /// <summary>The number of bytes of the current line consumed before the resume point, a byte-order mark excluded, used to keep columns accurate.</summary>
    internal int _bytesInLine;

    /// <summary>The offset, in the whole input, of the resume point: the bytes the readers before it consumed.</summary>
    internal long _bytesConsumed;

    /// <summary>Whether the reader has moved past the start of the input, where a byte-order mark may be skipped. Stored inverted so that a default-initialized state describes the start of the input.</summary>
    internal bool _pastStart;

    /// <summary>The reader lifecycle phase the next read resumes in.</summary>
    internal Utf8DotEnvReader.Phase _phase;

    /// <summary>Whether the next read reports the string value of a property name already reported.</summary>
    internal bool _pendingString;

    /// <summary>The copied raw source bytes of the pending string value.</summary>
    internal byte[]? _pendingRaw;

    /// <summary>The decoded pending string value when escape processing applied; otherwise <see langword="null" />.</summary>
    internal string? _pendingDecoded;

    /// <summary>The 1-based line number on which the pending string value begins.</summary>
    internal int _pendingLine;

    /// <summary>Whether the entry of the pending string value carried an <c>export</c> prefix.</summary>
    internal bool _pendingExport;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvReaderState" /> struct describing the start of an input.
    /// </summary>
    /// <param name="options">The reader options that the readers built from the state apply.</param>
    /// <remarks>
    /// A default-initialized <see cref="DotEnvReaderState" /> equally describes the start of an input read with
    /// <see cref="DotEnvReaderOptions.Default" />.
    /// </remarks>
    public DotEnvReaderState(DotEnvReaderOptions options = default)
    {
        _options = options;
        _linesRead = 0;
        _bytesInLine = 0;
        _bytesConsumed = 0;
        _pastStart = false;
        _phase = Utf8DotEnvReader.Phase.Start;
        _pendingString = false;
        _pendingRaw = null;
        _pendingDecoded = null;
        _pendingLine = 0;
        _pendingExport = false;
    }

    /// <summary>
    /// Gets the reader options the state carries.
    /// </summary>
    /// <value>The options supplied when the state was created.</value>
    public readonly DotEnvReaderOptions Options => _options;
}
