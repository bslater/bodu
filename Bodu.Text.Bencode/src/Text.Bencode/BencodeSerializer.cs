// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Provides static methods for serializing values to Bencode (BEP 3) bytes and deserializing Bencode bytes back into
/// values, mapping plain CLR objects to and from the format through configurable converters.
/// </summary>
/// <remarks>
/// <para>
/// Each entry point accepts an optional <see cref="BencodeSerializerOptions" /> that controls converters, the property
/// naming policy, and the maximum nesting depth. When no options are supplied a default instance is used. Reusing a
/// single configured options object across many calls is the efficient pattern, because resolved converters and type
/// metadata are cached on it.
/// </para>
/// <para>
/// Output is always canonical Bencode: dictionary entries are emitted in ascending bytewise key order regardless of the
/// declaration order of the corresponding members.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] bytes = BencodeSerializer.Serialize(new Torrent { Name = "demo", PieceLength = 262144 });
/// Torrent torrent = BencodeSerializer.Deserialize<Torrent>(bytes);
///]]>
/// </code>
/// </example>
public static class BencodeSerializer
{
    /// <summary>
    /// The shared options instance used when a caller passes <see langword="null" />, so resolved converters and type
    /// metadata are cached across default-options calls instead of being re-resolved per call.
    /// </summary>
    private static readonly BencodeSerializerOptions s_defaultOptions = new();

    /// <summary>
    /// Serializes the specified value to a new Bencode byte array.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The Bencode encoding of <paramref name="value" />.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when a value cannot be represented in Bencode.
    /// </exception>
    public static byte[] Serialize<T>(T value, BencodeSerializerOptions? options = null)
    {
        using var buffer = new PooledBufferBuilder<byte>();
        SerializeCore(buffer, value, options ?? s_defaultOptions);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes the specified value as Bencode to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The buffer writer that receives the Bencode bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when a value cannot be represented in Bencode.
    /// </exception>
    /// <remarks>
    /// The value is written through the buffer writer directly, with no intermediate array. Dictionary content is
    /// buffered internally until each dictionary closes (canonical key ordering requires it), so a serialization
    /// failure part-way through a root-level list may leave that list's already-emitted bytes in the destination.
    /// </remarks>
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        SerializeCore(destination, value, options ?? s_defaultOptions);
    }

    /// <summary>
    /// Serializes the specified value as Bencode to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The stream that receives the Bencode bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when a value cannot be represented in Bencode.
    /// </exception>
    public static void Serialize<T>(Stream destination, T value, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfStreamNotWritable(destination);

        using var buffer = new PooledBufferBuilder<byte>();
        SerializeCore(buffer, value, options ?? s_defaultOptions);
        destination.Write(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value into a mutable <see cref="BencodeNode" /> tree.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The root node of the serialized value.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when a value cannot be represented in Bencode.
    /// </exception>
    public static BencodeNode? SerializeToNode<T>(T value, BencodeSerializerOptions? options = null)
    {
        using var buffer = new PooledBufferBuilder<byte>();
        SerializeCore(buffer, value, options ?? s_defaultOptions);
        return BencodeNode.Parse(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value into a read-only <see cref="BencodeDocument" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>A document over the serialized value; dispose it when finished.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when a value cannot be represented in Bencode.
    /// </exception>
    public static BencodeDocument SerializeToDocument<T>(T value, BencodeSerializerOptions? options = null)
    {
        using var buffer = new PooledBufferBuilder<byte>();
        SerializeCore(buffer, value, options ?? s_defaultOptions);
        return BencodeDocument.Parse(buffer.WrittenSpan);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied node tree.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="node">The node tree to bind.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the node tree cannot be bound to <typeparamref name="T" />, or contains a <see langword="null" />
    /// entry, which has no Bencode representation.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    public static T Deserialize<T>(BencodeNode node, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(node);

        using var buffer = new PooledBufferBuilder<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        node.WriteTo(writer);
        return Deserialize<T>(buffer.WrittenSpan, options);
    }

    /// <summary>
    /// Asynchronously serializes the specified value as Bencode to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The stream that receives the Bencode bytes.</param>
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
    public static async ValueTask SerializeAsync<T>(Stream destination, T value, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfStreamNotWritable(destination);

        using var buffer = new PooledBufferBuilder<byte>();
        SerializeCore(buffer, value, options ?? s_defaultOptions);
        await destination.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes the specified value as Bencode into the supplied buffer writer using resolved options.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="destination">The buffer writer that receives the Bencode bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="effective">The resolved serializer options.</param>
    private static void SerializeCore<T>(IBufferWriter<byte> destination, T value, BencodeSerializerOptions effective)
    {
        var writer = new Utf8BencodeWriter(destination, new BencodeWriterOptions { MaxDepth = effective.MaxDepth });
        SerializerEngine.Serialize(writer, value, effective);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied Bencode bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The Bencode bytes to read.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    public static T Deserialize<T>(ReadOnlySpan<byte> data, BencodeSerializerOptions? options = null)
    {
        BencodeSerializerOptions effective = options ?? s_defaultOptions;

        var reader = new Utf8BencodeReader(data, new BencodeReaderOptions
        {
            MaxDepth = effective.MaxDepth,
            AllowUnsortedKeys = effective.AllowUnsortedKeys,
            AllowDuplicateKeys = effective.AllowDuplicateKeys,
        });
        return SerializerEngine.Deserialize<T>(ref reader, effective);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied Bencode byte array.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The Bencode bytes to read.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no converter is configured for a type that is encountered.
    /// </exception>
    public static T Deserialize<T>(byte[] data, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(data);

        return Deserialize<T>(data.AsSpan(), options);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The stream to read the Bencode bytes from.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// The stream is buffered in full before parsing — the span-based reader requires the complete document in
    /// memory — so the stream's length is bounded by the 2 GiB managed-array ceiling.
    /// </remarks>
    public static T Deserialize<T>(Stream source, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes a value of type <typeparamref name="T" /> from the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The stream to read the Bencode bytes from.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the read.</param>
    /// <returns>A task that yields the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// The stream is buffered in full before parsing — the span-based reader requires the complete document in
    /// memory — so only the buffering copy is asynchronous; parsing and binding run synchronously once the copy
    /// completes.
    /// </remarks>
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }
}
