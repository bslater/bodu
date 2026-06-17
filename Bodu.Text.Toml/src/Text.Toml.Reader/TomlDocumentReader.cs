// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Provides a forward-only cursor over the normalized, tree-order token stream of a parsed TOML document, serving as
/// the binding layer through which converters consume values. The reader is a <see langword="ref struct" />, so it
/// cannot be boxed or captured; pass it by <see langword="ref" /> to thread it through a converter.
/// </summary>
/// <remarks>
/// <para>
/// TOML cannot be tokenized into tree order in a single forward pass: out-of-line <c>[table]</c> and
/// <c>[[array-of-tables]]</c> headers contribute to structure declared elsewhere in the document. The constructor
/// therefore parses the entire document up front — scanning the UTF-8 bytes with <see cref="Utf8TomlReader" /> and
/// enforcing TOML's key, value, table, and array-of-tables rules through <see cref="TomlDocumentBuilder" /> — into a
/// flat row store, and <see cref="Read" /> advances a depth-first cursor over that store, emitting the normalized token
/// stream on demand rather than materializing it. This type walks a parsed document; <see cref="Utf8TomlReader" />
/// reads the UTF-8 source in document order.
/// </para>
/// <para>
/// The stream is normalized: the several TOML spellings of structure collapse to a single nested shape. A header table,
/// a dotted key, and an inline <c>{ … }</c> table all surface as <see cref="TomlTokenType.PropertyName" /> followed by
/// <see cref="TomlTokenType.StartTable" /> … <see cref="TomlTokenType.EndTable" />, with out-of-line headers merged
/// into the correct nested table. An array-of-tables surfaces as <see cref="TomlTokenType.StartArray" /> whose elements
/// are each a <see cref="TomlTokenType.StartTable" />. Scalars are decoded once during parsing and exposed through the
/// typed accessors.
/// </para>
/// <para>
/// Because parsing happens in the constructor, a malformed document raises <see cref="TomlFormatException" /> from the
/// constructor rather than from <see cref="Read" />.
/// </para>
/// <para>
/// Date-time values map onto the CLR date and time types, which imposes three deliberate deviations from the RFC 3339
/// grammar that TOML incorporates by reference: a leap second (<c>23:59:60</c>) is rejected because
/// <see cref="DateTime" /> and <see cref="TimeOnly" /> cannot represent second 60; year <c>0000</c> is rejected because
/// the CLR calendar begins at year 1; and offsets beyond ±14:00 are rejected by <see cref="DateTimeOffset" />. Each
/// surfaces as a <see cref="TomlFormatException" />.
/// </para>
/// </remarks>
public ref struct TomlDocumentReader
{
    /// <summary>The initial capacity of the traversal stack; it grows on demand for documents nested deeper than this.</summary>
    private const int InitialStackDepth = 8;

    /// <summary>The flat row store the cursor walks, with the document root at index 0.</summary>
    private readonly List<TomlReaderRow> _rows;

    /// <summary>The UTF-8 source bytes, retained so a string value can be decoded on demand and a token's byte offset mapped to a line and column on a binding failure. Held as a span because the reader is a <see langword="ref struct" /> scoped to a single read.</summary>
    private readonly ReadOnlySpan<byte> _source;

    /// <summary>A lazily created garbage-collected copy of <see cref="_source" />, materialized on the first <see cref="GetOwnedSource" /> so that subtree documents from <c>TomlDocument.ParseValue</c> can retain the source the reader holds only as a span. Every such document from one read shares this single copy.</summary>
    private byte[]? _ownedSource;

    /// <summary>The stack of open containers, one frame per open table or array, innermost last.</summary>
    private Frame[] _stack;

    /// <summary>The number of open containers on <see cref="_stack" />, counting the document-root table. The publicly reported <see cref="CurrentDepth" /> excludes the root, so it is this value less one.</summary>
    private int _depth;

    /// <summary>Whether the first <see cref="Read" /> has occurred, after which the cursor is positioned within the store.</summary>
    private bool _started;

    /// <summary>The kind of the current token.</summary>
    private TomlTokenType _tokenType;

    /// <summary>The string carried by the current token: the key for a property name, or the decoded string for a string scalar. It is <see langword="null" /> for a value-type scalar — whose value is decoded on demand from the current row — and for a structural token.</summary>
    private string? _value;

    /// <summary>The zero-based source byte offset at which the current token begins.</summary>
    private int _offset;

    /// <summary>The row index of the value the current token belongs to: the value row for a property name, scalar, or container start; the container row for a container end.</summary>
    private int _currentRow;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentReader" /> struct over the supplied bytes, enforcing
    /// strict TOML v1.0.0.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    public TomlDocumentReader(ReadOnlySpan<byte> utf8Toml)
        : this(utf8Toml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentReader" /> struct over the supplied bytes using the
    /// supplied options.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <param name="options">
    /// The reader options controlling the specification version and maximum nesting depth.
    /// </param>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    /// <remarks>
    /// A <see cref="TomlReaderOptions.MaxDepth" /> of zero or less selects the default maximum depth of 64, and a
    /// larger value is clamped to <see cref="TomlLimits.AbsoluteMaxDepth" /> so that an unbounded configured value
    /// cannot drive the parser into a <see cref="StackOverflowException" />; a document nested deeper than the
    /// effective limit throws <see cref="TomlFormatException" />.
    /// </remarks>
    public TomlDocumentReader(ReadOnlySpan<byte> utf8Toml, TomlReaderOptions options)
    {
        int maxDepth = options.MaxDepth <= 0 ? TomlLimits.AbsoluteMaxDepth : Math.Min(options.MaxDepth, TomlLimits.AbsoluteMaxDepth);

        _rows = new TomlDocumentBuilder(options.SpecVersion, maxDepth).Parse(utf8Toml);
        _source = utf8Toml;
        _stack = new Frame[InitialStackDepth];
        _depth = 0;
        _started = false;
        _tokenType = TomlTokenType.None;
        _value = null;
        _offset = 0;
        _currentRow = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentReader" /> struct over the supplied sequence, enforcing
    /// strict TOML v1.0.0.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    /// <remarks>
    /// A single-segment sequence is parsed in place; a multi-segment sequence is copied once into a contiguous buffer
    /// before parsing.
    /// </remarks>
    public TomlDocumentReader(in System.Buffers.ReadOnlySequence<byte> utf8Toml)
        : this(utf8Toml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentReader" /> struct over the supplied sequence using the
    /// supplied options.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <param name="options">
    /// The reader options controlling the specification version and maximum nesting depth.
    /// </param>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    /// <remarks>
    /// A single-segment sequence is parsed in place; a multi-segment sequence is copied once into a contiguous buffer
    /// before parsing.
    /// </remarks>
    public TomlDocumentReader(in System.Buffers.ReadOnlySequence<byte> utf8Toml, TomlReaderOptions options)
        : this(utf8Toml.IsSingleSegment ? utf8Toml.FirstSpan : System.Buffers.BuffersExtensions.ToArray(utf8Toml), options)
    {
    }

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <returns>
    /// The current token kind, or <see cref="TomlTokenType.None" /> before the first or after the last token.
    /// </returns>
    public readonly TomlTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <returns>
    /// The depth, where zero is the document root. A top-level table or array opens depth one; nested containers
    /// increase it further.
    /// </returns>
    public readonly int CurrentDepth => _depth > 0 ? _depth - 1 : 0;

    /// <summary>
    /// Gets the flat row store the cursor walks.
    /// </summary>
    /// <returns>The shared row store.</returns>
    internal readonly List<TomlReaderRow> Rows => _rows;

    /// <summary>
    /// Gets the row index of the value the current token belongs to, so a subtree can be materialized over the shared
    /// store without copying.
    /// </summary>
    /// <returns>The current value's row index, or <c>-1</c> before the first token.</returns>
    internal readonly int CurrentRowIndex => _currentRow;

    /// <summary>
    /// Returns a garbage-collected copy of the source, creating it once and reusing it for every subtree document
    /// materialized from this read.
    /// </summary>
    /// <returns>The owned source copy, whose offsets match the shared row store.</returns>
    /// <remarks>
    /// A subtree document needs to retain the source for on-demand string decoding, but the reader holds it only as a
    /// span; this materializes a single shared copy so binding many element-typed members does not copy per element.
    /// </remarks>
    internal byte[] GetOwnedSource() =>
        _ownedSource ??= _source.ToArray();

    /// <summary>
    /// Records the source position of the current token on a binding failure, setting the byte offset, line number, and
    /// column number of the supplied exception when it does not already carry a position.
    /// </summary>
    /// <param name="exception">The exception to annotate with the current token's position.</param>
    /// <remarks>
    /// The guard on an existing offset ensures the innermost converter — closest to the offending value — wins as the
    /// exception unwinds through the enclosing container converters. The line and column count UTF-8 bytes, matching
    /// <see cref="TomlFormatException" />.
    /// </remarks>
    internal readonly void StampPosition(TomlSerializationException exception)
    {
        if (exception.Offset is not null || _tokenType == TomlTokenType.None)
            return;

        int offset = _offset;

        int line = 1;
        int lineStart = 0;
        int limit = Math.Min(offset, _source.Length);
        for (int i = 0; i < limit; i++)
        {
            if (_source[i] == (byte)'\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        exception.Offset = offset;
        exception.LineNumber = line;
        exception.ColumnNumber = offset - lineStart + 1;
    }

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    public bool Read()
    {
        if (!_started)
        {
            _started = true;
            Enter(0);
            return true;
        }

        if (_depth == 0)
        {
            _tokenType = TomlTokenType.None;
            return false;
        }

        int top = _depth - 1;
        int container = _stack[top].Container;

        if (_rows[container].Kind == TomlReaderNodeKind.Table)
        {
            if (_stack[top].AwaitingValue)
            {
                _stack[top].AwaitingValue = false;
                EmitValue(_stack[top].CurrentChild);
                return true;
            }

            int next = _stack[top].CurrentChild < 0 ? _rows[container].FirstChild : _rows[_stack[top].CurrentChild].NextSibling;
            if (next >= 0)
            {
                _stack[top].CurrentChild = next;
                SetToken(TomlTokenType.PropertyName, _rows[next].Key, _rows[next].Offset, next);
                _stack[top].AwaitingValue = true;
                return true;
            }

            SetToken(TomlTokenType.EndTable, null, _rows[container].Offset, container);
            _depth--;
            return true;
        }

        int nextElement = _stack[top].CurrentChild < 0 ? _rows[container].FirstChild : _rows[_stack[top].CurrentChild].NextSibling;
        if (nextElement >= 0)
        {
            _stack[top].CurrentChild = nextElement;
            EmitValue(nextElement);
            return true;
        }

        SetToken(TomlTokenType.EndArray, null, _rows[container].Offset, container);
        _depth--;
        return true;
    }

    /// <summary>
    /// Reads the current token as UTF-8 text.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" /> or
    /// <see cref="TomlTokenType.PropertyName" />.
    /// </exception>
    public readonly string GetString()
    {
        if (_tokenType == TomlTokenType.PropertyName)
            return _value!;

        if (_tokenType == TomlTokenType.String)
        {
            TomlReaderRow row = _rows[_currentRow];
            return Utf8TomlReader.DecodeString(_source.Slice(row.StringContentStart, row.StringContentLength), row.StringHasEscapes);
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Reads the current token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly long GetInt64() =>
        _tokenType == TomlTokenType.Integer
            ? _rows[_currentRow].AsInt64()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an IEEE 754 binary64 floating-point value.
    /// </summary>
    /// <returns>The floating-point value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    public readonly double GetDouble() =>
        _tokenType == TomlTokenType.Float
            ? _rows[_currentRow].AsDouble()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a Boolean.
    /// </summary>
    /// <returns>The Boolean value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Boolean" />.
    /// </exception>
    public readonly bool GetBoolean() =>
        _tokenType == TomlTokenType.Boolean
            ? _rows[_currentRow].AsBoolean()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an offset date-time.
    /// </summary>
    /// <returns>The offset date-time value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.OffsetDateTime" />.
    /// </exception>
    public readonly DateTimeOffset GetDateTimeOffset() =>
        _tokenType == TomlTokenType.OffsetDateTime
            ? _rows[_currentRow].AsDateTimeOffset()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local date-time.
    /// </summary>
    /// <returns>
    /// The local date-time value, whose <see cref="DateTime.Kind" /> is <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalDateTime" />.
    /// </exception>
    public readonly DateTime GetDateTime() =>
        _tokenType == TomlTokenType.LocalDateTime
            ? _rows[_currentRow].AsDateTime()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local date.
    /// </summary>
    /// <returns>The local date value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalDate" />.
    /// </exception>
    public readonly DateOnly GetDateOnly() =>
        _tokenType == TomlTokenType.LocalDate
            ? _rows[_currentRow].AsDateOnly()
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local time.
    /// </summary>
    /// <returns>The local time value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalTime" />.
    /// </exception>
    public readonly TimeOnly GetTimeOnly() =>
        _tokenType == TomlTokenType.LocalTime
            ? _rows[_currentRow].AsTimeOnly()
            : throw new InvalidOperationException();

    /// <summary>
    /// Skips the current value, including the entire subtree when the reader is positioned on a
    /// <see cref="TomlTokenType.StartTable" /> or <see cref="TomlTokenType.StartArray" />.
    /// </summary>
    /// <remarks>
    /// When the reader is positioned on a <see cref="TomlTokenType.PropertyName" />, it advances to the property's
    /// value and then skips it. When it is positioned on a container start, the reader advances to the matching
    /// <see cref="TomlTokenType.EndTable" /> or <see cref="TomlTokenType.EndArray" /> at the same depth. When it is
    /// positioned on a scalar value, the call has no effect.
    /// </remarks>
    public void Skip()
    {
        if (_tokenType == TomlTokenType.PropertyName)
            _ = Read();

        if (_tokenType is not (TomlTokenType.StartTable or TomlTokenType.StartArray))
            return;

        int depth = _depth;
        while (_depth >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Emits the next token for a table value or array element: a scalar token for a scalar row, or the opening token
    /// of a nested container, which is then descended into.
    /// </summary>
    /// <param name="row">The row index of the value to emit.</param>
    private void EmitValue(int row)
    {
        if (_rows[row].Kind == TomlReaderNodeKind.Scalar)
        {
            SetToken(_rows[row].TokenType, null, _rows[row].Offset, row);
            return;
        }

        Enter(row);
    }

    /// <summary>
    /// Opens a container by emitting its start token and pushing a traversal frame for it onto the stack.
    /// </summary>
    /// <param name="row">The row index of the table or array to open.</param>
    private void Enter(int row)
    {
        if (_depth == _stack.Length)
            Array.Resize(ref _stack, _stack.Length * 2);

        _stack[_depth] = new Frame(row);
        _depth++;

        SetToken(
            _rows[row].Kind == TomlReaderNodeKind.Table ? TomlTokenType.StartTable : TomlTokenType.StartArray,
            null,
            _rows[row].Offset,
            row);
    }

    /// <summary>
    /// Sets the current token's kind, value, source offset, and owning row.
    /// </summary>
    /// <param name="tokenType">The kind of the token.</param>
    /// <param name="value">
    /// The string carried by the token (a key or string scalar), or <see langword="null" />.
    /// </param>
    /// <param name="offset">The zero-based source byte offset at which the token begins.</param>
    /// <param name="row">The row index the token belongs to.</param>
    private void SetToken(TomlTokenType tokenType, string? value, int offset, int row)
    {
        _tokenType = tokenType;
        _value = value;
        _offset = offset;
        _currentRow = row;
    }

    /// <summary>
    /// A traversal frame for one open container: the container row and the cursor's position within it.
    /// </summary>
    private struct Frame
    {
        /// <summary>The row index of the container this frame walks.</summary>
        public int Container;

        /// <summary>The row index of the child currently being emitted, or <c>-1</c> before the first child.</summary>
        public int CurrentChild;

        /// <summary>For a table frame, whether the property name for <see cref="CurrentChild" /> has been emitted and its value is the next token to produce.</summary>
        public bool AwaitingValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Frame" /> struct positioned before the container's first child.
        /// </summary>
        /// <param name="container">The row index of the container the frame walks.</param>
        public Frame(int container)
        {
            Container = container;
            CurrentChild = -1;
            AwaitingValue = false;
        }
    }
}
