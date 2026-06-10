// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FakeSerializationReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// An in-memory <see cref="ISerializationReader" /> that replays a recorded token list, used to exercise the engine and
/// converters without a real format.
/// </summary>
internal sealed class FakeSerializationReader
    : ISerializationReader
{
    /// <summary>
    /// The tokens being replayed.
    /// </summary>
    private readonly IReadOnlyList<SerializationToken> _tokens;

    /// <summary>
    /// The index of the current token, or -1 before the first <see cref="Read" />.
    /// </summary>
    private int _index = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeSerializationReader" /> class.
    /// </summary>
    /// <param name="tokens">The tokens to replay.</param>
    internal FakeSerializationReader(IReadOnlyList<SerializationToken> tokens)
    {
        ThrowHelper.ThrowIfNull(tokens);
        _tokens = tokens;
    }

    /// <inheritdoc />
    public SerializationValueKind TokenKind =>
        _index >= 0 && _index < _tokens.Count ? _tokens[_index].Kind : SerializationValueKind.None;

    /// <inheritdoc />
    public TextLocation Location => new(_index < 0 ? 0 : _index);

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
        switch (TokenKind)
        {
            case SerializationValueKind.StartObject:
            case SerializationValueKind.StartArray:
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

                break;
        }
    }

    /// <inheritdoc />
    public string GetPropertyName() => (string)Current(SerializationValueKind.PropertyName)!;

    /// <inheritdoc />
    public string GetString() => (string)Current(SerializationValueKind.String)!;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetBytes() => (byte[])Current(SerializationValueKind.Bytes)!;

    /// <inheritdoc />
    public long GetInt64() => (long)Current(SerializationValueKind.Int64)!;

    /// <inheritdoc />
    public ulong GetUInt64() => (ulong)Current(SerializationValueKind.UInt64)!;

    /// <inheritdoc />
    public double GetDouble() => (double)Current(SerializationValueKind.Double)!;

    /// <inheritdoc />
    public bool GetBoolean() => (bool)Current(SerializationValueKind.Boolean)!;

    /// <inheritdoc />
    public DateTimeOffset GetDateTimeOffset() => (DateTimeOffset)Current(SerializationValueKind.DateTimeOffset)!;

    /// <inheritdoc />
    public DateTime GetDateTime() => (DateTime)Current(SerializationValueKind.DateTime)!;

    /// <inheritdoc />
    public DateOnly GetDateOnly() => (DateOnly)Current(SerializationValueKind.DateOnly)!;

    /// <inheritdoc />
    public TimeOnly GetTimeOnly() => (TimeOnly)Current(SerializationValueKind.TimeOnly)!;

    /// <summary>
    /// Returns the value of the current token, asserting it is of the expected kind.
    /// </summary>
    /// <param name="expected">The expected token kind.</param>
    /// <returns>The current token value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not of the expected kind.
    /// </exception>
    private object? Current(SerializationValueKind expected)
    {
        if (TokenKind != expected)
            throw new InvalidOperationException($"Expected {expected} but found {TokenKind}.");

        return _tokens[_index].Value;
    }
}
