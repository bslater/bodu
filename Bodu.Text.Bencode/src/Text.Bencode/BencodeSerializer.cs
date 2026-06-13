// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
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
        BencodeSerializerOptions effective = options ?? new BencodeSerializerOptions();

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = effective.MaxDepth });
        BencodeSerializerEngine.Serialize(writer, value, effective);
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
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        destination.Write(Serialize(value, options));
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
        BencodeThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = Serialize(value, options);
        destination.Write(bytes, 0, bytes.Length);
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
    public static BencodeNode? SerializeToNode<T>(T value, BencodeSerializerOptions? options = null) =>
        BencodeNode.Parse(Serialize(value, options));

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
    public static BencodeDocument SerializeToDocument<T>(T value, BencodeSerializerOptions? options = null) =>
        BencodeDocument.Parse(Serialize(value, options));

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

        return Deserialize<T>(node.ToByteArray(), options);
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
    public static ValueTask SerializeAsync<T>(Stream destination, T value, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        BencodeThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = Serialize(value, options);
        return destination.WriteAsync(bytes, cancellationToken);
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
        BencodeSerializerOptions effective = options ?? new BencodeSerializerOptions();

        var reader = new Utf8BencodeReader(data, new BencodeReaderOptions
        {
            MaxDepth = effective.MaxDepth,
            AllowUnsortedKeys = effective.AllowUnsortedKeys,
            AllowDuplicateKeys = effective.AllowDuplicateKeys,
        });
        return BencodeSerializerEngine.Deserialize<T>(ref reader, effective);
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
    public static T Deserialize<T>(Stream source, BencodeSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        BencodeThrowHelper.ThrowIfStreamNotReadable(source);

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
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, BencodeSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        BencodeThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }
}
