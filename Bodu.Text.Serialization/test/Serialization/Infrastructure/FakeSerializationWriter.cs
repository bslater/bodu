// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FakeSerializationWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// An in-memory <see cref="ISerializationWriter" /> that records the tokens written to it, used to exercise the engine
/// and converters without a real format.
/// </summary>
internal sealed class FakeSerializationWriter
    : ISerializationWriter
{
    /// <summary>
    /// The captured tokens, in write order.
    /// </summary>
    private readonly List<SerializationToken> _tokens = [];

    /// <summary>
    /// Gets the captured tokens, in write order.
    /// </summary>
    /// <returns>The recorded tokens.</returns>
    internal IReadOnlyList<SerializationToken> Tokens => _tokens;

    /// <inheritdoc />
    public void WriteStartObject() => _tokens.Add(new SerializationToken(SerializationValueKind.StartObject, null));

    /// <inheritdoc />
    public void WriteEndObject() => _tokens.Add(new SerializationToken(SerializationValueKind.EndObject, null));

    /// <inheritdoc />
    public void WriteStartArray() => _tokens.Add(new SerializationToken(SerializationValueKind.StartArray, null));

    /// <inheritdoc />
    public void WriteEndArray() => _tokens.Add(new SerializationToken(SerializationValueKind.EndArray, null));

    /// <inheritdoc />
    public void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        _tokens.Add(new SerializationToken(SerializationValueKind.PropertyName, name));
    }

    /// <inheritdoc />
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        _tokens.Add(new SerializationToken(SerializationValueKind.String, value));
    }

    /// <inheritdoc />
    public void WriteBytes(ReadOnlySpan<byte> value) => _tokens.Add(new SerializationToken(SerializationValueKind.Bytes, value.ToArray()));

    /// <inheritdoc />
    public void WriteInt64(long value) => _tokens.Add(new SerializationToken(SerializationValueKind.Int64, value));

    /// <inheritdoc />
    public void WriteUInt64(ulong value) => _tokens.Add(new SerializationToken(SerializationValueKind.UInt64, value));

    /// <inheritdoc />
    public void WriteDouble(double value) => _tokens.Add(new SerializationToken(SerializationValueKind.Double, value));

    /// <inheritdoc />
    public void WriteBoolean(bool value) => _tokens.Add(new SerializationToken(SerializationValueKind.Boolean, value));

    /// <inheritdoc />
    public void WriteNull() => _tokens.Add(new SerializationToken(SerializationValueKind.Null, null));

    /// <inheritdoc />
    public void WriteDateTimeOffset(DateTimeOffset value) => _tokens.Add(new SerializationToken(SerializationValueKind.DateTimeOffset, value));

    /// <inheritdoc />
    public void WriteDateTime(DateTime value) => _tokens.Add(new SerializationToken(SerializationValueKind.DateTime, value));

    /// <inheritdoc />
    public void WriteDateOnly(DateOnly value) => _tokens.Add(new SerializationToken(SerializationValueKind.DateOnly, value));

    /// <inheritdoc />
    public void WriteTimeOnly(TimeOnly value) => _tokens.Add(new SerializationToken(SerializationValueKind.TimeOnly, value));
}
