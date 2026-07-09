// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Carries the resumable state of a <see cref="Utf8TomlReader" /> between input blocks: an opaque snapshot that,
/// together with the unconsumed bytes, lets a new reader continue exactly where the previous block's reader stopped.
/// </summary>
/// <remarks>
/// <para>
/// The multi-block protocol is as follows: construct a reader over the available bytes with <c>isFinalBlock: false</c>;
/// when <see cref="Utf8TomlReader.Read" /> returns <see langword="false" />, capture
/// <see cref="Utf8TomlReader.CurrentState" />, carry the bytes from <see cref="Utf8TomlReader.BytesConsumed" /> onward
/// into the next buffer together with the newly arrived data, and construct the next reader from that buffer and this
/// state. A token never spans reader instances — the reader consumes input only in whole tokens — so the caller's
/// buffer must eventually contain the largest single token.
/// </para>
/// <para>
/// The state preserves the grammar context (the scan state and the open-container stack), the line counter, and the
/// byte position within the current line, so token positions and error positions remain line- and column-accurate
/// across blocks. Byte offsets (<see cref="Utf8TomlReader.TokenStartIndex" /> and
/// <see cref="TomlFormatException.Offset" />) are relative to the current block's buffer.
/// </para>
/// </remarks>
public struct TomlReaderState
{
    /// <summary>The reader options the state was created with.</summary>
    private readonly TomlReaderOptions _options;

    /// <summary>The lexical context the next read resumes from.</summary>
    internal TomlScanState _scanState;

    /// <summary>The container context stack: one entry per open array or inline table.</summary>
    internal byte[]? _containers;

    /// <summary>The number of open containers on <see cref="_containers" />.</summary>
    internal int _containerCount;

    /// <summary>Whether the cursor inside an inline table sits immediately after a value separator comma.</summary>
    internal bool _inlineAfterComma;

    /// <summary>Whether the header being lexed is an <c>[[array-of-tables]]</c> header.</summary>
    internal bool _headerIsArray;

    /// <summary>The number of source lines completed before the resume point, so that the zero of a default-initialized state means line one.</summary>
    internal int _linesRead;

    /// <summary>The number of bytes of the current line already consumed in earlier blocks, used to keep columns accurate.</summary>
    internal int _bytesInLine;

    /// <summary>Whether the reader has moved past the document start, where a byte-order mark may be skipped. Stored inverted so that a default-initialized state describes the document start.</summary>
    internal bool _pastStart;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReaderState" /> struct describing the start of a document.
    /// </summary>
    /// <param name="options">
    /// The reader options controlling the specification version and maximum bracket nesting depth.
    /// </param>
    /// <remarks>
    /// A default-initialized <see cref="TomlReaderState" /> equally describes the start of a document with default
    /// options.
    /// </remarks>
    public TomlReaderState(TomlReaderOptions options = default)
    {
        _options = options;
        _scanState = TomlScanState.Expression;
        _containers = null;
        _containerCount = 0;
        _inlineAfterComma = false;
        _headerIsArray = false;
        _linesRead = 0;
        _bytesInLine = 0;
        _pastStart = false;
    }

    /// <summary>
    /// Gets the reader options the state carries.
    /// </summary>
    /// <value>The options supplied when the state was created.</value>
    public readonly TomlReaderOptions Options => _options;
}
