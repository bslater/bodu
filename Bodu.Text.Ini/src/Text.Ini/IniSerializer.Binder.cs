// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;
using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

public static partial class IniSerializer
{
    /// <summary>The message describing why the serializer requires unreferenced code.</summary>
    internal const string RequiresUnreferencedCodeMessage =
        "Reflection-based INI serialization may require members that trimming cannot statically determine.";

    /// <summary>The message describing why the serializer requires dynamic code.</summary>
    internal const string RequiresDynamicCodeMessage =
        "Reflection-based INI serialization may require runtime code generation.";

    /// <summary>
    /// Writes the INI document representation of a value, dispatching between dictionary and POCO roots.
    /// </summary>
    /// <param name="writer">The writer that receives the INI bytes.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="declaredType">The declared type of the value.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when the value cannot be mapped to an INI document.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteValue(ref Utf8IniWriter writer, object? value, Type declaredType, IniSerializerOptions options)
    {
        if (value is null)
            return;

        (value as IOnSerializing)?.OnSerializing();

        if (value is IDictionary dictionary)
            WriteRootDictionary(ref writer, dictionary, options);
        else if (IsScalarOrEnumerable(value.GetType()))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniRootNotObject, value.GetType()));
        else
            WriteRootPoco(ref writer, value, options);

        (value as IOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes a string-keyed dictionary root: scalar-valued entries (and the reserved global entry's contents) as
    /// global keys, then dictionary-valued entries as sections.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="dictionary">The root dictionary.</param>
    /// <param name="options">The serializer options.</param>
    private static void WriteRootDictionary(ref Utf8IniWriter writer, IDictionary dictionary, IniSerializerOptions options)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            string key = KeyToString(entry.Key);
            if (entry.Value is IDictionary nested)
            {
                if (IsGlobalName(key, options))
                    WriteDictionaryEntries(ref writer, nested, options);

                continue;
            }

            if (ShouldSkipDictionaryValue(entry.Value, options))
                continue;

            writer.WritePropertyName(key);
            writer.WriteString(ValueToString(entry.Value));
        }

        foreach (DictionaryEntry entry in dictionary)
        {
            string key = KeyToString(entry.Key);
            if (entry.Value is IDictionary nested && !IsGlobalName(key, options))
            {
                writer.WriteSectionHeader(key);
                WriteDictionaryEntries(ref writer, nested, options);
            }
        }
    }

    /// <summary>
    /// Writes a POCO root: scalar members (and the reserved global member's contents) as global keys, then
    /// object-shaped members as sections.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The root object.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when a member cannot be represented within INI's two levels.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteRootPoco(ref Utf8IniWriter writer, object value, IniSerializerOptions options)
    {
        List<Member> members = GetMembers(value.GetType(), options).Where(static m => m.CanRead).ToList();

        foreach (Member member in members)
        {
            if (IsScalarType(member.MemberType))
            {
                object? memberValue = member.GetValue(value);
                if (ShouldSkipOnWrite(memberValue, member, options))
                    continue;

                writer.WritePropertyName(member.Name);
                writer.WriteString(ValueToString(memberValue));
            }
            else if (IsGlobalName(member.Name, options))
            {
                object? memberValue = member.GetValue(value);
                if (memberValue is not null)
                    WriteSectionBody(ref writer, member.Name, memberValue, options);
            }
        }

        foreach (Member member in members)
        {
            if (IsScalarType(member.MemberType) || IsGlobalName(member.Name, options))
                continue;

            if (IsUnsupportedEnumerable(member.MemberType))
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));

            object? memberValue = member.GetValue(value);
            if (ShouldSkipOnWrite(memberValue, member, options))
                continue;

            writer.WriteSectionHeader(member.Name);
            if (memberValue is not null)
                WriteSectionBody(ref writer, member.Name, memberValue, options);
        }
    }

    /// <summary>
    /// Writes the body of one section from a section-shaped value — a string-keyed dictionary or a POCO of scalar
    /// members.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="sectionName">The section name, used for diagnostics.</param>
    /// <param name="body">The section-shaped value.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">Thrown when the value nests beyond INI's two levels.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteSectionBody(ref Utf8IniWriter writer, string sectionName, object body, IniSerializerOptions options)
    {
        if (body is IDictionary dictionary)
        {
            WriteDictionaryEntries(ref writer, dictionary, options);
            return;
        }

        if (IsScalarOrEnumerable(body.GetType()))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, sectionName));

        (body as IOnSerializing)?.OnSerializing();

        foreach (Member member in GetMembers(body.GetType(), options))
        {
            if (!member.CanRead)
                continue;

            if (!IsScalarType(member.MemberType))
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));

            object? memberValue = member.GetValue(body);
            if (ShouldSkipOnWrite(memberValue, member, options))
                continue;

            writer.WritePropertyName(member.Name);
            writer.WriteString(ValueToString(memberValue));
        }

        (body as IOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes the entries of a string-keyed dictionary as <c>key=value</c> lines.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="entries">The dictionary to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when an entry value is itself a dictionary, which INI cannot represent inside a section.
    /// </exception>
    private static void WriteDictionaryEntries(ref Utf8IniWriter writer, IDictionary entries, IniSerializerOptions options)
    {
        foreach (DictionaryEntry entry in entries)
        {
            if (entry.Value is IDictionary)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, KeyToString(entry.Key)));

            if (ShouldSkipDictionaryValue(entry.Value, options))
                continue;

            writer.WritePropertyName(KeyToString(entry.Key));
            writer.WriteString(ValueToString(entry.Value));
        }
    }

    /// <summary>
    /// Reads a normalized INI document into a value of the target type, dispatching between dictionary and POCO roots.
    /// </summary>
    /// <param name="document">The normalized document.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The materialized value.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document cannot be mapped to the target type.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadValue(IniNormalizedDocument document, Type targetType, IniSerializerOptions options)
    {
        if (TryGetStringKeyedDictionary(targetType, out Type? valueType))
            return ReadRootDictionary(document, targetType, valueType, options);

        if (IsScalarOrEnumerable(targetType))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniRootNotObject, targetType));

        return ReadRootPoco(document, targetType, options);
    }

    /// <summary>
    /// Reads the document into a string-keyed dictionary root: a nested-dictionary value type maps sections (and, with
    /// a reserved global name, the global entries) to sub-dictionaries; a scalar value type maps the global entries
    /// directly.
    /// </summary>
    /// <param name="document">The normalized document.</param>
    /// <param name="targetType">The dictionary type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated dictionary.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document's shape cannot map to the dictionary — global keys with no reserved name, or sections
    /// into a scalar-valued dictionary.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadRootDictionary(IniNormalizedDocument document, Type targetType, Type valueType, IniSerializerOptions options)
    {
        var dictionary = (IDictionary)Activator.CreateInstance(ConcreteDictionaryType(targetType, valueType)) !;

        if (TryGetStringKeyedDictionary(valueType, out Type? nestedValueType))
        {
            if (document.GlobalEntries.Count > 0)
            {
                if (options.GlobalSectionName is null)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniGlobalWithoutSectionName, targetType));

                dictionary[options.GlobalSectionName] = BuildEntryDictionary(document.GlobalEntries, valueType, nestedValueType);
            }

            foreach (IniNormalizedSection section in document.Sections)
                dictionary[section.Name] = BuildEntryDictionary(section.Entries, valueType, nestedValueType);

            return dictionary;
        }

        if (document.Sections.Count > 0)
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniValueConversion, document.Sections[0].Name, valueType));

        foreach ((string key, string raw) in document.GlobalEntries)
            dictionary[key] = ConvertFromString(raw, valueType, key);

        return dictionary;
    }

    /// <summary>
    /// Reads the document into a POCO root: scalar members bind to global keys, the reserved global member binds to the
    /// global entries, and object-shaped members bind to sections.
    /// </summary>
    /// <param name="document">The normalized document.</param>
    /// <param name="targetType">The POCO type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when a required member has no matching key or section, or a member nests beyond INI's two levels.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadRootPoco(IniNormalizedDocument document, Type targetType, IniSerializerOptions options)
    {
        object instance = Activator.CreateInstance(targetType) !;
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (Member member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            if (IsScalarType(member.MemberType))
            {
                int found = IndexOfEntry(document.GlobalEntries, member.Name, comparison);
                if (found >= 0)
                    member.SetValue(instance, ConvertFromString(document.GlobalEntries[found].Value, member.MemberType, member.Name));
                else if (member.Required)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));

                continue;
            }

            if (IsGlobalName(member.Name, options))
            {
                member.SetValue(instance, ReadSectionBody(document.GlobalEntries, member.MemberType, options));
                continue;
            }

            if (IsUnsupportedEnumerable(member.MemberType))
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));

            IniNormalizedSection? section = FindSection(document, member.Name, comparison);
            if (section is not null)
                member.SetValue(instance, ReadSectionBody(section.Entries, member.MemberType, options));
            else if (member.Required)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Reads one section's entries into a section-shaped value — a string-keyed dictionary or a POCO of scalar members.
    /// </summary>
    /// <param name="entries">The section's key/value entries.</param>
    /// <param name="targetType">The section-shaped target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated value.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when a required member has no matching key, or a member nests beyond INI's two levels.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadSectionBody(List<KeyValuePair<string, string>> entries, Type targetType, IniSerializerOptions options)
    {
        if (TryGetStringKeyedDictionary(targetType, out Type? valueType))
        {
            var dictionary = (IDictionary)Activator.CreateInstance(ConcreteDictionaryType(targetType, valueType)) !;
            foreach ((string key, string raw) in entries)
                dictionary[key] = ConvertFromString(raw, valueType, key);

            return dictionary;
        }

        object instance = Activator.CreateInstance(targetType) !;
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (Member member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            if (!IsScalarType(member.MemberType))
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));

            int found = IndexOfEntry(entries, member.Name, comparison);
            if (found >= 0)
                member.SetValue(instance, ConvertFromString(entries[found].Value, member.MemberType, member.Name));
            else if (member.Required)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Finds the section with the specified name.
    /// </summary>
    /// <param name="document">The normalized document.</param>
    /// <param name="name">The section name.</param>
    /// <param name="comparison">The name comparison.</param>
    /// <returns>The section, or <see langword="null" /> when absent.</returns>
    private static IniNormalizedSection? FindSection(IniNormalizedDocument document, string name, StringComparison comparison)
    {
        foreach (IniNormalizedSection section in document.Sections)
        {
            if (string.Equals(section.Name, name, comparison))
                return section;
        }

        return null;
    }

    /// <summary>
    /// Finds the index of the entry with the specified key.
    /// </summary>
    /// <param name="entries">The entries to search.</param>
    /// <param name="key">The key name.</param>
    /// <param name="comparison">The key comparison.</param>
    /// <returns>The zero-based entry index, or <c>-1</c> when absent.</returns>
    private static int IndexOfEntry(List<KeyValuePair<string, string>> entries, string key, StringComparison comparison)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].Key, key, comparison))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Builds a section dictionary from decoded entries.
    /// </summary>
    /// <param name="entries">The section's key/value entries.</param>
    /// <param name="dictionaryType">The declared dictionary type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>The populated dictionary.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object BuildEntryDictionary(List<KeyValuePair<string, string>> entries, Type dictionaryType, Type valueType)
    {
        var dictionary = (IDictionary)Activator.CreateInstance(ConcreteDictionaryType(dictionaryType, valueType)) !;
        foreach ((string key, string raw) in entries)
            dictionary[key] = ConvertFromString(raw, valueType, key);

        return dictionary;
    }

    /// <summary>
    /// Resolves the concrete dictionary type to instantiate for a declared dictionary type.
    /// </summary>
    /// <param name="declaredType">The declared dictionary type, possibly an interface.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>The instantiable dictionary type.</returns>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static Type ConcreteDictionaryType(Type declaredType, Type valueType) =>
        declaredType.IsInterface
            ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
            : declaredType;

    /// <summary>
    /// Determines whether a name is the reserved global-section name.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the name matches the configured reserved name.</returns>
    private static bool IsGlobalName(string name, IniSerializerOptions options) =>
        options.GlobalSectionName is not null && string.Equals(name, options.GlobalSectionName, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether a member should be omitted from the output for the current value and options.
    /// </summary>
    /// <param name="memberValue">The member's value.</param>
    /// <param name="member">The member descriptor.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the member should be skipped.</returns>
    private static bool ShouldSkipOnWrite(object? memberValue, Member member, IniSerializerOptions options)
    {
        IgnoreCondition condition = member.IgnoreCondition ?? options.DefaultIgnoreCondition;

        return condition switch
        {
            IgnoreCondition.Always => true,
            IgnoreCondition.WhenWritingNull => memberValue is null,
            IgnoreCondition.WhenWritingDefault => memberValue is null || IsDefaultValue(memberValue),
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether a dictionary entry value should be omitted from the output.
    /// </summary>
    /// <param name="value">The entry value.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the entry should be skipped.</returns>
    private static bool ShouldSkipDictionaryValue(object? value, IniSerializerOptions options) =>
        value is null && options.DefaultIgnoreCondition is IgnoreCondition.WhenWritingNull or IgnoreCondition.WhenWritingDefault;

    /// <summary>
    /// Determines whether a value equals the default for its type.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when the value is the type default.</returns>
    private static bool IsDefaultValue(object value)
    {
        Type type = value.GetType();
        if (!type.IsValueType)
            return false;

        object? defaultValue = Activator.CreateInstance(type);
        return value.Equals(defaultValue);
    }

    /// <summary>
    /// Converts a dictionary key to its string representation.
    /// </summary>
    /// <param name="key">The key object.</param>
    /// <returns>The key text.</returns>
    private static string KeyToString(object key) =>
        Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Converts a value to its INI string representation using an invariant, round-trippable format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string representation.</returns>
    private static string ValueToString(object? value) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// <summary>
    /// Converts an INI string value to the specified target type using invariant parsing.
    /// </summary>
    /// <param name="raw">The raw string value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="key">The key, used for diagnostics.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="IniSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, Type targetType, string key)
    {
        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying != typeof(string) && Nullable.GetUnderlyingType(targetType) is not null && raw.Length == 0)
            return null;

        try
        {
            if (underlying == typeof(string))
                return raw;
            if (underlying == typeof(bool))
                return bool.Parse(raw);
            if (underlying.IsEnum)
                return Enum.Parse(underlying, raw, ignoreCase: true);
            if (underlying == typeof(Guid))
                return Guid.Parse(raw);
            if (underlying == typeof(DateTime))
                return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (underlying == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (underlying == typeof(TimeSpan))
                return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(Uri))
                return new Uri(raw, UriKind.RelativeOrAbsolute);

            return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new IniSerializationException(
                string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniValueConversion, key, targetType), ex);
        }
    }

    /// <summary>
    /// Determines whether a type maps to a single INI value rather than a section.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type is scalar-convertible.</returns>
    private static bool IsScalarType(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying == typeof(string)
            || underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid)
            || underlying == typeof(Uri);
    }

    /// <summary>
    /// Determines whether a type is a scalar or a non-dictionary enumerable that cannot be an INI root.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type cannot be an INI document root.</returns>
    private static bool IsScalarOrEnumerable(Type type) =>
        IsScalarType(type) || IsUnsupportedEnumerable(type);

    /// <summary>
    /// Determines whether a type is a non-dictionary enumerable, which INI cannot represent at any level.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type is an unsupported enumerable.</returns>
    private static bool IsUnsupportedEnumerable(Type type) =>
        type != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(type)
            && !typeof(IDictionary).IsAssignableFrom(type)
            && !TryGetStringKeyedDictionary(type, out _);

    /// <summary>
    /// Determines whether the type is (or implements) a dictionary keyed by <see cref="string" />.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is a string-keyed dictionary.</returns>
    private static bool TryGetStringKeyedDictionary(Type type, [NotNullWhen(true)] out Type? valueType)
    {
        foreach (Type candidate in new[] { type }.Concat(type.GetInterfaces()))
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                Type[] args = candidate.GetGenericArguments();
                if (args[0] == typeof(string))
                {
                    valueType = args[1];
                    return true;
                }
            }
        }

        valueType = null;
        return false;
    }

    /// <summary>
    /// Enumerates the mapped members of a type, applying naming, inclusion, and ordering rules.
    /// </summary>
    /// <param name="type">The type whose members to map.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The ordered member descriptors.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static IEnumerable<Member> GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        IniSerializerOptions options)
    {
        var members = new List<Member>();

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetCustomAttribute<IgnoreAttribute>() is { Condition: IgnoreCondition.Always })
                continue;

            members.Add(Member.FromProperty(property, options));
        }

        if (options.IncludeFields)
        {
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fieldInfo.GetCustomAttribute<IgnoreAttribute>() is { Condition: IgnoreCondition.Always })
                    continue;

                members.Add(Member.FromField(fieldInfo, options));
            }
        }

        return members.OrderBy(static m => m.Order);
    }
}
