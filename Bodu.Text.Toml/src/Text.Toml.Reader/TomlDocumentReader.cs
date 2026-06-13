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
/// structural value tree, and <see cref="Read" /> advances a depth-first cursor over that tree, emitting the normalized
/// token stream on demand rather than materializing it. This type walks a parsed document; <see cref="Utf8TomlReader" />
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
    /// <summary>
    /// The initial capacity of the traversal stack; it grows on demand for documents nested deeper than this.
    /// </summary>
    private const int InitialStackDepth = 8;

    /// <summary>
    /// The root table of the parsed structural tree the cursor walks.
    /// </summary>
    private readonly TomlTableNode _root;

    /// <summary>
    /// The UTF-8 source bytes, retained so a token's byte offset can be mapped to a line and column on a binding
    /// failure. Held as a span because the reader is a <see langword="ref struct" /> scoped to a single read.
    /// </summary>
    private readonly ReadOnlySpan<byte> _source;

    /// <summary>
    /// The stack of open containers, one frame per open table or array, innermost last.
    /// </summary>
    private Frame[] _stack;

    /// <summary>
    /// The number of open containers on <see cref="_stack" />, counting the document-root table. The publicly reported
    /// <see cref="CurrentDepth" /> excludes the root, so it is this value less one.
    /// </summary>
    private int _depth;

    /// <summary>
    /// Whether the first <see cref="Read" /> has occurred, after which the cursor is positioned within the tree.
    /// </summary>
    private bool _started;

    /// <summary>
    /// The kind of the current token.
    /// </summary>
    private TomlTokenType _tokenType;

    /// <summary>
    /// The value carried by the current token: a key for a property name, the decoded CLR value for a scalar, or
    /// <see langword="null" /> for a structural token.
    /// </summary>
    private object? _value;

    /// <summary>
    /// The zero-based source byte offset at which the current token begins.
    /// </summary>
    private int _offset;

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
    /// A <see cref="TomlReaderOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public TomlDocumentReader(ReadOnlySpan<byte> utf8Toml, TomlReaderOptions options)
    {
        var maxDepth = options.MaxDepth <= 0 ? 256 : options.MaxDepth;

        _root = new TomlDocumentBuilder(options.SpecVersion, maxDepth).Parse(utf8Toml);
        _source = utf8Toml;
        _stack = new Frame[InitialStackDepth];
        _depth = 0;
        _started = false;
        _tokenType = TomlTokenType.None;
        _value = null;
        _offset = 0;
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

        var offset = _offset;

        var line = 1;
        var lineStart = 0;
        var limit = Math.Min(offset, _source.Length);
        for (var i = 0; i < limit; i++)
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
            Enter(_root);
            return true;
        }

        if (_depth == 0)
        {
            _tokenType = TomlTokenType.None;
            return false;
        }

        var top = _depth - 1;
        if (_stack[top].Node.Kind == TomlReaderNodeKind.Table)
        {
            var table = (TomlTableNode)_stack[top].Node;
            if (_stack[top].AwaitingValue)
            {
                _stack[top].AwaitingValue = false;
                TomlReaderNode value = table.Items[_stack[top].Index].Value;
                _stack[top].Index++;
                EmitValue(value);
                return true;
            }

            if (_stack[top].Index < table.Items.Count)
            {
                KeyValuePair<string, TomlReaderNode> entry = table.Items[_stack[top].Index];
                SetToken(TomlTokenType.PropertyName, entry.Key, table.Offset);
                _stack[top].AwaitingValue = true;
                return true;
            }

            SetToken(TomlTokenType.EndTable, null, table.Offset);
            _depth--;
            return true;
        }

        var array = (TomlArrayNode)_stack[top].Node;
        if (_stack[top].Index < array.Count)
        {
            TomlReaderNode item = array.Items[_stack[top].Index];
            _stack[top].Index++;
            EmitValue(item);
            return true;
        }

        SetToken(TomlTokenType.EndArray, null, array.Offset);
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
    public readonly string GetString() =>
        _tokenType is TomlTokenType.String or TomlTokenType.PropertyName
            ? (string)_value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly long GetInt64() =>
        _tokenType == TomlTokenType.Integer
            ? (long)_value!
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
            ? (double)_value!
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
            ? (bool)_value!
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
            ? (DateTimeOffset)_value!
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
            ? (DateTime)_value!
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
            ? (DateOnly)_value!
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
            ? (TimeOnly)_value!
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

        if (_tokenType is not(TomlTokenType.StartTable or TomlTokenType.StartArray))
            return;

        var depth = _depth;
        while (_depth >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Emits the next token for a table value or array element: a scalar token for a scalar node, or the opening token
    /// of a nested container, which is then descended into.
    /// </summary>
    /// <param name="node">The value node to emit.</param>
    private void EmitValue(TomlReaderNode node)
    {
        if (node.Kind == TomlReaderNodeKind.Scalar)
        {
            var scalar = (TomlScalarNode)node;
            SetToken(scalar.TokenType, scalar.Value, scalar.Offset);
            return;
        }

        Enter(node);
    }

    /// <summary>
    /// Opens a container by emitting its start token and pushing a traversal frame for it onto the stack.
    /// </summary>
    /// <param name="node">The table or array node to open.</param>
    private void Enter(TomlReaderNode node)
    {
        if (_depth == _stack.Length)
            Array.Resize(ref _stack, _stack.Length * 2);

        _stack[_depth] = new Frame(node);
        _depth++;

        SetToken(
            node.Kind == TomlReaderNodeKind.Table ? TomlTokenType.StartTable : TomlTokenType.StartArray,
            null,
            node.Offset);
    }

    /// <summary>
    /// Sets the current token's kind, value, and source offset.
    /// </summary>
    /// <param name="tokenType">The kind of the token.</param>
    /// <param name="value">The value carried by the token, or <see langword="null" /> for a structural token.</param>
    /// <param name="offset">The zero-based source byte offset at which the token begins.</param>
    private void SetToken(TomlTokenType tokenType, object? value, int offset)
    {
        _tokenType = tokenType;
        _value = value;
        _offset = offset;
    }

    /// <summary>
    /// A traversal frame for one open container: the container node and the cursor's position within it.
    /// </summary>
    private struct Frame
    {
        /// <summary>
        /// The container node this frame walks, a <see cref="TomlTableNode" /> or a <see cref="TomlArrayNode" />.
        /// </summary>
        public TomlReaderNode Node;

        /// <summary>
        /// The index of the next child to emit.
        /// </summary>
        public int Index;

        /// <summary>
        /// For a table frame, whether the property name for <see cref="Index" /> has been emitted and its value is the
        /// next token to produce.
        /// </summary>
        public bool AwaitingValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Frame" /> struct positioned before the container's first child.
        /// </summary>
        /// <param name="node">The container node the frame walks.</param>
        public Frame(TomlReaderNode node)
        {
            Node = node;
            Index = 0;
            AwaitingValue = false;
        }
    }
}
