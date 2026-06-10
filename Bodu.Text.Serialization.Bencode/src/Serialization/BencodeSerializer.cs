// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Bencode.Syntax;

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Serializes and deserializes objects to and from Bencode, following a <see cref="System.Text.Json.JsonSerializer" />
/// -style API shape. Strings and byte arrays map to byte strings, integers to Bencode integers, lists and arrays to
/// Bencode lists, and objects and dictionaries to Bencode dictionaries with keys in canonical order.
/// </summary>
/// <remarks>
/// Values the grammar cannot represent — Booleans, floating-point numbers, and date-times — are rejected unless a
/// converter maps them to an integer or byte string.
/// </remarks>
/// <example>
///<![CDATA[
/// byte[] payload = BencodeSerializer.Serialize(new TorrentInfo { Name = "ubuntu.iso", Length = 1024 });
/// TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(payload);
///]]>
/// </example>
public static class BencodeSerializer
{
    /// <summary>
    /// Serializes a value to a new Bencode byte array.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The Bencode encoding of <paramref name="value" />.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when a value cannot be represented in Bencode and no converter handles it.
    /// </exception>
    public static byte[] Serialize<T>(T value, BencodeSerializerOptions? options = null)
    {
        BencodeSerializerOptions effective = options ?? new BencodeSerializerOptions();
        var writer = new BencodeWriterAdapter(effective.MaxDepth);
        SerializationEngine.Serialize(writer, value, effective);
        return writer.ToByteArray();
    }

    /// <summary>
    /// Serializes a value and writes the Bencode bytes to the specified buffer writer.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="destination">The destination buffer writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        destination.Write(Serialize(value, options));
    }

    /// <summary>
    /// Asynchronously serializes a value and writes the Bencode bytes to the specified stream.
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
    public static ValueTask SerializeAsync<T>(Stream destination, T value, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        SerializationThrowHelper.ThrowIfStreamNotWritable(destination);

        return destination.WriteAsync(Serialize(value, options), cancellationToken);
    }

    /// <summary>
    /// Deserializes a value from the specified Bencode bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is not a valid Bencode document.
    /// </exception>
    /// <exception cref="FormatSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    public static T Deserialize<T>(ReadOnlySpan<byte> data, BencodeSerializerOptions? options = null)
    {
        BencodeSerializerOptions effective = options ?? new BencodeSerializerOptions();
        BencodeDocumentSyntax document = BencodeSyntaxTree.Parse(data, effective.MaxDepth);
        var reader = new BencodeReaderAdapter(document);
        return SerializationEngine.Deserialize<T>(reader, effective);
    }

    /// <summary>
    /// Deserializes a value from the specified Bencode byte array.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is not a valid Bencode document.
    /// </exception>
    public static T Deserialize<T>(byte[] data, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(data);
        return Deserialize<T>(data.AsSpan(), options);
    }

    /// <summary>
    /// Deserializes a value by reading a stream of Bencode bytes to its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the Bencode bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not a valid Bencode document.</exception>
    public static T Deserialize<T>(Stream source, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        SerializationThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);
        return Deserialize<T>(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes a value by reading a stream of Bencode bytes to its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the Bencode bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not a valid Bencode document.</exception>
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        SerializationThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options);
    }
}
