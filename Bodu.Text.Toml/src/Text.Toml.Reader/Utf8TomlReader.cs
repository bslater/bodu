// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Provides a forward-only token reader for UTF-8 TOML bytes, mirroring the API shape of
/// <see cref="System.Text.Json.Utf8JsonReader" /> over a document that is parsed in full by the constructor. The
/// reader is a <see langword="ref struct" />, so it cannot be boxed or captured; pass it by <see langword="ref" /> to
/// thread it through a converter.
/// </summary>
/// <remarks>
/// <para>
/// Unlike JSON, TOML cannot be tokenized in a single forward pass: out-of-line <c>[table]</c> and
/// <c>[[array-of-tables]]</c> headers contribute to structure declared elsewhere in the document. The constructor
/// therefore decodes and parses the entire document up front — enforcing TOML's key, value, table, and array-of-tables
/// rules — and <see cref="Read" /> advances a cursor over the resulting, fully materialized token stream.
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
/// <see cref="DateTime" /> and <see cref="TimeOnly" /> cannot represent second 60; year <c>0000</c> is rejected
/// because the CLR calendar begins at year 1; and offsets beyond ±14:00 are rejected by
/// <see cref="DateTimeOffset" />. Each surfaces as a <see cref="TomlFormatException" />.
/// </para>
/// </remarks>
public ref struct Utf8TomlReader
{
    /// <summary>
    /// The UTF-8 encoding used to decode the source; invalid byte sequences are rejected rather than replaced.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The fully materialized, normalized token stream.
    /// </summary>
    private readonly List<TomlReaderToken> _tokens;

    /// <summary>
    /// The index of the current token, or <c>-1</c> before the first <see cref="Read" />.
    /// </summary>
    private int _index;

    /// <summary>
    /// The number of containers currently open, counting the synthetic document-root table. The publicly reported
    /// <see cref="CurrentDepth" /> excludes the root, so it is this value less one.
    /// </summary>
    private int _openDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlReader" /> struct over the supplied bytes, enforcing strict
    /// TOML v1.0.0.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    public Utf8TomlReader(ReadOnlySpan<byte> utf8Toml)
        : this(utf8Toml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlReader" /> struct over the supplied bytes using the
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
    public Utf8TomlReader(ReadOnlySpan<byte> utf8Toml, TomlReaderOptions options)
    {
        var maxDepth = options.MaxDepth <= 0 ? 256 : options.MaxDepth;

        var source = Decode(utf8Toml);
        TomlTableNode root = new TomlDocumentParser(source, options.SpecVersion, maxDepth).Parse();

        _tokens = new List<TomlReaderToken>();
        Flatten(root);

        _index = -1;
        _openDepth = 0;
    }

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <returns>
    /// The current token kind, or <see cref="TomlTokenType.None" /> before the first or after the last token.
    /// </returns>
    public readonly TomlTokenType TokenType =>
        _index >= 0 && _index < _tokens.Count ? _tokens[_index].TokenType : TomlTokenType.None;

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <returns>
    /// The depth, where zero is the document root. A top-level table or array opens depth one; nested containers
    /// increase it further.
    /// </returns>
    public readonly int CurrentDepth => _openDepth > 0 ? _openDepth - 1 : 0;

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    public bool Read()
    {
        if (_index + 1 >= _tokens.Count)
        {
            // Position past the final token so that TokenType reports None once the document is exhausted.
            _index = _tokens.Count;
            return false;
        }

        _index++;
        switch (_tokens[_index].TokenType)
        {
            case TomlTokenType.StartTable:
            case TomlTokenType.StartArray:
                _openDepth++;
                break;

            case TomlTokenType.EndTable:
            case TomlTokenType.EndArray:
                _openDepth--;
                break;
        }

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
        TokenType is TomlTokenType.String or TomlTokenType.PropertyName
            ? (string)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly long GetInt64() =>
        TokenType == TomlTokenType.Integer
            ? (long)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an IEEE 754 binary64 floating-point value.
    /// </summary>
    /// <returns>The floating-point value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    public readonly double GetDouble() =>
        TokenType == TomlTokenType.Float
            ? (double)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a Boolean.
    /// </summary>
    /// <returns>The Boolean value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Boolean" />.
    /// </exception>
    public readonly bool GetBoolean() =>
        TokenType == TomlTokenType.Boolean
            ? (bool)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an offset date-time.
    /// </summary>
    /// <returns>The offset date-time value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.OffsetDateTime" />.
    /// </exception>
    public readonly DateTimeOffset GetDateTimeOffset() =>
        TokenType == TomlTokenType.OffsetDateTime
            ? (DateTimeOffset)_tokens[_index].Value!
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
        TokenType == TomlTokenType.LocalDateTime
            ? (DateTime)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local date.
    /// </summary>
    /// <returns>The local date value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalDate" />.
    /// </exception>
    public readonly DateOnly GetDateOnly() =>
        TokenType == TomlTokenType.LocalDate
            ? (DateOnly)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local time.
    /// </summary>
    /// <returns>The local time value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalTime" />.
    /// </exception>
    public readonly TimeOnly GetTimeOnly() =>
        TokenType == TomlTokenType.LocalTime
            ? (TimeOnly)_tokens[_index].Value!
            : throw new InvalidOperationException();

    /// <summary>
    /// Skips the current value, including the entire subtree when the reader is positioned on a
    /// <see cref="TomlTokenType.StartTable" /> or <see cref="TomlTokenType.StartArray" />.
    /// </summary>
    /// <remarks>
    /// When the reader is positioned on a <see cref="TomlTokenType.PropertyName" />, it advances to the property's
    /// value and then skips it, mirroring <see cref="System.Text.Json.Utf8JsonReader.Skip" />. When it is positioned on
    /// a container start, the reader advances to the matching <see cref="TomlTokenType.EndTable" /> or
    /// <see cref="TomlTokenType.EndArray" /> at the same depth. When it is positioned on a scalar value, the call has
    /// no effect.
    /// </remarks>
    public void Skip()
    {
        if (TokenType == TomlTokenType.PropertyName)
            _ = Read();

        if (TokenType is not(TomlTokenType.StartTable or TomlTokenType.StartArray))
            return;

        var depth = _openDepth;
        while (_openDepth >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Decodes the supplied bytes as UTF-8 text.
    /// </summary>
    /// <param name="utf8Toml">The bytes to decode.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not valid UTF-8.</exception>
    private static string Decode(ReadOnlySpan<byte> utf8Toml)
    {
        try
        {
            return s_utf8.GetString(utf8Toml);
        }
        catch (DecoderFallbackException ex)
        {
            throw new TomlFormatException(TomlResourceStrings.Format_Invalid_TomlInvalidUtf8, ex);
        }
    }

    /// <summary>
    /// Appends the tokens of a table to the stream as <see cref="TomlTokenType.StartTable" />, its entries, and
    /// <see cref="TomlTokenType.EndTable" />.
    /// </summary>
    /// <param name="table">The table to flatten.</param>
    private readonly void Flatten(TomlTableNode table)
    {
        _tokens.Add(new TomlReaderToken(TomlTokenType.StartTable, null, table.Offset));
        foreach (KeyValuePair<string, TomlReaderNode> pair in table.Items)
        {
            _tokens.Add(new TomlReaderToken(TomlTokenType.PropertyName, pair.Key, table.Offset));
            Flatten(pair.Value);
        }

        _tokens.Add(new TomlReaderToken(TomlTokenType.EndTable, null, table.Offset));
    }

    /// <summary>
    /// Appends the tokens of a value node to the stream.
    /// </summary>
    /// <param name="node">The node to flatten.</param>
    private readonly void Flatten(TomlReaderNode node)
    {
        switch (node)
        {
            case TomlTableNode table:
                Flatten(table);
                break;

            case TomlArrayNode array:
                _tokens.Add(new TomlReaderToken(TomlTokenType.StartArray, null, array.Offset));
                foreach (TomlReaderNode item in array.Items)
                    Flatten(item);
                _tokens.Add(new TomlReaderToken(TomlTokenType.EndArray, null, array.Offset));
                break;

            case TomlScalarNode scalar:
                _tokens.Add(new TomlReaderToken(scalar.TokenType, scalar.Value, scalar.Offset));
                break;
        }
    }
}
