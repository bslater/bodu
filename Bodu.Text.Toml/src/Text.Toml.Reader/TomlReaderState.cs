// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Carries the resumable state of a <see cref="Utf8TomlReader" /> between input blocks, mirroring the role of
/// <see cref="System.Text.Json.JsonReaderState" />: an opaque snapshot that, together with the unconsumed bytes,
/// lets a new reader continue exactly where the previous block's reader stopped.
/// </summary>
/// <remarks>
/// <para>
/// The multi-block protocol matches <see cref="System.Text.Json.Utf8JsonReader" />: construct a reader over the
/// available bytes with <c>isFinalBlock: false</c>; when <see cref="Utf8TomlReader.Read" /> returns
/// <see langword="false" />, capture <see cref="Utf8TomlReader.CurrentState" />, carry the bytes from
/// <see cref="Utf8TomlReader.BytesConsumed" /> onward into the next buffer together with the newly arrived data, and
/// construct the next reader from that buffer and this state. A token never spans reader instances — the reader
/// consumes input only in whole tokens — so the caller's buffer must eventually contain the largest single token.
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
    /// <summary>
    /// The reader options the state was created with.
    /// </summary>
    private readonly TomlReaderOptions _options;

    /// <summary>
    /// The lexical context the next read resumes from.
    /// </summary>
    internal TomlScanState ScanState;

    /// <summary>
    /// The container context stack: one entry per open array or inline table.
    /// </summary>
    internal byte[]? Containers;

    /// <summary>
    /// The number of open containers on <see cref="Containers" />.
    /// </summary>
    internal int ContainerCount;

    /// <summary>
    /// Whether the cursor inside an inline table sits immediately after a value separator comma.
    /// </summary>
    internal bool InlineAfterComma;

    /// <summary>
    /// Whether the header being lexed is an <c>[[array-of-tables]]</c> header.
    /// </summary>
    internal bool HeaderIsArray;

    /// <summary>
    /// The number of source lines completed before the resume point, so that the zero of a default-initialized state
    /// means line one.
    /// </summary>
    internal int LinesRead;

    /// <summary>
    /// The number of bytes of the current line already consumed in earlier blocks, used to keep columns accurate.
    /// </summary>
    internal int BytesInLine;

    /// <summary>
    /// Whether the reader has moved past the document start, where a byte-order mark may be skipped. Stored inverted
    /// so that a default-initialized state describes the document start.
    /// </summary>
    internal bool PastStart;

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
        ScanState = TomlScanState.Expression;
        Containers = null;
        ContainerCount = 0;
        InlineAfterComma = false;
        HeaderIsArray = false;
        LinesRead = 0;
        BytesInLine = 0;
        PastStart = false;
    }

    /// <summary>
    /// Gets the reader options the state carries.
    /// </summary>
    /// <returns>The options supplied when the state was created.</returns>
    public readonly TomlReaderOptions Options => _options;
}
