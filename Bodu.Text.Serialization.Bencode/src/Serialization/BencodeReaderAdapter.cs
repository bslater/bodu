// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReaderAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization.Bencode.Syntax;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Presents a parsed Bencode document to the serialization engine as a forward-only stream of format-neutral tokens.
/// </summary>
/// <remarks>
/// A Bencode integer surfaces as a 64-bit integer token and a byte string as a value readable either as a string
/// (decoded as UTF-8) or as raw bytes. Lists surface as arrays and dictionaries as objects whose keys are the property
/// names. Kinds the Bencode grammar cannot produce — Booleans, floating-point values, and date-times — are reported as
/// unsupported when a converter attempts to read them.
/// </remarks>
internal sealed class BencodeReaderAdapter
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
    /// Initializes a new instance of the <see cref="BencodeReaderAdapter" /> class over a parsed document.
    /// </summary>
    /// <param name="document">The parsed Bencode document.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    internal BencodeReaderAdapter(BencodeDocumentSyntax document)
    {
        ThrowHelper.ThrowIfNull(document);
        Flatten(document.Root);
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
    public string GetString() => System.Text.Encoding.UTF8.GetString(Bytes().Span);

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetBytes() => Bytes();

    /// <inheritdoc />
    public long GetInt64() => (long)Require(SerializationValueKind.Int64) !;

    /// <inheritdoc />
    public ulong GetUInt64()
    {
        var value = GetInt64();
        return value >= 0
            ? (ulong)value
            : throw Unsupported(SerializationValueKind.UInt64);
    }

    /// <inheritdoc />
    public double GetDouble() => throw Unsupported(SerializationValueKind.Double);

    /// <inheritdoc />
    public bool GetBoolean() => throw Unsupported(SerializationValueKind.Boolean);

    /// <inheritdoc />
    public DateTimeOffset GetDateTimeOffset() => throw Unsupported(SerializationValueKind.DateTimeOffset);

    /// <inheritdoc />
    public DateTime GetDateTime() => throw Unsupported(SerializationValueKind.DateTime);

    /// <inheritdoc />
    public DateOnly GetDateOnly() => throw Unsupported(SerializationValueKind.DateOnly);

    /// <inheritdoc />
    public TimeOnly GetTimeOnly() => throw Unsupported(SerializationValueKind.TimeOnly);

    /// <summary>
    /// Flattens a Bencode node into the token stream.
    /// </summary>
    /// <param name="node">The node to flatten.</param>
    private void Flatten(BencodeSyntaxNode node)
    {
        switch (node)
        {
            case BencodeIntegerSyntax integer:
                _tokens.Add(new ReaderToken(SerializationValueKind.Int64, integer.Value, integer.Span.Start));
                break;

            case BencodeByteStringSyntax text:
                _tokens.Add(new ReaderToken(SerializationValueKind.String, text.Value, text.Span.Start));
                break;

            case BencodeListSyntax list:
                _tokens.Add(new ReaderToken(SerializationValueKind.StartArray, null, list.Span.Start));
                foreach (BencodeSyntaxNode item in list.Items)
                    Flatten(item);
                _tokens.Add(new ReaderToken(SerializationValueKind.EndArray, null, list.Span.End));
                break;

            case BencodeDictionarySyntax dictionary:
                _tokens.Add(new ReaderToken(SerializationValueKind.StartObject, null, dictionary.Span.Start));
                foreach (BencodeKeyValueSyntax entry in dictionary.Entries)
                {
                    _tokens.Add(new ReaderToken(SerializationValueKind.PropertyName, entry.Key.AsString(), entry.Key.Span.Start));
                    Flatten(entry.Value);
                }

                _tokens.Add(new ReaderToken(SerializationValueKind.EndObject, null, dictionary.Span.End));
                break;
        }
    }

    /// <summary>
    /// Returns the current token's value, asserting it is of the expected kind.
    /// </summary>
    /// <param name="expected">The expected token kind.</param>
    /// <returns>The token value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not of the expected kind.
    /// </exception>
    private object? Require(SerializationValueKind expected) =>
        TokenKind == expected
            ? _tokens[_index].Value
            : throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_TypeMismatch, TokenKind, expected));

    /// <summary>
    /// Returns the current byte-string token's bytes.
    /// </summary>
    /// <returns>The byte content.</returns>
    private ReadOnlyMemory<byte> Bytes() => (ReadOnlyMemory<byte>)Require(SerializationValueKind.String) !;

    /// <summary>
    /// Creates the exception thrown when a converter requests a kind the Bencode grammar cannot represent.
    /// </summary>
    /// <param name="kind">The requested value kind.</param>
    /// <returns>The exception to throw.</returns>
    private NotSupportedException Unsupported(SerializationValueKind kind) =>
        new(string.Format(CultureInfo.CurrentCulture, BencodeSerializationResourceStrings.Op_NotSupported_BencodeValueKind, kind));

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
        /// <param name="offset">The source byte offset of the token.</param>
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
        /// Gets the source byte offset of the token.
        /// </summary>
        /// <returns>The byte offset.</returns>
        internal int Offset { get; }
    }
}
