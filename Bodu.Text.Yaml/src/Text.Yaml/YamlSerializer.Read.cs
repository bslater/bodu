// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Deserialization for <see cref="YamlSerializer" />.
/// </summary>
/// <remarks>
/// Deserialization consumes the forward-only <see cref="Utf8YamlReader" /> token stream directly rather than a
/// materialized document object model. The reader resolves anchors and aliases, expands merge keys, applies the
/// duplicate-key policy, and enforces the alias-expansion budget during its parse, so the converters observe a fully
/// composed stream and each of these behaviors is inherited unchanged.
/// </remarks>
public static partial class YamlSerializer
{
    /// <summary>
    /// Deserializes YAML text to a value of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="yaml">The YAML source.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    public static T? Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string yaml, YamlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(yaml);
        return Deserialize<T>(Encoding.UTF8.GetBytes(yaml), options);
    }

    /// <summary>
    /// Deserializes UTF-8 YAML to a value of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    public static T? Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ReadOnlySpan<byte> utf8Yaml, YamlSerializerOptions? options = null) =>
        (T?)Deserialize(utf8Yaml, typeof(T), options);

    /// <summary>
    /// Deserializes UTF-8 YAML to a value of the specified type.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <param name="returnType">The target type.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="returnType" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    public static object? Deserialize(
        ReadOnlySpan<byte> utf8Yaml,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type returnType,
        YamlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(returnType);

        YamlSerializerOptions o = options ?? s_defaultOptions;
        o.MakeReadOnly();
        var reader = new Utf8YamlReader(utf8Yaml, new YamlReaderOptions
        {
            SpecVersion = o.SpecVersion,
            DuplicateKeyBehavior = o.DuplicateKeyBehavior,
            MergeKeyBehavior = o.MergeKeyBehavior,
            MaxDepth = o.MaxDepth,
        });

        // An empty document deserializes to the type's default rather than failing, so the converter — which requires
        // a positioned token — is resolved only once the document is known to carry a value.
        if (!reader.Read())
            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

        return o.GetConverter(returnType).ReadAsObject(ref reader, returnType, o);
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> by reading a stream of UTF-8 YAML bytes to its end.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 YAML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The deserialized value.</returns>
    /// <remarks>
    /// The stream is read to its end into an in-memory buffer before parsing begins: the method buffers the complete
    /// input rather than parsing incrementally, so peak memory includes the entire document.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="YamlFormatException">The stream contents are not valid YAML.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    public static T? Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Stream source, YamlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes a value of type <typeparamref name="T" /> by reading a stream of UTF-8 YAML bytes to
    /// its end.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="source">The readable stream containing the UTF-8 YAML bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
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
    /// <exception cref="YamlFormatException">The stream contents are not valid YAML.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    public static async ValueTask<T?> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Stream source, YamlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfStreamNotReadable(source);

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }
}
