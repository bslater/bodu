// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
/// types, dictionaries keyed by string, and enumerations. Mapping is reflection-based; custom behaviour is supplied
/// through <see cref="YamlConverter{T}" /> instances registered on <see cref="YamlSerializerOptions" />.
/// </remarks>
public static partial class YamlSerializer
{
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
        var o = options ?? s_defaultOptions;
        o.MakeReadOnly();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);
        WriteValue(ref writer, value, inputType, o, 0);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes a value of the given declared type to the writer.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="declaredType">The declared type of the value.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteValue(ref Utf8YamlWriter writer, object? value, Type declaredType, YamlSerializerOptions options, int depth)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (depth > options.EffectiveMaxDepth)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlNestingTooDeep, options.EffectiveMaxDepth));
        }

        var runtimeType = value.GetType();
        var converter = options.GetConverter(runtimeType) ?? options.GetConverter(declaredType);
        if (converter is not null)
        {
            converter.WriteAsObject(ref writer, value, options);
            return;
        }

        switch (value)
        {
            case string s:
                writer.WriteString(s);
                return;
            case bool b:
                writer.WriteBoolean(b);
                return;
            case char c:
                writer.WriteString(c.ToString());
                return;
            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteInt64(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;
            case ulong ul:
                if (ul <= long.MaxValue)
                    writer.WriteInt64((long)ul);
                else
                    writer.WriteString(ul.ToString(CultureInfo.InvariantCulture));

                return;
            case float or double:
                writer.WriteDouble(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                return;
            case decimal dec:

                // A decimal carries more precision than a double, so it is written as its exact invariant text
                // (quoted, because that text resolves as a float) to round-trip without precision loss.
                writer.WriteString(dec.ToString(CultureInfo.InvariantCulture));
                return;
            case Enum e:
                if (options.WriteEnumsAsStrings)
                    writer.WriteString(e.ToString());
                else
                    writer.WriteInt64(Convert.ToInt64(e, CultureInfo.InvariantCulture));

                return;
            case Guid g:
                writer.WriteString(g.ToString());
                return;
            case DateTime dt:
                writer.WriteString(dt.ToString("o", CultureInfo.InvariantCulture));
                return;
            case DateTimeOffset dto:
                writer.WriteString(dto.ToString("o", CultureInfo.InvariantCulture));
                return;
            case TimeSpan ts:
                writer.WriteString(ts.ToString());
                return;
        }

        if (value is IDictionary dictionary)
        {
            WriteDictionary(ref writer, dictionary, options, depth);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            WriteSequence(ref writer, enumerable, options, depth);
            return;
        }

        WriteObject(ref writer, value, runtimeType, options, depth);
    }

    /// <summary>
    /// Writes a dictionary as a YAML mapping.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="dictionary">The dictionary to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteDictionary(ref Utf8YamlWriter writer, IDictionary dictionary, YamlSerializerOptions options, int depth)
    {
        writer.WriteStartMapping();
        foreach (DictionaryEntry entry in dictionary)
        {
            writer.WritePropertyName(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty);
            WriteValue(ref writer, entry.Value, entry.Value?.GetType() ?? typeof(object), options, depth + 1);
        }

        writer.WriteEndMapping();
    }

    /// <summary>
    /// Writes an enumerable as a YAML sequence.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="enumerable">The enumerable to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteSequence(ref Utf8YamlWriter writer, IEnumerable enumerable, YamlSerializerOptions options, int depth)
    {
        writer.WriteStartSequence();
        foreach (var item in enumerable)
            WriteValue(ref writer, item, item?.GetType() ?? typeof(object), options, depth + 1);

        writer.WriteEndSequence();
    }

    /// <summary>
    /// Writes a plain CLR object as a YAML mapping of its members.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The object to write.</param>
    /// <param name="type">The runtime type of the object.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteObject(
        ref Utf8YamlWriter writer,
        object value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        YamlSerializerOptions options,
        int depth)
    {
        var members = YamlMemberInfo.ForType(type, options.IncludeFields);
        YamlMemberInfo.EnsureUniqueWireNames(members, options, type);

        writer.WriteStartMapping();
        foreach (var member in members)
        {
            var memberValue = member.Get(value);
            if (options.IgnoreNullValues && memberValue is null)
                continue;

            writer.WritePropertyName(member.WireName(options));
            WriteValue(ref writer, memberValue, member.Type, options, depth + 1);
        }

        writer.WriteEndMapping();
    }
}
