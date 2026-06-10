// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization.Syntax;
using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Presents a parsed TOML document to the serialization engine as a forward-only stream of format-neutral tokens.
/// </summary>
/// <remarks>
/// TOML scalars map directly to the corresponding value kinds. A byte array is read by base64-decoding a string value,
/// the inverse of how the writer renders one.
/// </remarks>
internal sealed class TomlReaderAdapter
    : ISerializationReader
{
    /// <summary>
    /// The flattened token stream.
    /// </summary>
    private readonly List<ReaderToken> _tokens = [];

    /// <summary>
    /// The index of the current token, or -1 before the first read.
    /// </summary>
    private int _index = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReaderAdapter" /> class over a parsed document.
    /// </summary>
    /// <param name="document">The parsed TOML document.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    internal TomlReaderAdapter(TomlDocumentSyntax document)
    {
        ThrowHelper.ThrowIfNull(document);
        FlattenTable(document.Root);
    }

    /// <inheritdoc />
    public SerializationValueKind TokenKind =>
        _index >= 0 && _index < _tokens.Count ? _tokens[_index].Kind : SerializationValueKind.None;

    /// <inheritdoc />
    public TextLocation Location => new(_index >= 0 && _index < _tokens.Count ? _tokens[_index].Offset : 0);

    /// <inheritdoc />
    public bool Read()
    {
        if (_index + 1 >= _tokens.Count)
            return false;

        _index++;
        return true;
    }

    /// <inheritdoc />
    public void Skip()
    {
        if (TokenKind is not(SerializationValueKind.StartObject or SerializationValueKind.StartArray))
            return;

        var depth = 1;
        while (depth > 0 && Read())
        {
            depth += TokenKind switch
            {
                SerializationValueKind.StartObject or SerializationValueKind.StartArray => 1,
                SerializationValueKind.EndObject or SerializationValueKind.EndArray => -1,
                _ => 0,
            };
        }
    }

    /// <inheritdoc />
    public string GetPropertyName() => (string)Require(SerializationValueKind.PropertyName) !;

    /// <inheritdoc />
    public string GetString() => (string)Require(SerializationValueKind.String) !;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetBytes() => Convert.FromBase64String(GetString());

    /// <inheritdoc />
    public long GetInt64() => (long)Require(SerializationValueKind.Int64) !;

    /// <inheritdoc />
    public ulong GetUInt64()
    {
        var value = GetInt64();
        return value >= 0 ? (ulong)value : throw Mismatch(SerializationValueKind.UInt64);
    }

    /// <inheritdoc />
    public double GetDouble() => (double)Require(SerializationValueKind.Double) !;

    /// <inheritdoc />
    public bool GetBoolean() => (bool)Require(SerializationValueKind.Boolean) !;

    /// <inheritdoc />
    public DateTimeOffset GetDateTimeOffset() => (DateTimeOffset)Require(SerializationValueKind.DateTimeOffset) !;

    /// <inheritdoc />
    public DateTime GetDateTime() => (DateTime)Require(SerializationValueKind.DateTime) !;

    /// <inheritdoc />
    public DateOnly GetDateOnly() => (DateOnly)Require(SerializationValueKind.DateOnly) !;

    /// <inheritdoc />
    public TimeOnly GetTimeOnly() => (TimeOnly)Require(SerializationValueKind.TimeOnly) !;

    /// <summary>
    /// Flattens a table into the token stream.
    /// </summary>
    /// <param name="table">The table to flatten.</param>
    private void FlattenTable(TomlTableSyntax table)
    {
        _tokens.Add(new ReaderToken(SerializationValueKind.StartObject, null, table.Span.Start));
        foreach (KeyValuePair<string, TomlSyntaxNode> pair in table.Items)
        {
            _tokens.Add(new ReaderToken(SerializationValueKind.PropertyName, pair.Key, table.Span.Start));
            Flatten(pair.Value);
        }

        _tokens.Add(new ReaderToken(SerializationValueKind.EndObject, null, table.Span.End));
    }

    /// <summary>
    /// Flattens a value node into the token stream.
    /// </summary>
    /// <param name="node">The node to flatten.</param>
    private void Flatten(TomlSyntaxNode node)
    {
        switch (node)
        {
            case TomlTableSyntax table:
                FlattenTable(table);
                break;

            case TomlArraySyntax array:
                _tokens.Add(new ReaderToken(SerializationValueKind.StartArray, null, array.Span.Start));
                foreach (TomlSyntaxNode item in array.Items)
                    Flatten(item);
                _tokens.Add(new ReaderToken(SerializationValueKind.EndArray, null, array.Span.End));
                break;

            case TomlScalarSyntax scalar:
                _tokens.Add(new ReaderToken(ScalarKind(scalar.Kind), scalar.Value, scalar.Span.Start));
                break;
        }
    }

    /// <summary>
    /// Maps a TOML scalar kind to the format-neutral value kind.
    /// </summary>
    /// <param name="kind">The TOML scalar kind.</param>
    /// <returns>The neutral value kind.</returns>
    private static SerializationValueKind ScalarKind(TomlSyntaxKind kind) =>
        kind switch
        {
            TomlSyntaxKind.String => SerializationValueKind.String,
            TomlSyntaxKind.Integer => SerializationValueKind.Int64,
            TomlSyntaxKind.Float => SerializationValueKind.Double,
            TomlSyntaxKind.Boolean => SerializationValueKind.Boolean,
            TomlSyntaxKind.OffsetDateTime => SerializationValueKind.DateTimeOffset,
            TomlSyntaxKind.LocalDateTime => SerializationValueKind.DateTime,
            TomlSyntaxKind.LocalDate => SerializationValueKind.DateOnly,
            _ => SerializationValueKind.TimeOnly,
        };

    /// <summary>
    /// Returns the current token's value, asserting it is of the expected kind.
    /// </summary>
    /// <param name="expected">The expected token kind.</param>
    /// <returns>The token value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not of the expected kind.
    /// </exception>
    private object? Require(SerializationValueKind expected) =>
        TokenKind == expected ? _tokens[_index].Value : throw Mismatch(expected);

    /// <summary>
    /// Creates the exception thrown when the current token does not match the requested kind.
    /// </summary>
    /// <param name="expected">The expected kind.</param>
    /// <returns>The exception to throw.</returns>
    private InvalidOperationException Mismatch(SerializationValueKind expected) =>
        new(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_TypeMismatch, TokenKind, expected));

    /// <summary>
    /// A single token in the flattened stream.
    /// </summary>
    private readonly struct ReaderToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderToken" /> struct.
        /// </summary>
        /// <param name="kind">The token kind.</param>
        /// <param name="value">The token value.</param>
        /// <param name="offset">The source offset of the token.</param>
        internal ReaderToken(SerializationValueKind kind, object? value, int offset)
        {
            Kind = kind;
            Value = value;
            Offset = offset;
        }

        /// <summary>
        /// Gets the token kind.
        /// </summary>
        /// <returns>The token kind.</returns>
        internal SerializationValueKind Kind { get; }

        /// <summary>
        /// Gets the token value.
        /// </summary>
        /// <returns>The token value.</returns>
        internal object? Value { get; }

        /// <summary>
        /// Gets the source offset of the token.
        /// </summary>
        /// <returns>The source offset.</returns>
        internal int Offset { get; }
    }
}
