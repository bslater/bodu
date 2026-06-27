// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Provides a forward-only, read-only token cursor over a parsed YAML document, surfacing its nodes as structural and
/// scalar tokens in document order. The token surface mirrors <see cref="System.Text.Json.Utf8JsonReader" />, but the
/// underlying reader is buffered rather than a single-pass streaming scanner.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="System.Text.Json.Utf8JsonReader" />, this type is not a low-allocation single-pass streaming
/// reader. YAML cannot be tokenized in a single forward pass because indentation context, back-referencing aliases,
/// merge keys, and document boundaries all require look-ahead and composition. The constructor therefore copies the
/// source and fully parses it into an in-memory node store; <see cref="Read" /> then walks that store in document
/// order. In that respect the reader is the analogue of the sibling <c>TomlDocumentReader</c> cursor rather than the
/// streaming <c>Utf8TomlReader</c> scanner.
/// </para>
/// <para>
/// The reader honors the library's YAML 1.2 core, JSON-compatible tree profile: mapping keys must resolve to scalar
/// strings, mapping keys must be unique, anchors must be unique and acyclic, and tabs are not permitted as indentation.
/// Inputs that violate the profile are rejected with <see cref="YamlFormatException" />.
/// </para>
/// <para>
/// The reader is a <see langword="ref struct" /> and cannot be boxed, stored on the heap, or captured by a lambda.
/// </para>
/// </remarks>
public ref struct Utf8YamlReader
{
    private readonly List<YamlReaderRow> _rows;
    private readonly string[] _strings;
    private Frame[] _stack;
    private int _depth;
    private bool _started;

    private YamlTokenType _tokenType;
    private int _currentRow;
    private string? _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlReader" /> struct over UTF-8 source.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public Utf8YamlReader(ReadOnlySpan<byte> utf8Yaml)
        : this(utf8Yaml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlReader" /> struct over UTF-8 source with options.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <param name="options">The reader options.</param>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public Utf8YamlReader(ReadOnlySpan<byte> utf8Yaml, YamlReaderOptions options)
    {
        var buffer = utf8Yaml.ToArray();
        var parser = new YamlParser(buffer, buffer.Length, options.SpecVersion, options.EffectiveMaxDepth, options.DuplicateKeyBehavior, options.MergeKeyBehavior);
        _rows = parser.Parse();
        _strings = parser.Strings.ToArray();
        _stack = new Frame[8];
        _depth = 0;
        _started = false;
        _tokenType = YamlTokenType.None;
        _currentRow = -1;
        _key = null;
    }

    /// <summary>
    /// Gets the type of the current token.
    /// </summary>
    /// <value>The current token type, or <see cref="YamlTokenType.None" /> before the first read.</value>
    public readonly YamlTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the nesting depth of the current token, with the root at zero.
    /// </summary>
    /// <value>The current container depth.</value>
    public readonly int CurrentDepth => _depth;

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
            return BeginNode(0);
        }

        while (_depth > 0)
        {
            ref var frame = ref _stack[_depth - 1];
            if (frame.IsMapping)
            {
                if (frame.AwaitingValue)
                {
                    frame.AwaitingValue = false;
                    return BeginNode(frame.Current);
                }

                var next = frame.Current < 0 ? FirstChild(frame.Container) : NextSibling(frame.Current);
                if (next < 0)
                {
                    _tokenType = YamlTokenType.EndMapping;
                    _currentRow = frame.Container;
                    _depth--;
                    return true;
                }

                frame.Current = next;
                frame.AwaitingValue = true;
                _tokenType = YamlTokenType.PropertyName;
                _currentRow = next;
                _key = _rows[next].Key;
                return true;
            }
            else
            {
                var next = frame.Current < 0 ? FirstChild(frame.Container) : NextSibling(frame.Current);
                if (next < 0)
                {
                    _tokenType = YamlTokenType.EndSequence;
                    _currentRow = frame.Container;
                    _depth--;
                    return true;
                }

                frame.Current = next;
                return BeginNode(next);
            }
        }

        _tokenType = YamlTokenType.None;
        return false;
    }

    /// <summary>
    /// Returns the string value of the current property name or string scalar token.
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current token is not a property name or string scalar.
    /// </exception>
    public readonly string GetString()
    {
        if (_tokenType == YamlTokenType.PropertyName)
            return _key ?? string.Empty;

        if (_tokenType == YamlTokenType.String)
            return _strings[(int)_rows[_currentRow].ScalarBits];

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Returns the integer value of the current integer scalar token.
    /// </summary>
    /// <returns>The 64-bit integer value.</returns>
    /// <exception cref="InvalidOperationException">The current token is not an integer scalar.</exception>
    public readonly long GetInt64()
    {
        if (_tokenType != YamlTokenType.Integer)
            throw new InvalidOperationException();

        return _rows[_currentRow].AsInt64();
    }

    /// <summary>
    /// Returns the floating-point value of the current numeric scalar token.
    /// </summary>
    /// <returns>The double-precision value.</returns>
    /// <exception cref="InvalidOperationException">The current token is not a numeric scalar.</exception>
    public readonly double GetDouble()
    {
        var r = _rows[_currentRow];
        return _tokenType switch
        {
            YamlTokenType.Float => r.AsDouble(),
            YamlTokenType.Integer => r.AsInt64(),
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// Returns the boolean value of the current boolean scalar token.
    /// </summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException">The current token is not a boolean scalar.</exception>
    public readonly bool GetBoolean()
    {
        if (_tokenType != YamlTokenType.Boolean)
            throw new InvalidOperationException();

        return _rows[_currentRow].AsBoolean();
    }

    /// <summary>
    /// Determines whether the current property name equals the given UTF-8 text.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to compare against.</param>
    /// <returns><see langword="true" /> when the current property name matches.</returns>
    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text)
    {
        if (_tokenType != YamlTokenType.PropertyName || _key is null)
            return false;

        // The decoded UTF-16 length never exceeds the UTF-8 byte length, so a property name of typical size is compared
        // without allocating; only an unusually long key falls back to the heap.
        Span<char> chars = utf8Text.Length <= 256 ? stackalloc char[256] : new char[utf8Text.Length];
        var written = Encoding.UTF8.GetChars(utf8Text, chars);
        return _key.AsSpan().SequenceEqual(chars[..written]);
    }

    /// <summary>
    /// Begins emitting a node, producing a scalar token directly or a container-start token with a pushed frame.
    /// </summary>
    /// <param name="row">The row index of the node to begin.</param>
    /// <returns>Always <see langword="true" />.</returns>
    private bool BeginNode(int row)
    {
        var index = Resolve(row);
        var r = _rows[index];
        _currentRow = index;

        switch (r.Kind)
        {
            case YamlReaderNodeKind.Mapping:
                _tokenType = YamlTokenType.StartMapping;
                Push(index, isMapping: true);
                return true;

            case YamlReaderNodeKind.Sequence:
                _tokenType = YamlTokenType.StartSequence;
                Push(index, isMapping: false);
                return true;

            default:
                _tokenType = r.ValueKind switch
                {
                    YamlValueKind.String => YamlTokenType.String,
                    YamlValueKind.Integer => YamlTokenType.Integer,
                    YamlValueKind.Float => YamlTokenType.Float,
                    YamlValueKind.Boolean => YamlTokenType.Boolean,
                    _ => YamlTokenType.Null,
                };
                return true;
        }
    }

    /// <summary>
    /// Resolves an alias row to its target when available.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The resolved row index.</returns>
    private readonly int Resolve(int index)
    {
        var r = _rows[index];
        return r.Kind == YamlReaderNodeKind.Alias && r.AliasTarget >= 0 ? r.AliasTarget : index;
    }

    /// <summary>
    /// Gets the first child row index of a container.
    /// </summary>
    /// <param name="index">The container row index.</param>
    /// <returns>The first child row index, or <c>-1</c>.</returns>
    private readonly int FirstChild(int index) => _rows[index].FirstChild;

    /// <summary>
    /// Gets the next sibling row index of a node.
    /// </summary>
    /// <param name="index">The node row index.</param>
    /// <returns>The next sibling row index, or <c>-1</c>.</returns>
    private readonly int NextSibling(int index) => _rows[index].NextSibling;

    /// <summary>
    /// Pushes a traversal frame for a container, growing the stack as needed.
    /// </summary>
    /// <param name="container">The container row index.</param>
    /// <param name="isMapping">Whether the container is a mapping.</param>
    private void Push(int container, bool isMapping)
    {
        if (_depth == _stack.Length)
            Array.Resize(ref _stack, _stack.Length * 2);

        _stack[_depth] = new Frame { Container = container, Current = -1, IsMapping = isMapping, AwaitingValue = false };
        _depth++;
    }

    /// <summary>
    /// A traversal frame describing progress through a container node.
    /// </summary>
    private struct Frame
    {
        /// <summary>The container row index.</summary>
        public int Container;

        /// <summary>The current child row index, or <c>-1</c> before the first child.</summary>
        public int Current;

        /// <summary>Whether the container is a mapping.</summary>
        public bool IsMapping;

        /// <summary>For mappings, whether the value of the current property is pending emission.</summary>
        public bool AwaitingValue;
    }
}
