// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Serializes and deserializes objects to and from TOML, following a <see cref="System.Text.Json.JsonSerializer" />
/// -style API shape. The root value must be a table, so the type serialized at the document root must map to an object.
/// </summary>
/// <remarks>
/// Scalars map to the corresponding TOML kinds; enumerations to strings; byte arrays to base64 strings; and TOML has no
/// null, so a null member is omitted by default and a null array element is rejected.
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
    /// The strict UTF-8 encoding used for stream input and output.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Serializes a value to TOML text.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The TOML representation of <paramref name="value" />.</returns>
    /// <exception cref="FormatSerializationException">
    /// Thrown when the value does not map to a table at the document root.
    /// </exception>
    public static string Serialize<T>(T value, TomlSerializerOptions? options = null)
    {
        TomlSerializerOptions effective = options ?? new TomlSerializerOptions();
        var writer = new TomlWriterAdapter();
        SerializationEngine.Serialize(writer, value, effective);
        return TomlTextWriter.Write(writer.GetRootTable());
    }

    /// <summary>
    /// Serializes a value and writes the TOML text to the specified writer.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="destination">The destination text writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    public static void Serialize<T>(TextWriter destination, T value, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        destination.Write(Serialize(value, options));
    }

    /// <summary>
    /// Serializes a value and writes the TOML text to the specified stream as UTF-8.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    public static void Serialize<T>(Stream destination, T value, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        SerializationThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = s_utf8.GetBytes(Serialize(value, options));
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Asynchronously serializes a value and writes the TOML text to the specified stream as UTF-8.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the write.</param>
    /// <returns>A task that completes once the bytes have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    public static ValueTask SerializeAsync<T>(Stream destination, T value, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        SerializationThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = s_utf8.GetBytes(Serialize(value, options));
        return destination.WriteAsync(bytes.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Deserializes a value from TOML text.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="text">The TOML source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="text" /> is not a valid TOML document.
    /// </exception>
    /// <exception cref="FormatSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(string text, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);
        return Deserialize<T>(text.AsSpan(), options);
    }

    /// <summary>
    /// Deserializes a value from the specified TOML character span.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="text">The TOML source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="text" /> is not a valid TOML document.
    /// </exception>
    /// <exception cref="FormatSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(ReadOnlySpan<char> text, TomlSerializerOptions? options = null)
    {
        TomlSerializerOptions effective = options ?? new TomlSerializerOptions();
        TomlDocumentSyntax document = TomlSyntaxTree.Parse(text, effective.SpecVersion);
        var reader = new TomlReaderAdapter(document);
        return SerializationEngine.Deserialize<T>(reader, effective);
    }

    /// <summary>
    /// Deserializes a value by reading a stream of UTF-8 TOML text to its end.
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
    public static T Deserialize<T>(Stream source, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        SerializationThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);
        return Deserialize<T>(Decode(buffer).AsSpan(), options);
    }

    /// <summary>
    /// Asynchronously deserializes a value by reading a stream of UTF-8 TOML text to its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        SerializationThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(Decode(buffer).AsSpan(), options);
    }

    /// <summary>
    /// Decodes the contents of a buffer as UTF-8 text.
    /// </summary>
    /// <param name="buffer">The buffered bytes.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not valid UTF-8.</exception>
    private static string Decode(MemoryStream buffer)
    {
        ReadOnlySpan<byte> bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        try
        {
            return s_utf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new TomlFormatException(TomlSerializationResourceStrings.Format_Invalid_TomlInvalidUtf8, ex);
        }
    }
}
