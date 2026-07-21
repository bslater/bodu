// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Provides static methods for serializing values to normalized TOML text and deserializing TOML back into values,
/// mapping plain CLR objects to and from the format through configurable converters.
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
/// TOML has no representation for an object reference, so reference identity is not preserved: a value reachable by
/// more than one path is written once per path, by value, and a reference cycle is rejected with
/// <see cref="TomlSerializationException" /> rather than serialized.
/// </para>
/// <para>
/// Each entry point accepts an optional <see cref="TomlSerializerOptions" />. When none is supplied a default instance
/// is used. Reusing a single configured options object across many calls is the efficient pattern, because resolved
/// converters and type metadata are cached on it.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// string text = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
/// ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(text);
///]]>
/// </code>
/// </example>
public static class TomlSerializer
{
    /// <summary>The strict UTF-8 encoding used for text and stream input and output; it omits a byte-order mark.</summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>The shared options instance used when a caller passes <see langword="null" />, so resolved converters and type metadata are cached across default-options calls instead of being re-resolved per call.</summary>
    private static readonly TomlSerializerOptions s_defaultOptions = new();

    /// <summary>
    /// Serializes the specified value to normalized TOML text.
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
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
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
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        TomlSerializerOptions effective = options ?? s_defaultOptions;
        RequireRootIsTable(value, effective);

        var writer = new Utf8TomlWriter(destination, new TomlWriterOptions { MaxDepth = effective.MaxDepth });
        SerializerEngine.Serialize(writer, value, effective);
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
    /// <remarks>
    /// The value is serialized into an in-memory buffer in full and then written to the stream in a single asynchronous
    /// operation: the method buffers the complete output rather than streaming it, so peak memory includes the entire
    /// rendered document. Cancellation applies to the final write, not to the serialization that precedes it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the value does not map to a table at the document root, or cannot be represented in TOML.
    /// </exception>
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
    public static ValueTask SerializeAsync<T>(Stream destination, T value, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfStreamNotWritable(destination);

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
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
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
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Toml, TomlSerializerOptions? options = null)
    {
        TomlSerializerOptions effective = options ?? s_defaultOptions;

        var reader = new TomlDocumentReader(utf8Toml, new TomlReaderOptions { SpecVersion = effective.SpecVersion, MaxDepth = effective.MaxDepth });
        return SerializerEngine.Deserialize<T>(ref reader, effective);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> by reading a stream of UTF-8 TOML bytes to its end.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <remarks>
    /// The stream is read to its end into an in-memory buffer before parsing begins: the method buffers the complete
    /// input rather than parsing incrementally, so peak memory includes the entire document.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(Stream source, TomlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

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
    /// <remarks>
    /// The stream is copied to an in-memory buffer in full before parsing begins: the method buffers the complete input
    /// rather than parsing incrementally, so peak memory includes the entire document. Cancellation applies to reading
    /// the stream, not to the parse and bind that follow.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, TomlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Verifies that the document root maps to a TOML table, throwing when it does not.
    /// </summary>
    /// <typeparam name="T">The document root type.</typeparam>
    /// <param name="value">The value being serialized at the document root.</param>
    /// <param name="options">The serializer options used to classify the type.</param>
    /// <exception cref="TomlSerializationException">
    /// Thrown when <typeparamref name="T" /> does not map to a table at the document root.
    /// </exception>
    /// <remarks>
    /// A document object model node passes the static type gate as a <see cref="Nodes.TomlNode" />, so its actual kind
    /// is checked here against the runtime value: a node whose root is a scalar or array is rejected, because a TOML
    /// document has no top-level scalar or array form.
    /// </remarks>
    private static void RequireRootIsTable<T>(T value, TomlSerializerOptions options)
    {
        if (!options.RootMapsToTable(typeof(T)))
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_RootNotTable, typeof(T)));
        }

        if (value is Nodes.TomlNode node && node.GetValueKind() != TomlValueKind.Table)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_RootNotTable, node.GetType()));
        }
    }
}
