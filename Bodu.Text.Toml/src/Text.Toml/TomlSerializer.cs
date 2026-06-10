// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Provides static methods for serializing values to canonical TOML text and deserializing TOML back into values,
/// mapping plain CLR objects to and from the format through configurable converters. Mirrors the role of
/// <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
/// <remarks>
/// <para>
/// A TOML document's root is always a table, so the type serialized at the document root must map to an object or a
/// string-keyed dictionary; serializing a top-level scalar or array throws <see cref="TomlSerializationException" />.
/// Scalars map to the corresponding TOML kinds; enumerations to strings; a byte array to an array of integers or a
/// Base64 string per <see cref="TomlSerializerOptions.ByteArrayHandling" />; and TOML has no null, so a null member is
/// omitted by default and a null array element is rejected.
/// </para>
/// <para>
/// Each entry point accepts an optional <see cref="TomlSerializerOptions" />. When none is supplied a default instance
/// is used. Reusing a single configured options object across many calls is the efficient pattern, because resolved
/// converters and type metadata are cached on it.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// string text = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
/// ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(text);
///]]>
/// </example>
public static class TomlSerializer
{
    /// <summary>
    /// The strict UTF-8 encoding used for text and stream input and output; it omits a byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Serializes the specified value to canonical TOML text.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The TOML representation of <paramref name="value" />.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the value does not map to a table at the document root, or cannot be represented in TOML.
    /// </exception>
    public static string Serialize<T>(T value, TomlSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);
        return s_utf8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value as UTF-8 TOML to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The buffer writer that receives the UTF-8 TOML bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the value does not map to a table at the document root, or cannot be represented in TOML.
    /// </exception>
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        TomlSerializerOptions effective = options ?? new TomlSerializerOptions();
        RequireRootIsTable<T>(effective);

        var writer = new Utf8TomlWriter(destination, new TomlWriterOptions { SpecVersion = effective.SpecVersion, MaxDepth = effective.MaxDepth });
        TomlSerializerEngine.Serialize(writer, value, effective);
    }

    /// <summary>
    /// Asynchronously serializes the specified value as UTF-8 TOML to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The stream that receives the UTF-8 TOML bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the write.</param>
    /// <returns>A task that completes when the value has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the value does not map to a table at the document root, or cannot be represented in TOML.
    /// </exception>
    public static ValueTask SerializeAsync<T>(Stream destination, T value, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        TomlThrowHelper.ThrowIfStreamNotWritable(destination);

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);
        return destination.WriteAsync(buffer.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied TOML text.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="text">The TOML source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">Thrown when the text is not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(string text, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Deserialize<T>(s_utf8.GetBytes(text).AsSpan(), options);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied UTF-8 TOML bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="utf8Toml">The UTF-8 TOML bytes to read.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Toml, TomlSerializerOptions? options = null)
    {
        TomlSerializerOptions effective = options ?? new TomlSerializerOptions();

        var reader = new Utf8TomlReader(utf8Toml, new TomlReaderOptions { SpecVersion = effective.SpecVersion, MaxDepth = effective.MaxDepth });
        return TomlSerializerEngine.Deserialize<T>(ref reader, effective);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> by reading a stream of UTF-8 TOML bytes to its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(Stream source, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        TomlThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes a value of type <typeparamref name="T" /> by reading a stream of UTF-8 TOML bytes to
    /// its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the read.</param>
    /// <returns>A task that yields the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        TomlThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Verifies that the document root type maps to a TOML table, throwing when it does not.
    /// </summary>
    /// <typeparam name="T">The document root type.</typeparam>
    /// <param name="options">The serializer options used to classify the type.</param>
    /// <exception cref="TomlSerializationException">
    /// Thrown when <typeparamref name="T" /> does not map to a table at the document root.
    /// </exception>
    private static void RequireRootIsTable<T>(TomlSerializerOptions options)
    {
        if (!options.RootMapsToTable(typeof(T)))
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_RootNotTable, typeof(T)));
        }
    }
}
