// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Provides static methods to serialize objects to YAML and deserialize YAML to objects, in the manner of
/// <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
/// <remarks>
/// The serializer maps public properties (and optionally fields) of plain CLR types, the built-in scalar and collection
/// types, dictionaries keyed by string, and enumerations. Mapping is driven by converters resolved through
/// <see cref="YamlSerializerOptions.GetConverter(Type)" /> over the shared serialization metadata; custom behaviour is
/// supplied through <see cref="YamlConverter{T}" /> instances registered on <see cref="YamlSerializerOptions" />.
/// </remarks>
public static partial class YamlSerializer
{
    /// <summary>The shared default options used when a caller passes <see langword="null" />.</summary>
    private static readonly YamlSerializerOptions s_defaultOptions = new();

    /// <summary>
    /// Serializes a value to a YAML string.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The YAML representation of <paramref name="value" />.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    public static string Serialize<T>(T value, YamlSerializerOptions? options = null) =>
        Serialize(value, typeof(T), options);

    /// <summary>
    /// Serializes a value of the specified type to a YAML string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="inputType">The declared type of the value.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The YAML representation of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputType" /> is <see langword="null" />.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    public static string Serialize(object? value, Type inputType, YamlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(inputType);

        var buffer = new ArrayBufferWriter<byte>();
        SerializeCore(buffer, value, inputType, options ?? s_defaultOptions);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes a value as UTF-8 YAML to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="destination">The buffer writer that receives the UTF-8 YAML bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, YamlSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        SerializeCore(destination, value, typeof(T), options ?? s_defaultOptions);
    }

    /// <summary>
    /// Asynchronously serializes a value as UTF-8 YAML to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="destination">The stream that receives the UTF-8 YAML bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> for the defaults.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the write.</param>
    /// <returns>A task that completes when the value has been written.</returns>
    /// <remarks>
    /// The value is serialized into an in-memory buffer in full and then written to the stream in a single
    /// asynchronous operation: the method buffers the complete output rather than streaming it, so peak memory
    /// includes the entire rendered document. Cancellation applies to the final write, not to the serialization that
    /// precedes it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    public static ValueTask SerializeAsync<T>(Stream destination, T value, YamlSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfStreamNotWritable(destination);

        var buffer = new ArrayBufferWriter<byte>();
        SerializeCore(buffer, value, typeof(T), options ?? s_defaultOptions);
        return destination.WriteAsync(buffer.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// Serializes a value as UTF-8 YAML to the supplied buffer writer, resolving the root converter from the declared
    /// type.
    /// </summary>
    /// <param name="destination">The buffer writer that receives the UTF-8 YAML bytes.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="inputType">The declared type of the value.</param>
    /// <param name="options">The serializer options.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void SerializeCore(IBufferWriter<byte> destination, object? value, Type inputType, YamlSerializerOptions options)
    {
        options.MakeReadOnly();
        var writer = new Utf8YamlWriter(destination);

        // Attach the serializer write state so the converters report a reference cycle cooperatively and unwind
        // through returns; the single recorded failure is thrown once here at the root boundary.
        var state = new YamlWriteStack();
        writer.AttachWriteStack(state);

        // A null root writes the null scalar without resolving a converter, so a null value paired with a type the
        // converter pipeline does not support still serializes, as it always has.
        if (value is null)
            writer.WriteNull();
        else
            options.GetConverter(inputType).WriteAsObject(writer, value, options);

        state.ThrowIfFailed();
    }
}
