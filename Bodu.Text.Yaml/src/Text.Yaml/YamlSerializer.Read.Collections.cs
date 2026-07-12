// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Collection and object binding for <see cref="YamlSerializer" /> deserialization.
/// </summary>
public static partial class YamlSerializer
{
    /// <summary>
    /// Binds the YAML sequence at the reader's current position to an array.
    /// </summary>
    /// <param name="reader">The reader positioned on the sequence's start token.</param>
    /// <param name="arrayType">The array type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound array.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static Array BindArray(ref Utf8YamlReader reader, Type arrayType, YamlSerializerOptions options)
    {
        Type elementType = arrayType.GetElementType()!;
        RequireSequence(ref reader);

        var items = new List<object?>();
        int index = 0;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
            items.Add(BindChild(ref reader, elementType, options, $"[{index++}]"));

        var array = Array.CreateInstance(elementType, items.Count);
        for (int i = 0; i < items.Count; i++)
            array.SetValue(items[i], i);

        return array;
    }

    /// <summary>
    /// Binds the YAML sequence at the reader's current position to a generic list or compatible collection interface.
    /// </summary>
    /// <param name="reader">The reader positioned on the sequence's start token.</param>
    /// <param name="type">The target collection type.</param>
    /// <param name="elementType">The element type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound collection.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindList(
        ref Utf8YamlReader reader,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        Type elementType,
        YamlSerializerOptions options)
    {
        RequireSequence(ref reader);

        Type listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        int index = 0;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
            list.Add(BindChild(ref reader, elementType, options, $"[{index++}]"));

        if (type.IsAssignableFrom(listType))
            return list;

        // A concrete collection with a parameterless constructor and an Add method.
        var target = (IList)Activator.CreateInstance(type)!;
        foreach (object? item in list)
            target.Add(item);

        return target;
    }

    /// <summary>
    /// Binds the YAML mapping at the reader's current position to a dictionary keyed by string-convertible keys.
    /// </summary>
    /// <param name="reader">The reader positioned on the mapping's start token.</param>
    /// <param name="type">The target dictionary type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound dictionary.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindDictionary(ref Utf8YamlReader reader, Type type, Type valueType, YamlSerializerOptions options)
    {
        RequireMapping(ref reader);

        Type keyType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(string);
        Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dict = (IDictionary)Activator.CreateInstance(dictType)!;

        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();

            object key = keyType == typeof(string)
                ? name
                : keyType.IsEnum
                    ? Enum.Parse(keyType, name, ignoreCase: true)
                    : Convert.ChangeType(name, keyType, CultureInfo.InvariantCulture);

            dict[key] = BindChild(ref reader, valueType, options, name);
        }

        return dict;
    }

    /// <summary>
    /// Binds the YAML mapping at the reader's current position to a plain CLR object by setting its writable members.
    /// </summary>
    /// <param name="reader">The reader positioned on the mapping's start token.</param>
    /// <param name="type">The target object type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound object.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindObject(
        ref Utf8YamlReader reader,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
        YamlSerializerOptions options)
    {
        RequireMapping(ref reader);

        object instance = Activator.CreateInstance(type)
            ?? throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlTypeNotInstantiable, type));

        (instance as IOnDeserializing)?.OnDeserializing();

        YamlMemberInfo[] members = YamlMemberInfo.ForType(type, options.IncludeFields);
        YamlMemberInfo.EnsureUniqueWireNames(members, options, type);
        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        HashSet<YamlMemberInfo>? seenRequired = Array.Exists(members, static m => m.IsRequired) ? new HashSet<YamlMemberInfo>() : null;
        IDictionary<string, object?>? extensionData = ResolveExtensionData(instance, members);

        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();

            YamlMemberInfo? match = null;
            foreach (YamlMemberInfo member in members)
            {
                if (!member.IsExtensionData && string.Equals(member.WireName(options), name, comparison))
                {
                    match = member;
                    break;
                }
            }

            if (match is not null)
            {
                if (match.IsRequired)
                    seenRequired!.Add(match);

                if (match.Set is not null)
                    match.Set(instance, BindChild(ref reader, match.Type, options, match.WireName(options)));
                else
                    SkipValue(ref reader);
            }
            else if (extensionData is not null)
            {
                extensionData[name] = BindChild(ref reader, typeof(object), options, name);
            }
            else if (options.UnmappedMemberHandling == UnmappedMemberHandling.Disallow)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlUnmappedMember, name, type));
            }
            else
            {
                SkipValue(ref reader);
            }
        }

        if (seenRequired is not null)
        {
            foreach (YamlMemberInfo member in members)
            {
                if (member.IsRequired && !seenRequired.Contains(member))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlMissingRequiredMember, member.WireName(options), type));
                }
            }
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Resolves the dictionary that captures unmapped keys for a type declaring an extension-data member, reusing an
    /// existing instance or creating and assigning a new one when the member is settable.
    /// </summary>
    /// <param name="instance">The object being populated.</param>
    /// <param name="members">The discovered members of the object's type.</param>
    /// <returns>
    /// The extension-data dictionary, or <see langword="null" /> when the type declares no extension-data member or the
    /// member holds no dictionary and cannot be assigned one.
    /// </returns>
    private static IDictionary<string, object?>? ResolveExtensionData(object instance, YamlMemberInfo[] members)
    {
        YamlMemberInfo? extension = Array.Find(members, static m => m.IsExtensionData);
        if (extension is null)
            return null;

        if (extension.Get(instance) is IDictionary<string, object?> existing)
            return existing;

        if (extension.Set is null)
            return null;

        var created = new Dictionary<string, object?>(StringComparer.Ordinal);
        extension.Set(instance, created);
        return created;
    }

    /// <summary>
    /// Determines whether a type is a dictionary and yields its value type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="valueType">When the method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is a dictionary.</returns>
    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        foreach (Type? i in type.GetInterfaces().Append(type))
        {
            if (i.IsGenericType)
            {
                Type def = i.GetGenericTypeDefinition();
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
        foreach (Type? i in type.GetInterfaces().Append(type))
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
    /// Validates that the reader is positioned on a sequence start token.
    /// </summary>
    /// <param name="reader">The reader to validate.</param>
    /// <exception cref="YamlSerializationException">The current token is not a sequence start.</exception>
    private static void RequireSequence(ref Utf8YamlReader reader)
    {
        if (reader.TokenType != YamlTokenType.StartSequence)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedSequence);
    }

    /// <summary>
    /// Validates that the reader is positioned on a mapping start token.
    /// </summary>
    /// <param name="reader">The reader to validate.</param>
    /// <exception cref="YamlSerializationException">The current token is not a mapping start.</exception>
    private static void RequireMapping(ref Utf8YamlReader reader)
    {
        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedMapping);
    }
}
