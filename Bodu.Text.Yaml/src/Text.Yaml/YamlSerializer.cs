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

        YamlSerializerOptions o = options ?? s_defaultOptions;
        o.MakeReadOnly();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);

        // Attach the serializer write state so the converters report a reference cycle cooperatively and unwind
        // through returns; the single recorded failure is thrown once here at the root boundary.
        var state = new YamlWriteStack();
        writer.AttachWriteStack(state);

        // A null root writes the null scalar without resolving a converter, so a null value paired with a type the
        // converter pipeline does not support still serializes, as it always has.
        if (value is null)
            writer.WriteNull();
        else
            o.GetConverter(inputType).WriteAsObject(writer, value, o);

        state.ThrowIfFailed();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
