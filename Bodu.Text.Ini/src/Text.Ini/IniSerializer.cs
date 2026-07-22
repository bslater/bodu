// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini;

/// <summary>
/// Provides methods for serializing .NET objects to INI text and deserializing INI text into .NET objects, shaped after
/// <c>System.Text.Json</c>'s <c>JsonSerializer</c>.
/// </summary>
/// <remarks>
/// <para>
/// INI models a two-level object-of-objects, so the serializer maps a root POCO (or string-keyed dictionary) whose
/// scalar-convertible members become global keys and whose object-shaped members — section POCOs or
/// <see cref="IDictionary{TKey, TValue}" /> values keyed by <see cref="string" /> — become sections. A member nested
/// beyond the second level is rejected. Property names honour <see cref="IniSerializerOptions.PropertyNamingPolicy" />
/// and the <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> family; the callback interfaces (<see cref="Bodu.Text.Serialization.IOnSerializing" />
/// and its siblings) fire around the mapping.
/// </para>
/// <para>
/// Deserialization always routes through the normalized document model, applying the options' duplicate-section and
/// duplicate-key policies, because duplicate-section merge declares structure out of source order. The asynchronous
/// stream overloads buffer the entire document rather than streaming it; only the stream copy is asynchronous.
/// </para>
/// </remarks>
public static partial class IniSerializer
{
    /// <summary>
    /// Serializes the specified value to INI text.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The INI text.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static string Serialize<T>(T value, IniSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value as INI to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="destination">The buffer writer that receives the INI bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        IniSerializerOptions effective = options ?? IniSerializerOptions.Default;
        effective.MakeReadOnly();

        var writer = new Utf8IniWriter(destination);
        WriteValue(ref writer, value, typeof(T), effective);
        writer.Flush();
    }

    /// <summary>
    /// Asynchronously serializes the specified value as INI to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the final stream write.</param>
    /// <returns>A task that completes when the INI has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static ValueTask SerializeAsync<T>(Stream destination, T value, IniSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return destination.WriteAsync(buffer.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// Deserializes the specified INI text into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="text">The INI source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">Thrown when the text is not valid INI.</exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(string text, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Deserialize<T>(Encoding.UTF8.GetBytes(text).AsSpan(), options);
    }

    /// <summary>
    /// Deserializes the specified UTF-8 INI bytes into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Ini, IniSerializerOptions? options = null)
    {
        IniSerializerOptions effective = options ?? IniSerializerOptions.Default;
        effective.MakeReadOnly();

        IniNormalizedDocument document = IniNormalizedDocument.Parse(utf8Ini, IniReaderOptions.Default, effective.ToDocumentOptions());

        return (T)ReadValue(document, typeof(T), effective) !;
    }

    /// <summary>
    /// Deserializes the INI content of the supplied stream into a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">Thrown when the content is not valid INI.</exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(Stream source, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes the INI content of the supplied stream into a value of type
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
    /// <exception cref="IniFormatException">Thrown when the content is not valid INI.</exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, IniSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }
}
