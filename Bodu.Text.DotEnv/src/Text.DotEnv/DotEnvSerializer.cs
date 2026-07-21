// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Bodu.Text.DotEnv.Reader;
using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides methods for serializing .NET objects to DotEnv text and deserializing DotEnv text into .NET objects, shaped
/// after <c>System.Text.Json</c>'s <c>JsonSerializer</c>.
/// </summary>
/// <remarks>
/// <para>
/// DotEnv models a flat, ordered object of string-valued keys, so the serializer maps a POCO (or an
/// <see cref="IDictionary{TKey, TValue}" /> keyed by <see cref="string" />) whose members are strings or types
/// convertible from a string. Property names honour <see cref="DotEnvSerializerOptions.PropertyNamingPolicy" /> and the
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> family; the callback interfaces (<see cref="Bodu.Text.Serialization.IOnSerializing" />
/// and its siblings) fire around the mapping.
/// </para>
/// <para>
/// The asynchronous stream overloads buffer the entire document rather than streaming it; only the stream copy is
/// asynchronous.
/// </para>
/// </remarks>
public static partial class DotEnvSerializer
{
    /// <summary>
    /// Serializes the specified value to DotEnv text.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The DotEnv text.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static string Serialize<T>(T value, DotEnvSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value as DotEnv to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="destination">The buffer writer that receives the DotEnv bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, DotEnvSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        DotEnvSerializerOptions effective = options ?? DotEnvSerializerOptions.Default;
        effective.MakeReadOnly();

        var writer = new Utf8DotEnvWriter(destination, new DotEnvWriterOptions { WriteExportPrefix = effective.WriteExportPrefix });
        WriteValue(ref writer, value, typeof(T), effective);
        writer.Flush();
    }

    /// <summary>
    /// Asynchronously serializes the specified value as DotEnv to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the final stream write.</param>
    /// <returns>A task that completes when the DotEnv has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static ValueTask SerializeAsync<T>(Stream destination, T value, DotEnvSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return destination.WriteAsync(buffer.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// Deserializes the specified DotEnv text into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="text">The DotEnv source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvFormatException">Thrown when the text is not valid DotEnv.</exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(string text, DotEnvSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Deserialize<T>(Encoding.UTF8.GetBytes(text).AsSpan(), options);
    }

    /// <summary>
    /// Deserializes the specified UTF-8 DotEnv bytes into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8DotEnv, DotEnvSerializerOptions? options = null)
    {
        DotEnvSerializerOptions effective = options ?? DotEnvSerializerOptions.Default;
        effective.MakeReadOnly();

        List<KeyValuePair<string, string>> entries = ReadEntries(utf8DotEnv);

        return (T)ReadValue(entries, typeof(T), effective) !;
    }

    /// <summary>
    /// Deserializes the DotEnv content of the supplied stream into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvFormatException">Thrown when the content is not valid DotEnv.</exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(Stream source, DotEnvSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes the DotEnv content of the supplied stream into a value of type
    /// <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the stream copy.</param>
    /// <returns>A task that yields the deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvFormatException">Thrown when the content is not valid DotEnv.</exception>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, DotEnvSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Reads all key/value entries from the supplied DotEnv bytes in source order.
    /// </summary>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <returns>The decoded entries.</returns>
    private static List<KeyValuePair<string, string>> ReadEntries(ReadOnlySpan<byte> utf8DotEnv)
    {
        var reader = new Utf8DotEnvReader(utf8DotEnv);
        var entries = new List<KeyValuePair<string, string>>();
        string? pendingKey = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DotEnvTokenType.PropertyName:
                    pendingKey = reader.GetString();
                    break;

                case DotEnvTokenType.String:
                    entries.Add(new KeyValuePair<string, string>(pendingKey!, reader.GetString()));
                    pendingKey = null;
                    break;

                default:
                    break;
            }
        }

        return entries;
    }
}
