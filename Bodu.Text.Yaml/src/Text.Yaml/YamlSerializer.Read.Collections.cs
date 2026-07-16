// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializer.Read.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        YamlSerializerOptions options)
    {
        RequireMapping(ref reader);

        YamlTypeBinding binding = options.GetBinding(type);
        YamlMemberInfo[] members = binding.Members;

        ConstructorInfo? constructor = YamlMemberInfo.GetDeserializationConstructor(type);
        if (constructor is not null && constructor.GetParameters().Length > 0)
            return BindObjectWithConstructor(ref reader, type, binding, constructor, options);

        object instance = Activator.CreateInstance(type)
            ?? throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlTypeNotInstantiable, type));

        (instance as IOnDeserializing)?.OnDeserializing();

        HashSet<YamlMemberInfo>? seenRequired = Array.Exists(members, static m => m.IsRequired) ? new HashSet<YamlMemberInfo>() : null;
        IDictionary<string, object?>? extensionData = ResolveExtensionData(instance, members);

        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();

            if (binding.TryGetMember(name, out YamlMemberInfo? match, out string? wireName))
            {
                if (match.IsRequired)
                    seenRequired!.Add(match);

                if (match.Set is not null)
                    match.Set(instance, BindChild(ref reader, match.Type, options, wireName));
                else if (!TryPopulate(ref reader, match, instance, options))
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
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].IsRequired && !seenRequired.Contains(members[i]))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlMissingRequiredMember, binding.WireNames[i], type));
                }
            }
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Binds a YAML mapping to a type instantiated through a parameterized constructor (such as a positional record):
    /// the read members are buffered, the constructor is invoked with the arguments bound by name, and any remaining
    /// settable members are assigned afterward.
    /// </summary>
    /// <param name="reader">The reader positioned on the mapping's start token.</param>
    /// <param name="type">The target object type.</param>
    /// <param name="binding">The member binding of the target type.</param>
    /// <param name="constructor">The constructor used to instantiate the type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound object.</returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static object BindObjectWithConstructor(
        ref Utf8YamlReader reader,
        Type type,
        YamlTypeBinding binding,
        ConstructorInfo constructor,
        YamlSerializerOptions options)
    {
        YamlMemberInfo[] members = binding.Members;
        var buffered = new Dictionary<YamlMemberInfo, object?>();
        YamlMemberInfo? extension = binding.ExtensionData;
        Dictionary<string, object?>? extensionBuffer = extension is not null ? new Dictionary<string, object?>(StringComparer.Ordinal) : null;
        HashSet<YamlMemberInfo>? seenRequired = Array.Exists(members, static m => m.IsRequired) ? new HashSet<YamlMemberInfo>() : null;

        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();

            if (binding.TryGetMember(name, out YamlMemberInfo? match, out string? wireName))
            {
                if (match.IsRequired)
                    seenRequired!.Add(match);

                buffered[match] = BindChild(ref reader, match.Type, options, wireName);
            }
            else if (extensionBuffer is not null)
            {
                extensionBuffer[name] = BindChild(ref reader, typeof(object), options, name);
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

        ParameterInfo[] parameters = constructor.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        var boundToConstructor = new HashSet<YamlMemberInfo>();
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            YamlMemberInfo? member = Array.Find(
                members,
                m => !m.IsExtensionData && string.Equals(m.MemberName, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (member is not null && buffered.TryGetValue(member, out object? value))
            {
                arguments[i] = value;
                boundToConstructor.Add(member);
            }
            else
            {
                arguments[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }
        }

        object instance = constructor.Invoke(arguments);
        (instance as IOnDeserializing)?.OnDeserializing();

        foreach (KeyValuePair<YamlMemberInfo, object?> entry in buffered)
        {
            if (!boundToConstructor.Contains(entry.Key))
                entry.Key.Set?.Invoke(instance, entry.Value);
        }

        if (extension is not null && extensionBuffer is { Count: > 0 })
        {
            IDictionary<string, object?>? dictionary = extension.Get(instance) as IDictionary<string, object?>;
            if (dictionary is null && extension.Set is not null)
            {
                dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
                extension.Set(instance, dictionary);
            }

            if (dictionary is not null)
            {
                foreach (KeyValuePair<string, object?> entry in extensionBuffer)
                    dictionary[entry.Key] = entry.Value;
            }
        }

        if (seenRequired is not null)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].IsRequired && !seenRequired.Contains(members[i]))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlMissingRequiredMember, binding.WireNames[i], type));
                }
            }
        }

        (instance as IOnDeserialized)?.OnDeserialized();
        return instance;
    }

    /// <summary>
    /// Attempts to populate a get-only collection member by merging the read sequence items into the instance the
    /// member already holds, when the member's resolved object-creation handling is
    /// <see cref="ObjectCreationHandling.Populate" /> and the value at the reader is a sequence.
    /// </summary>
    /// <param name="reader">The reader positioned on the member value.</param>
    /// <param name="member">The member being bound.</param>
    /// <param name="instance">The object being populated.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// <see langword="true" /> when the member was populated (and its value consumed); otherwise
    /// <see langword="false" />.
    /// </returns>
    [RequiresUnreferencedCode("Reflection-based YAML deserialization may require types that trimming cannot statically determine.")]
    private static bool TryPopulate(ref Utf8YamlReader reader, YamlMemberInfo member, object instance, YamlSerializerOptions options)
    {
        ObjectCreationHandling handling = member.CreationHandling ?? options.PreferredObjectCreationHandling;
        if (handling != ObjectCreationHandling.Populate)
            return false;

        if (member.Get(instance) is not IList existing || reader.TokenType != YamlTokenType.StartSequence
            || !TryGetEnumerableElementType(member.Type, out Type elementType))
        {
            return false;
        }

        int index = 0;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
            existing.Add(BindChild(ref reader, elementType, options, $"[{index++}]"));

        return true;
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
