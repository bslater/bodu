// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// Provides serialize, deserialize, and round-trip helpers over the in-memory token reader and writer, so the engine
/// and converters can be tested without a concrete text or binary format.
/// </summary>
internal static class FakeFormat
{
    /// <summary>
    /// Serializes a value to a token list.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for defaults.</param>
    /// <returns>The recorded tokens.</returns>
    internal static IReadOnlyList<SerializationToken> Serialize<T>(T value, FormatSerializerOptions? options = null)
    {
        var writer = new FakeSerializationWriter();
        SerializationEngine.Serialize(writer, value, options ?? new FormatSerializerOptions());
        return writer.Tokens;
    }

    /// <summary>
    /// Deserializes a value from a token list.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tokens">The tokens to read.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for defaults.</param>
    /// <returns>The deserialized value.</returns>
    internal static T Deserialize<T>(IReadOnlyList<SerializationToken> tokens, FormatSerializerOptions? options = null)
    {
        var reader = new FakeSerializationReader(tokens);
        return SerializationEngine.Deserialize<T>(reader, options ?? new FormatSerializerOptions());
    }

    /// <summary>
    /// Serializes a value and deserializes it back, using the same options instance for both directions.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to round-trip.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for defaults.</param>
    /// <returns>The value produced by the round trip.</returns>
    internal static T RoundTrip<T>(T value, FormatSerializerOptions? options = null)
    {
        FormatSerializerOptions effective = options ?? new FormatSerializerOptions();
        IReadOnlyList<SerializationToken> tokens = Serialize(value, effective);
        return Deserialize<T>(tokens, effective);
    }
}
