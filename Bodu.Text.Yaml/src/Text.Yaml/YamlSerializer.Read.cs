// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Deserialization (binding) for <see cref="YamlSerializer" />.
/// </summary>
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
        using var document = YamlDocument.Parse(utf8Yaml, new YamlDocumentOptions
        {
            SpecVersion = o.SpecVersion,
            DuplicateKeyBehavior = o.DuplicateKeyBehavior,
            MergeKeyBehavior = o.MergeKeyBehavior,
            MaxDepth = o.MaxDepth,
        });
        return BindValue(document.RootElement, returnType, o);
    }

    /// <summary>
    /// Binds a YAML element to a value of the target type.
    /// </summary>
    /// <param name="element">The element to bind.</param>
    /// <param name="type">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound value.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindValue(
        YamlElement element,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        YamlSerializerOptions options)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return element.ValueKind == YamlValueKind.Null ? null : BindValue(element, underlying, options);
        }

        YamlConverter? converter = options.GetConverter(type);
        if (converter is not null)
            return converter.ReadAsObject(element, options);

        if (type == typeof(object))
            return BindDynamic(element, options);

        if (type == typeof(string))
            return element.ValueKind == YamlValueKind.Null ? null : ElementToString(element);

        if (element.ValueKind == YamlValueKind.Null)
            return type.IsValueType ? Activator.CreateInstance(type) : null;

        if (type.IsEnum)
            return BindEnum(element, type);

        if (type == typeof(bool))
            return element.GetBoolean();

        if (type == typeof(char))
            return BindChar(element);

        if (IsIntegral(type))
            return BindIntegral(element, type, options);

        if (type == typeof(float))
            return float.Parse(ElementToString(element), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(double))
            return double.Parse(ElementToString(element), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(decimal))
            return decimal.Parse(ElementToString(element), NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return Guid.Parse(ElementToString(element));

        if (type == typeof(DateTime))
            return DateTime.Parse(ElementToString(element), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(ElementToString(element), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (type == typeof(TimeSpan))
            return TimeSpan.Parse(ElementToString(element), CultureInfo.InvariantCulture);

        if (type.IsArray)
            return BindArray(element, type, options);

        if (TryGetDictionaryValueType(type, out Type? valueType))
            return BindDictionary(element, type, valueType, options);

        if (TryGetEnumerableElementType(type, out Type? elementType))
            return BindList(element, type, elementType, options);

        return BindObject(element, type, options);
    }

    /// <summary>
    /// Binds a child element, attaching the child's path segment to any binding failure so the reported
    /// <see cref="YamlSerializationException.Path" /> identifies the offending member, index, or key.
    /// </summary>
    /// <param name="element">The child element to bind.</param>
    /// <param name="type">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="segment">The path segment for this child (a member or key name, or a <c>[index]</c>).</param>
    /// <returns>The bound value.</returns>
    /// <exception cref="YamlSerializationException">The child cannot be bound to the target type.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindChild(
        YamlElement element,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        YamlSerializerOptions options,
        string segment)
    {
        try
        {
            return BindValue(element, type, options);
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
    /// Binds a YAML element to a loosely-typed object graph of dictionaries, lists, and primitives.
    /// </summary>
    /// <param name="element">The element to bind.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound value.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object? BindDynamic(YamlElement element, YamlSerializerOptions options)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Null:
                return null;
            case YamlValueKind.Boolean:
                return element.GetBoolean();
            case YamlValueKind.Integer:
                return element.GetInt64();
            case YamlValueKind.Float:
                return element.GetDouble();
            case YamlValueKind.String:
                return element.GetString();
            case YamlValueKind.Sequence:
                var list = new List<object?>();
                foreach (YamlElement item in element.EnumerateSequence())
                    list.Add(BindDynamic(item, options));

                return list;
            default:
                var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (YamlProperty pair in element.EnumerateMapping())
                    map[pair.Name] = BindDynamic(pair.Value, options);

                return map;
        }
    }

    /// <summary>
    /// Binds a scalar element to an integral target, rejecting a non-integral or out-of-range source rather than
    /// silently truncating it.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <param name="type">The integral target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound integral value.</returns>
    /// <exception cref="YamlSerializationException">
    /// The source is a non-integral float, or its value is outside the range of <paramref name="type" />.
    /// </exception>
    private static object BindIntegral(YamlElement element, Type type, YamlSerializerOptions options)
    {
        if (element.ValueKind == YamlValueKind.Float)
        {
            double d = element.GetDouble();
            if (options.NumberHandling == YamlNumberHandling.AllowFloatToInteger)
                return Convert.ChangeType(Math.Truncate(d), type, CultureInfo.InvariantCulture);

            if (!double.IsFinite(d) || Math.Floor(d) != d)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlFloatNotIntegral, d.ToString(CultureInfo.InvariantCulture)));
            }
        }

        string text = ElementToString(element);
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
    /// Binds a scalar element to a <see cref="char" />, requiring exactly one Unicode code unit.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <returns>The bound character.</returns>
    /// <exception cref="YamlSerializationException">The source is not exactly one character.</exception>
    private static char BindChar(YamlElement element)
    {
        string s = ElementToString(element);
        if (s.Length != 1)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCharLength, s));
        }

        return s[0];
    }

    /// <summary>
    /// Binds a scalar element to an enumeration value by name or number.
    /// </summary>
    /// <param name="element">The element to bind.</param>
    /// <param name="enumType">The enumeration type.</param>
    /// <returns>The enumeration value.</returns>
    private static object BindEnum(YamlElement element, Type enumType)
    {
        if (element.ValueKind == YamlValueKind.Integer)
            return Enum.ToObject(enumType, element.GetInt64());

        return Enum.Parse(enumType, ElementToString(element), ignoreCase: true);
    }

    /// <summary>
    /// Produces the string form of a scalar element.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <returns>The scalar's text.</returns>
    private static string ElementToString(YamlElement element) => element.ValueKind switch
    {
        YamlValueKind.String => element.GetString(),
        YamlValueKind.Integer => element.GetInt64().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Float => element.GetDouble().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Boolean => element.GetBoolean() ? "true" : "false",
        YamlValueKind.Null => string.Empty,
        _ => throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedScalar),
    };

    /// <summary>
    /// Determines whether a type is a CLR integral type.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type is integral.</returns>
    private static bool IsIntegral(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
}
