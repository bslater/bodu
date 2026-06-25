// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Provides a forward-only, read-only reader over a YAML document, surfacing its nodes as a stream of structural and
/// scalar tokens in document order, in the manner of <see cref="System.Text.Json.Utf8JsonReader" />.
/// </summary>
/// <remarks>
/// <para>
/// YAML cannot be tokenized by a pure forward-only single-pass reader because indentation context, back-referencing
/// aliases, and document boundaries all require look-ahead. This reader therefore parses the source into a buffered
/// node store on construction and then presents a forward-only token view over that store, so the public surface
/// matches the sibling <c>Utf8TomlReader</c> while the buffering remains an internal detail.
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
        var parser = new YamlParser(buffer, buffer.Length, options.SpecVersion, options.EffectiveMaxDepth);
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

        return Encoding.UTF8.GetByteCount(_key) == utf8Text.Length
            && Encoding.UTF8.GetString(utf8Text) == _key;
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
