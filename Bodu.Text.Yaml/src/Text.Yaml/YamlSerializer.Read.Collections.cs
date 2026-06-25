// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Collection and object binding for <see cref="YamlSerializer" /> deserialization.
/// </summary>
public static partial class YamlSerializer
{
    /// <summary>
    /// Binds a YAML sequence to an array.
    /// </summary>
    /// <param name="element">The sequence element.</param>
    /// <param name="arrayType">The array type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound array.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static Array BindArray(YamlElement element, Type arrayType, YamlSerializerOptions options)
    {
        var elementType = arrayType.GetElementType()!;
        RequireSequence(element);

        var items = new List<object?>();
        foreach (var item in element.EnumerateSequence())
            items.Add(BindValue(item, elementType, options));

        var array = Array.CreateInstance(elementType, items.Count);
        for (var i = 0; i < items.Count; i++)
            array.SetValue(items[i], i);

        return array;
    }

    /// <summary>
    /// Binds a YAML sequence to a generic list or compatible collection interface.
    /// </summary>
    /// <param name="element">The sequence element.</param>
    /// <param name="type">The target collection type.</param>
    /// <param name="elementType">The element type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound collection.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindList(
        YamlElement element,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        Type elementType,
        YamlSerializerOptions options)
    {
        RequireSequence(element);

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in element.EnumerateSequence())
            list.Add(BindValue(item, elementType, options));

        if (type.IsAssignableFrom(listType))
            return list;

        // A concrete collection with a parameterless constructor and an Add method.
        var target = (IList)Activator.CreateInstance(type)!;
        foreach (var item in list)
            target.Add(item);

        return target;
    }

    /// <summary>
    /// Binds a YAML mapping to a dictionary keyed by string-convertible keys.
    /// </summary>
    /// <param name="element">The mapping element.</param>
    /// <param name="type">The target dictionary type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound dictionary.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindDictionary(YamlElement element, Type type, Type valueType, YamlSerializerOptions options)
    {
        RequireMapping(element);

        var keyType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(string);
        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dict = (IDictionary)Activator.CreateInstance(dictType)!;

        foreach (var pair in element.EnumerateMapping())
        {
            var key = keyType == typeof(string)
                ? pair.Name
                : keyType.IsEnum
                    ? Enum.Parse(keyType, pair.Name, ignoreCase: true)
                    : Convert.ChangeType(pair.Name, keyType, CultureInfo.InvariantCulture);

            dict[key] = BindValue(pair.Value, valueType, options);
        }

        return dict;
    }

    /// <summary>
    /// Binds a YAML mapping to a plain CLR object by setting its writable members.
    /// </summary>
    /// <param name="element">The mapping element.</param>
    /// <param name="type">The target object type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound object.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindObject(
        YamlElement element,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
        YamlSerializerOptions options)
    {
        RequireMapping(element);

        var instance = Activator.CreateInstance(type)
            ?? throw new YamlSerializationException($"The type '{type}' could not be instantiated.");

        var members = YamlMemberInfo.ForType(type, options.IncludeFields);
        var comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var pair in element.EnumerateMapping())
        {
            foreach (var member in members)
            {
                if (member.Set is null || !string.Equals(member.WireName(options), pair.Name, comparison))
                    continue;

                member.Set(instance, BindValue(pair.Value, member.Type, options));
                break;
            }
        }

        return instance;
    }

    /// <summary>
    /// Determines whether a type is a dictionary and yields its value type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="valueType">When the method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is a dictionary.</returns>
    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        foreach (var i in type.GetInterfaces().Append(type))
        {
            if (i.IsGenericType)
            {
                var def = i.GetGenericTypeDefinition();
                if (def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                {
                    valueType = i.GetGenericArguments()[1];
                    return true;
                }
            }
        }

        valueType = typeof(object);
        return false;
    }

    /// <summary>
    /// Determines whether a type is an enumerable (other than a string) and yields its element type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="elementType">When the method returns <see langword="true" />, the element type.</param>
    /// <returns><see langword="true" /> when the type is an enumerable.</returns>
    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        foreach (var i in type.GetInterfaces().Append(type))
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = i.GetGenericArguments()[0];
                return true;
            }
        }

        elementType = typeof(object);
        return false;
    }

    /// <summary>
    /// Validates that an element is a sequence.
    /// </summary>
    /// <param name="element">The element to validate.</param>
    /// <exception cref="YamlSerializationException">The element is not a sequence.</exception>
    private static void RequireSequence(YamlElement element)
    {
        if (element.ValueKind != YamlValueKind.Sequence)
            throw new YamlSerializationException("Expected a YAML sequence.");
    }

    /// <summary>
    /// Validates that an element is a mapping.
    /// </summary>
    /// <param name="element">The element to validate.</param>
    /// <exception cref="YamlSerializationException">The element is not a mapping.</exception>
    private static void RequireMapping(YamlElement element)
    {
        if (element.ValueKind != YamlValueKind.Mapping)
            throw new YamlSerializationException("Expected a YAML mapping.");
    }
}
