// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization;

/// <summary>
/// Drives serialization and deserialization between a strongly-typed value and the format-neutral reader and writer,
/// resolving the root converter from the options and delegating to it.
/// </summary>
/// <remarks>
/// This type is the bridge each format facade calls once it has built a reader over its parsed document or a writer
/// over its output. It is format-agnostic; everything format-specific lives in the reader and writer implementations.
/// </remarks>
internal static class SerializationEngine
{
    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the reader.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="reader">The reader over the parsed document.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    internal static T Deserialize<T>(ISerializationReader reader, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();
        FormatConverter converter = options.GetConverter(typeof(T));

        if (!reader.Read())
            throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_UnexpectedEndOfInput, typeof(T)));

        return (T)converter.ReadAsObject(reader, typeof(T), options) !;
    }

    /// <summary>
    /// Serializes a value of type <typeparamref name="T" /> to the writer.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    internal static void Serialize<T>(ISerializationWriter writer, T value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();
        FormatConverter converter = options.GetConverter(typeof(T));
        converter.WriteAsObject(writer, value, options);
    }
}
