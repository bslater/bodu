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
/// convertible from a string. A dictionary that implements only that generic interface, and not the non-generic
/// <see cref="System.Collections.IDictionary" />, is written and read through it, as a
/// <see cref="Dictionary{TKey, TValue}" /> is. Each member or entry is one value, so its type must be a scalar: a
/// string or character, a number, a <see cref="bool" />, an enumeration, a date, time or time span, a
/// <see cref="Guid" /> or a <see cref="Uri" />, or <see cref="object" /> holding one. A type with a member of any other
/// type, such as a dictionary or a class with members of its own, whether the member is readable, writable or both, is
/// rejected with <see cref="DotEnvSerializationException" /> by <c>Serialize</c> and <c>Deserialize</c> alike, before
/// anything is written or read, whatever the member holds and whatever the ignore condition; a dictionary entry of such
/// a type, and a member declared as <see cref="object" /> holding such a value, are rejected when they are written.
/// Neither is written as the value's <see cref="object.ToString" /> text, which does not read back. A dictionary whose
/// type is not keyed by <see cref="string" />, such as a <see cref="System.Collections.Hashtable" /> or a
/// <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />, is a collection rather than an object, and is
/// rejected with <see cref="DotEnvSerializationException" /> when written and when read. Property names honor
/// <see cref="DotEnvSerializerOptions.PropertyNamingPolicy" /> and the
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> family; the callback interfaces (<see cref="Bodu.Text.Serialization.IOnSerializing" />
/// and its siblings) fire around the mapping. Each member name maps to one key, its most derived declaration: a
/// property or field hidden with the <see langword="new" /> modifier is neither written nor read. A type that maps two
/// members to one key is rejected with <see cref="DotEnvSerializationException" /> before anything is read or written.
/// A member declared with the C# <see langword="required" /> keyword is required, as one marked
/// <see cref="Bodu.Text.Serialization.RequiredAttribute" /> is: reading a document without its key throws.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object, including when a member's type is not
    /// a scalar; when a dictionary entry that is written is not a scalar, or a member declared as <see cref="object" />
    /// holds a value that is not, and the message then names the key; or when a member name or dictionary key is not a
    /// key the reader reads back (one matching <c>[A-Za-z_][A-Za-z0-9_]*</c>), and the message then names the key, with
    /// the writer's <see cref="ArgumentException" /> as the inner exception.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object, including when a member's type is not
    /// a scalar; when a dictionary entry that is written is not a scalar, or a member declared as <see cref="object" />
    /// holds a value that is not, and the message then names the key; or when a member name or dictionary key is not a
    /// key the reader reads back (one matching <c>[A-Za-z_][A-Za-z0-9_]*</c>), and the message then names the key, with
    /// the writer's <see cref="ArgumentException" /> as the inner exception.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to a DotEnv object, including when a member's type is not
    /// a scalar; when a dictionary entry that is written is not a scalar, or a member declared as <see cref="object" />
    /// holds a value that is not, and the message then names the key; or when a member name or dictionary key is not a
    /// key the reader reads back (one matching <c>[A-Za-z_][A-Za-z0-9_]*</c>), and the message then names the key, with
    /// the writer's <see cref="ArgumentException" /> as the inner exception.
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />
    /// is abstract or has no public parameterless constructor, or has a member whose type is not a scalar, which is
    /// reported before the input is read.
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />
    /// is abstract or has no public parameterless constructor, or has a member whose type is not a scalar, which is
    /// reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8DotEnv, DotEnvSerializerOptions? options = null)
    {
        DotEnvSerializerOptions effective = PrepareRead(typeof(T), options);

        List<KeyValuePair<string, string>> entries = ReadEntries(utf8DotEnv);

        return (T)ReadValue(entries, typeof(T), effective)!;
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />
    /// is abstract or has no public parameterless constructor, or has a member whose type is not a scalar, which is
    /// reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(Stream source, DotEnvSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);

        DotEnvSerializerOptions effective = PrepareRead(typeof(T), options);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), effective);
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />
    /// is abstract or has no public parameterless constructor, or has a member whose type is not a scalar, which is
    /// reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, DotEnvSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        DotEnvSerializerOptions effective = PrepareRead(typeof(T), options);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), effective);
    }

    /// <summary>
    /// Resolves the options a read uses and checks the target type, before any input is read.
    /// </summary>
    /// <param name="targetType">The type to read the document into.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The read-only options to read with.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <paramref name="targetType" /> cannot hold a DotEnv document: a scalar or a collection, a type that
    /// maps two members to one key, or one with a member whose type is not a scalar.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static DotEnvSerializerOptions PrepareRead(Type targetType, DotEnvSerializerOptions? options)
    {
        DotEnvSerializerOptions effective = options ?? DotEnvSerializerOptions.Default;
        effective.MakeReadOnly();
        ThrowIfNotReadable(targetType, effective);

        return effective;
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
