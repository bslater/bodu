// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Bodu.Text.Serialization;
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

        // Attach the serializer write state so the writer's converters report a reference cycle cooperatively and
        // unwind through returns; the single recorded failure is thrown once here at the root boundary.
        var state = new YamlWriteStack();
        writer.AttachWriteStack(state);

        WriteValue(writer, value, inputType, o, 0);

        state.ThrowIfFailed();
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
    private static void WriteValue(Utf8YamlWriter writer, object? value, Type declaredType, YamlSerializerOptions options, int depth)
    {
        // Once a reference cycle has been recorded, stop dispatching so the recursion unwinds through normal returns
        // rather than continuing to write into a document that will be discarded.
        if (writer.WriteStack is { HasFailure: true })
            return;

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

        Type runtimeType = value.GetType();
        YamlConverter? converter = options.GetConverter(runtimeType) ?? options.GetConverter(declaredType);
        if (converter is not null)
        {
            converter.WriteAsObject(writer, value, options);
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
            WriteDictionary(writer, dictionary, options, depth);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            WriteSequence(writer, enumerable, options, depth);
            return;
        }

        WriteObject(writer, value, runtimeType, options, depth);
    }

    /// <summary>
    /// Writes a dictionary as a YAML mapping.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="dictionary">The dictionary to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteDictionary(Utf8YamlWriter writer, IDictionary dictionary, YamlSerializerOptions options, int depth)
    {
        YamlWriteStack? state = writer.WriteStack;
        if (state is not null && !state.TryEnterReference(dictionary))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, dictionary.GetType()));
            return;
        }

        try
        {
            writer.WriteStartMapping();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (state is { HasFailure: true })
                    break;

                string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!seen.Add(key))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlDuplicateDictionaryKey, key));
                }

                state?.PushPath(key);
                writer.WritePropertyName(key);
                WriteValue(writer, entry.Value, entry.Value?.GetType() ?? typeof(object), options, depth + 1);
                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            writer.WriteEndMapping();
        }
        finally
        {
            state?.ExitReference(dictionary);
        }
    }

    /// <summary>
    /// Writes an enumerable as a YAML sequence.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="enumerable">The enumerable to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteSequence(Utf8YamlWriter writer, IEnumerable enumerable, YamlSerializerOptions options, int depth)
    {
        YamlWriteStack? state = writer.WriteStack;
        if (state is not null && !state.TryEnterReference(enumerable))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, enumerable.GetType()));
            return;
        }

        try
        {
            writer.WriteStartSequence();
            int index = 0;
            foreach (object? item in enumerable)
            {
                if (state is { HasFailure: true })
                    break;

                state?.PushPath($"[{index++}]");
                WriteValue(writer, item, item?.GetType() ?? typeof(object), options, depth + 1);
                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            writer.WriteEndSequence();
        }
        finally
        {
            state?.ExitReference(enumerable);
        }
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
        Utf8YamlWriter writer,
        object value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        YamlSerializerOptions options,
        int depth)
    {
        YamlMemberInfo[] members = YamlMemberInfo.ForType(type, options.IncludeFields);
        YamlMemberInfo.EnsureUniqueWireNames(members, options, type);

        YamlWriteStack? state = writer.WriteStack;
        if (state is not null && !state.TryEnterReference(value))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, type));
            return;
        }

        try
        {
            (value as IOnSerializing)?.OnSerializing();

            writer.WriteStartMapping();
            foreach (YamlMemberInfo member in OrderMembers(members))
            {
                if (state is { HasFailure: true })
                    break;

                if (member.IsExtensionData)
                    continue;

                object? memberValue = member.Get(value);
                if (ShouldOmit(member, memberValue, options))
                    continue;

                state?.PushPath(member.WireName(options));
                writer.WritePropertyName(member.WireName(options));
                WriteValue(writer, memberValue, member.Type, options, depth + 1);
                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            WriteExtensionData(writer, value, members, options, depth);

            if (state is { HasFailure: true })
                return;

            writer.WriteEndMapping();

            (value as IOnSerialized)?.OnSerialized();
        }
        finally
        {
            state?.ExitReference(value);
        }
    }

    /// <summary>
    /// Writes the entries of the type's extension-data member, if any, after its declared members, rejecting an entry
    /// whose key collides with a declared member's wire name.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The object being written.</param>
    /// <param name="members">The discovered members of the object's type.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="depth">The current recursion depth.</param>
    /// <exception cref="YamlSerializationException">An extension-data key collides with a declared member.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    private static void WriteExtensionData(Utf8YamlWriter writer, object value, YamlMemberInfo[] members, YamlSerializerOptions options, int depth)
    {
        YamlMemberInfo? extension = Array.Find(members, static m => m.IsExtensionData);
        if (extension?.Get(value) is not IDictionary<string, object?> entries || entries.Count == 0)
            return;

        YamlWriteStack? state = writer.WriteStack;
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlMemberInfo member in members)
        {
            if (!member.IsExtensionData)
                declared.Add(member.WireName(options));
        }

        foreach (KeyValuePair<string, object?> entry in entries)
        {
            if (state is { HasFailure: true })
                return;

            if (declared.Contains(entry.Key))
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlExtensionDataKeyCollision, entry.Key, value.GetType()));
            }

            state?.PushPath(entry.Key);
            writer.WritePropertyName(entry.Key);
            WriteValue(writer, entry.Value, entry.Value?.GetType() ?? typeof(object), options, depth + 1);
            state?.PopPath();
        }
    }

    /// <summary>
    /// Determines whether a member is omitted from the output for the given value, honoring the member's per-member
    /// <see cref="IgnoreCondition" /> and, in its absence, the serializer-wide null handling.
    /// </summary>
    /// <param name="member">The member being considered.</param>
    /// <param name="value">The member's current value.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the member is omitted; otherwise <see langword="false" />.</returns>
    private static bool ShouldOmit(YamlMemberInfo member, object? value, YamlSerializerOptions options) =>
        member.Condition switch
        {
            IgnoreCondition.WhenWritingNull => value is null,
            IgnoreCondition.WhenWritingDefault => IsTypeDefault(member.Type, value),
            IgnoreCondition.Never => false,
            _ => options.IgnoreNullValues && value is null,
        };

    /// <summary>
    /// Determines whether a value equals the default of its declared type, used to evaluate
    /// <see cref="IgnoreCondition.WhenWritingDefault" />.
    /// </summary>
    /// <param name="type">The member's declared type.</param>
    /// <param name="value">The member's current value.</param>
    /// <returns><see langword="true" /> when the value is the type default; otherwise <see langword="false" />.</returns>
    private static bool IsTypeDefault(Type type, object? value)
    {
        if (value is null)
            return true;

        if (!type.IsValueType)
            return false;

        object? defaultValue = Activator.CreateInstance(type);
        return value.Equals(defaultValue);
    }

    /// <summary>
    /// Returns the members in write order, sorting by ascending <see cref="YamlMemberInfo.Order" /> only when at least
    /// one member declares a non-default order; otherwise the declared order is preserved unchanged.
    /// </summary>
    /// <param name="members">The discovered members in declaration order.</param>
    /// <returns>The members in the order they should be written.</returns>
    private static IEnumerable<YamlMemberInfo> OrderMembers(YamlMemberInfo[] members)
    {
        foreach (YamlMemberInfo member in members)
        {
            if (member.Order != 0)
                return members.OrderBy(static m => m.Order);
        }

        return members;
    }
}
