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
/// scalar-convertible members become global keys and whose object-shaped members - section POCOs or
/// <see cref="IDictionary{TKey, TValue}" /> values keyed by <see cref="string" /> - become sections. A dictionary,
/// whether the root or a section, that implements only that generic interface, and not the non-generic
/// <see cref="System.Collections.IDictionary" />, is written and read through it, as a
/// <see cref="Dictionary{TKey, TValue}" /> is. A dictionary whose type is not keyed by <see cref="string" />, such as a
/// <see cref="System.Collections.Hashtable" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />,
/// is a collection rather than an object or a section, and is rejected with <see cref="IniSerializationException" />,
/// as the root or as a section, when written and when read. A dictionary entry that is not a section is one key, so its
/// value must be <see langword="null" /> or a scalar, whatever the dictionary's value type: any other value, such as a
/// list or a class with members of its own, is rejected with <see cref="IniSerializationException" /> naming the
/// section and the key, rather than written as its <see cref="object.ToString" /> text, which does not read back. A
/// <see langword="null" /> entry of a dictionary root, which the ignore condition writes only when it is
/// <see cref="Bodu.Text.Serialization.IgnoreCondition.Never" />, has no value to decide by, so the dictionary's value
/// type decides: a string-keyed dictionary type writes it as an empty section, a scalar or <see cref="object" /> as an
/// empty key, and any other type, which no empty key reads back as, is rejected. A <see langword="null" /> entry of a
/// dictionary written as a section, the reserved global section included, is decided in the same way, except that a
/// string-keyed dictionary type, a third level, is rejected. Property names honor
/// <see cref="IniSerializerOptions.PropertyNamingPolicy" /> and the
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> family; the callback interfaces (<see cref="Bodu.Text.Serialization.IOnSerializing" />
/// and its siblings) fire around the mapping. Each member name maps to one key or section, its most derived
/// declaration: a property or field hidden with the <see langword="new" /> modifier is neither written nor read. A type
/// that maps two members to one name is rejected with <see cref="IniSerializationException" /> before anything is read
/// or written. A member declared with the C# <see langword="required" /> keyword is required, as one marked
/// <see cref="Bodu.Text.Serialization.RequiredAttribute" /> is: reading a document without its key or section throws.
/// </para>
/// <para>
/// A POCO root's type is checked whole, once per type and options instance, before the input is read or anything is
/// written. A member declared as <see cref="object" />, which could be a key or a section, a member whose type is a
/// collection other than a string-keyed dictionary, and a member of a section type that is not a scalar, which would
/// nest beyond the second level, are each rejected with <see cref="IniSerializationException" /> naming the member. The
/// check is on the type's shape, not the instance: it covers every mapped member, whether it can be read, set or both,
/// and the declared type of every section, whether or not the instance holds one, so a type is rejected in both
/// directions or in neither, and reading rejects it whether or not the document has the section. A member left out with
/// <see cref="Bodu.Text.Serialization.IgnoreAttribute" /> is not checked. A string-keyed dictionary, as the root or as
/// a section, is checked by its entries' values instead, as they are written.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document, including when the type of
    /// <paramref name="value" /> has a member INI cannot represent, which is reported before anything is written, when
    /// a dictionary entry that is not a section holds a value that is not a scalar, or when <paramref name="value" />
    /// maps to a key, section name or value that <see cref="Utf8IniWriter" /> refuses because it would not read back
    /// unchanged; the message then names the section and key, and the writer's <see cref="ArgumentException" /> is the
    /// inner exception.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document, including when the type of
    /// <paramref name="value" /> has a member INI cannot represent, which is reported before anything is written, when
    /// a dictionary entry that is not a section holds a value that is not a scalar, or when <paramref name="value" />
    /// maps to a key, section name or value that <see cref="Utf8IniWriter" /> refuses because it would not read back
    /// unchanged; the message then names the section and key, and the writer's <see cref="ArgumentException" /> is the
    /// inner exception.
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
    /// Thrown when <typeparamref name="T" /> cannot be mapped to an INI document, including when the type of
    /// <paramref name="value" /> has a member INI cannot represent, which is reported before anything is written, when
    /// a dictionary entry that is not a section holds a value that is not a scalar, or when <paramref name="value" />
    /// maps to a key, section name or value that <see cref="Utf8IniWriter" /> refuses because it would not read back
    /// unchanged; the message then names the section and key, and the writer's <see cref="ArgumentException" /> is the
    /// inner exception.
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />,
    /// or the type of a section it is read into, is abstract or has no public parameterless constructor, or when
    /// <typeparamref name="T" /> has a member INI cannot represent, which is reported before the input is read.
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />,
    /// or the type of a section it is read into, is abstract or has no public parameterless constructor, or when
    /// <typeparamref name="T" /> has a member INI cannot represent, which is reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Ini, IniSerializerOptions? options = null)
    {
        IniSerializerOptions effective = PrepareRead(typeof(T), options);

        IniNormalizedDocument document = IniNormalizedDocument.Parse(utf8Ini, IniReaderOptions.Default, effective.ToDocumentOptions());

        return (T)ReadValue(document, typeof(T), effective)!;
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />,
    /// or the type of a section it is read into, is abstract or has no public parameterless constructor, or when
    /// <typeparamref name="T" /> has a member INI cannot represent, which is reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(Stream source, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);

        IniSerializerOptions effective = PrepareRead(typeof(T), options);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), effective);
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
    /// Thrown when the document cannot be mapped to <typeparamref name="T" />, including when <typeparamref name="T" />,
    /// or the type of a section it is read into, is abstract or has no public parameterless constructor, or when
    /// <typeparamref name="T" /> has a member INI cannot represent, which is reported before the input is read.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<T> DeserializeAsync<T>(Stream source, IniSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        IniSerializerOptions effective = PrepareRead(typeof(T), options);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), effective);
    }

    /// <summary>
    /// Resolves the options a read uses and checks the target type's shape, before any input is read.
    /// </summary>
    /// <param name="targetType">The type to read the document into.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The read-only options to read with.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when <paramref name="targetType" /> cannot hold an INI document: a scalar or a collection other than a
    /// string-keyed dictionary, a type that maps two members to one name, or one with a member INI cannot represent.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static IniSerializerOptions PrepareRead(Type targetType, IniSerializerOptions? options)
    {
        IniSerializerOptions effective = options ?? IniSerializerOptions.Default;
        effective.MakeReadOnly();
        ThrowIfRootNotSupported(targetType, effective);

        return effective;
    }
}
