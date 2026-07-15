// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Deserialization (binding) for <see cref="YamlSerializer" />.
/// </summary>
/// <remarks>
/// Binding consumes the forward-only <see cref="Utf8YamlReader" /> token stream directly rather than a materialized
/// document object model. The reader resolves anchors and aliases, expands merge keys, applies the duplicate-key
/// policy, and enforces the alias-expansion budget during its parse, so the binder observes a fully composed stream and
/// each of these behaviors is inherited unchanged.
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

        if (!reader.Read())
            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

        return BindValue(ref reader, returnType, o);
    }

    /// <summary>
    /// Binds the value at the reader's current position to the target type.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <param name="type">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound value.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindValue(
        ref Utf8YamlReader reader,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        YamlSerializerOptions options)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return reader.TokenType == YamlTokenType.Null ? null : BindValue(ref reader, underlying, options);

        YamlConverter? converter = options.GetConverter(type);
        if (converter is not null)
            return converter.ReadAsObject(ref reader, type, options);

        if (type == typeof(object))
            return BindDynamic(ref reader, options);

        if (type == typeof(string))
            return reader.TokenType == YamlTokenType.Null ? null : ReaderScalarText(ref reader);

        if (reader.TokenType == YamlTokenType.Null)
            return type.IsValueType ? Activator.CreateInstance(type) : null;

        if (type.IsEnum)
            return BindEnum(ref reader, type);

        if (type == typeof(bool))
            return reader.GetBoolean();

        if (type == typeof(char))
            return BindChar(ref reader);

        if (IsIntegral(type))
            return BindIntegral(ref reader, type, options);

        if (type == typeof(float))
            return float.Parse(ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(double))
            return double.Parse(ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(decimal))
            return decimal.Parse(ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return Guid.Parse(ReaderScalarText(ref reader));

        if (type == typeof(DateTime))
            return DateTime.Parse(ReaderScalarText(ref reader), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(ReaderScalarText(ref reader), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (type == typeof(TimeSpan))
            return TimeSpan.Parse(ReaderScalarText(ref reader), CultureInfo.InvariantCulture);

        if (type.IsArray)
            return BindArray(ref reader, type, options);

        if (TryGetDictionaryValueType(type, out Type? valueType))
            return BindDictionary(ref reader, type, valueType, options);

        if (TryGetEnumerableElementType(type, out Type? elementType))
            return BindList(ref reader, type, elementType, options);

        return BindObject(ref reader, type, options);
    }

    /// <summary>
    /// Binds a child value, attaching the child's path segment to any binding failure so the reported
    /// <see cref="YamlSerializationException.Path" /> identifies the offending member, index, or key.
    /// </summary>
    /// <param name="reader">The reader positioned on the child value's first token.</param>
    /// <param name="type">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="segment">The path segment for this child (a member or key name, or a <c>[index]</c>).</param>
    /// <returns>The bound value.</returns>
    /// <exception cref="YamlSerializationException">The child cannot be bound to the target type.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindChild(
        ref Utf8YamlReader reader,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        YamlSerializerOptions options,
        string segment)
    {
        try
        {
            return BindValue(ref reader, type, options);
        }
        catch (YamlSerializationException ex)
        {
            ex.Path = YamlSerializationException.CombinePath(segment, ex.Path);
            throw;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException or ArgumentException)
        {
            throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, type), ex)
            {
                Path = segment,
            };
        }
    }

    /// <summary>
    /// Binds the value at the reader's current position to a loosely-typed graph of dictionaries, lists, and
    /// primitives.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound value.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindDynamic(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case YamlTokenType.Null:
                return null;
            case YamlTokenType.Boolean:
                return reader.GetBoolean();
            case YamlTokenType.Integer:
                return reader.GetInt64();
            case YamlTokenType.Float:
                return reader.GetDouble();
            case YamlTokenType.String:
                return reader.GetString();
            case YamlTokenType.StartSequence:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
                    list.Add(BindDynamic(ref reader, options));

                return list;
            default:
                var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
                {
                    string key = reader.GetString();
                    reader.Read();
                    map[key] = BindDynamic(ref reader, options);
                }

                return map;
        }
    }

    /// <summary>
    /// Binds the scalar at the reader's current position to an integral target, rejecting a non-integral or
    /// out-of-range source rather than silently truncating it.
    /// </summary>
    /// <param name="reader">The reader positioned on the scalar.</param>
    /// <param name="type">The integral target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound integral value.</returns>
    /// <exception cref="YamlSerializationException">
    /// The source is a non-integral float, or its value is outside the range of <paramref name="type" />.
    /// </exception>
    private static object BindIntegral(ref Utf8YamlReader reader, Type type, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Float)
        {
            double d = reader.GetDouble();
            if (options.NumberHandling == YamlNumberHandling.AllowFloatToInteger)
                return Convert.ChangeType(Math.Truncate(d), type, CultureInfo.InvariantCulture);

            if (!double.IsFinite(d) || Math.Floor(d) != d)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlFloatNotIntegral, d.ToString(CultureInfo.InvariantCulture)));
            }
        }

        string text = ReaderScalarText(ref reader);
        try
        {
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }
        catch (OverflowException ex)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlNumberOutOfRange, text, type), ex);
        }
    }

    /// <summary>
    /// Binds the scalar at the reader's current position to a <see cref="char" />, requiring exactly one Unicode code
    /// unit.
    /// </summary>
    /// <param name="reader">The reader positioned on the scalar.</param>
    /// <returns>The bound character.</returns>
    /// <exception cref="YamlSerializationException">The source is not exactly one character.</exception>
    private static char BindChar(ref Utf8YamlReader reader)
    {
        string s = ReaderScalarText(ref reader);
        if (s.Length != 1)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCharLength, s));
        }

        return s[0];
    }

    /// <summary>
    /// Binds the scalar at the reader's current position to an enumeration value by name or number.
    /// </summary>
    /// <param name="reader">The reader positioned on the scalar.</param>
    /// <param name="enumType">The enumeration type.</param>
    /// <returns>The enumeration value.</returns>
    private static object BindEnum(ref Utf8YamlReader reader, Type enumType)
    {
        if (reader.TokenType == YamlTokenType.Integer)
            return Enum.ToObject(enumType, reader.GetInt64());

        return Enum.Parse(enumType, ReaderScalarText(ref reader), ignoreCase: true);
    }

    /// <summary>
    /// Produces the string form of the scalar at the reader's current position.
    /// </summary>
    /// <param name="reader">The reader positioned on the scalar.</param>
    /// <returns>The scalar's text.</returns>
    /// <exception cref="YamlSerializationException">The current token is not a scalar.</exception>
    private static string ReaderScalarText(ref Utf8YamlReader reader) => reader.TokenType switch
    {
        YamlTokenType.String => reader.GetString(),
        YamlTokenType.Integer => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
        YamlTokenType.Float => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
        YamlTokenType.Boolean => reader.GetBoolean() ? "true" : "false",
        YamlTokenType.Null => string.Empty,
        _ => throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedScalar),
    };

    /// <summary>
    /// Advances the reader past the value at its current position without binding it, so an unbound member's value is
    /// consumed and the enclosing loop resumes on the next token.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    private static void SkipValue(ref Utf8YamlReader reader)
    {
        if (reader.TokenType is not (YamlTokenType.StartMapping or YamlTokenType.StartSequence))
            return;

        int depth = 1;
        while (depth > 0 && reader.Read())
        {
            if (reader.TokenType is YamlTokenType.StartMapping or YamlTokenType.StartSequence)
                depth++;
            else if (reader.TokenType is YamlTokenType.EndMapping or YamlTokenType.EndSequence)
                depth--;
        }
    }

    /// <summary>
    /// Determines whether a type is a CLR integral type.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type is integral.</returns>
    private static bool IsIntegral(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
}
