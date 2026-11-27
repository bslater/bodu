// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Carries the resumable state of a <see cref="Utf8IniReader" /> between input blocks: an opaque snapshot that,
/// together with the unconsumed bytes, lets a new reader continue exactly where the previous block's reader stopped.
/// </summary>
/// <remarks>
/// <para>
/// The multi-block protocol is as follows: construct a reader over the available bytes with <c>isFinalBlock: false</c>;
/// when <see cref="Utf8IniReader.Read" /> returns <see langword="false" />, capture
/// <see cref="Utf8IniReader.CurrentState" />, carry the bytes from <see cref="Utf8IniReader.BytesConsumed" /> onward
/// into the next buffer together with the newly arrived data, and construct the next reader from that buffer and this
/// state, until a reader built with <c>isFinalBlock: true</c> returns <see langword="false" /> at the end of the input.
/// The reader reads a line whole before it reports its tokens, so a line never spans reader instances, and the buffer
/// must eventually hold the longest single line.
/// </para>
/// <para>
/// The state preserves the line counter and the offset of the resume point in the whole input, so that
/// <see cref="Utf8IniReader.LineNumber" /> and the positions of an <see cref="IniFormatException" /> count from the
/// start of the whole input across blocks, while <see cref="Utf8IniReader.BytesConsumed" /> stays relative to the
/// current block. A state captured between a key and its value also carries the value, so the next reader reports it
/// first.
/// </para>
/// </remarks>
public struct IniReaderState
{
    /// <summary>The reader options the state was created with.</summary>
    private readonly IniReaderOptions _options;

    /// <summary>The number of lines completed before the resume point, so that the zero of a default-initialized state means line one.</summary>
    internal int _linesRead;

    /// <summary>The offset, in the whole input, of the resume point: the bytes the readers before it consumed.</summary>
    internal long _bytesConsumed;

    /// <summary>Whether the reader has moved past the start of the input, where a byte-order mark may be skipped. Stored inverted so that a default-initialized state describes the start of the input.</summary>
    internal bool _pastStart;

    /// <summary>Whether the next read reports the string value of a key already reported.</summary>
    internal bool _pendingString;

    /// <summary>The text of the pending string value.</summary>
    internal string? _pendingValue;

    /// <summary>The 1-based line number on which the pending string value begins.</summary>
    internal int _pendingLine;

    /// <summary>The offset, in the whole input, at which the pending string value begins.</summary>
    internal long _pendingOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniReaderState" /> struct describing the start of an input.
    /// </summary>
    /// <param name="options">The reader options that the readers built from the state apply.</param>
    /// <remarks>
    /// A default-initialized <see cref="IniReaderState" /> equally describes the start of an input read with
    /// <see cref="IniReaderOptions.Default" />.
    /// </remarks>
    public IniReaderState(IniReaderOptions options = default)
    {
        _options = options;
        _linesRead = 0;
        _bytesConsumed = 0;
        _pastStart = false;
        _pendingString = false;
        _pendingValue = null;
        _pendingLine = 0;
        _pendingOffset = 0;
    }

    /// <summary>
    /// Gets the reader options the state carries.
    /// </summary>
    /// <value>The options supplied when the state was created.</value>
    public readonly IniReaderOptions Options => _options;
}
