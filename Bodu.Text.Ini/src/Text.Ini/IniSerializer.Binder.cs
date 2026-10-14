// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
    /// Thrown when the value cannot be mapped to an INI document, including when the type of a value that is not a
    /// dictionary has a member INI cannot represent, which is reported before anything is written.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteValue(ref Utf8IniWriter writer, object? value, Type declaredType, IniSerializerOptions options)
    {
        if (value is null)
            return;

        (value as IOnSerializing)?.OnSerializing();

        if (FlatBinding.TryGetDictionary(value, out IDictionary? dictionary, out Type? valueType))
        {
            WriteRootDictionary(ref writer, dictionary, valueType, options);
        }
        else
        {
            // The type is checked whole, whatever the instance holds, before anything is written, as reading checks it.
            ThrowIfRootNotSupported(value.GetType(), options);
            WriteRootPoco(ref writer, value, options);
        }

        (value as IOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes a string-keyed dictionary root: scalar-valued entries (and the reserved global entry's contents) as
    /// global keys, then dictionary-valued entries as sections.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="dictionary">The root dictionary.</param>
    /// <param name="valueType">The root dictionary's value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when a section nests beyond INI's two levels, when an entry's value is a dictionary whose keys are not
    /// strings, which cannot be a section, when a global entry's value is not a scalar, when a <see langword="null" />
    /// entry that is written has a value type that is neither a scalar, <see cref="object" /> nor a section type, or
    /// when a key, section name or value cannot be written so that it reads back unchanged.
    /// </exception>
    /// <remarks>
    /// An entry is a section or a key by its value. A <see langword="null" /> entry has no value to decide by, so, when
    /// the ignore condition writes it, <paramref name="valueType" /> decides: a string-keyed dictionary type makes it
    /// an empty section, as a POCO root's <see langword="null" /> section member is written, a scalar or
    /// <see cref="object" /> makes it an empty key, and any other type, which no empty key reads back as, is rejected.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteRootDictionary(ref Utf8IniWriter writer, IDictionary dictionary, Type valueType, IniSerializerOptions options)
    {
        bool nullIsSection = FlatBinding.TryGetStringKeyedDictionary(valueType, out _);
        Type? nullUnmappedType = nullIsSection ? null : InvariantScalarCodec.GetUnmappedType(null, valueType, InvariantScalarCodec.Classify(valueType));

        foreach (DictionaryEntry entry in dictionary)
        {
            string key = KeyToString(entry.Key);
            if (FlatBinding.TryGetDictionary(entry.Value, out IDictionary? nested, out Type? nestedValueType))
            {
                if (IsGlobalName(key, options))
                    WriteDictionaryEntries(ref writer, string.Empty, nested, nestedValueType, options);

                continue;
            }

            // A dictionary whose keys are not strings is a collection, which cannot be a section.
            if (entry.Value is IDictionary)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, key));

            if (FlatBinding.ShouldSkipDictionaryValue(entry.Value, options.DefaultIgnoreCondition))
                continue;

            if (entry.Value is null)
            {
                // A null section is written as an empty section, after the global keys.
                if (nullIsSection)
                    continue;

                if (nullUnmappedType is not null)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniValueNotScalar, string.Empty, key, nullUnmappedType));
            }

            ThrowIfNotScalar(entry.Value, string.Empty, key);
            WriteEntry(ref writer, string.Empty, key, ValueToString(entry.Value));
        }

        foreach (DictionaryEntry entry in dictionary)
        {
            string key = KeyToString(entry.Key);
            if (IsGlobalName(key, options))
                continue;

            if (FlatBinding.TryGetDictionary(entry.Value, out IDictionary? nested, out Type? nestedValueType))
            {
                WriteHeader(ref writer, key);
                WriteDictionaryEntries(ref writer, key, nested, nestedValueType, options);
            }
            else if (entry.Value is null && nullIsSection && !FlatBinding.ShouldSkipDictionaryValue(null, options.DefaultIgnoreCondition))
            {
                WriteHeader(ref writer, key);
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
    /// Thrown when a section value's type, derived from the member's declared type, has a member that is not a scalar,
    /// or when a key, section name or value cannot be written so that it reads back unchanged.
    /// </exception>
    /// <remarks>
    /// <see cref="ThrowIfRootNotSupported" /> has checked the root type, and the declared type of every section, before
    /// this method is called.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteRootPoco(ref Utf8IniWriter writer, object value, IniSerializerOptions options)
    {
        FlatMemberDescriptor[] members = GetMembers(value.GetType(), options);

        foreach (FlatMemberDescriptor member in members)
        {
            if (!member.CanRead)
                continue;

            if (member.IsScalar)
            {
                object? memberValue = member.GetValue(value);
                if (FlatBinding.ShouldSkipOnWrite(memberValue, member.MemberType, member.IgnoreCondition, options.DefaultIgnoreCondition))
                    continue;

                WriteEntry(ref writer, string.Empty, member.Name, ValueToString(memberValue, member.ScalarKind));
            }
            else if (IsGlobalName(member.Name, options))
            {
                object? memberValue = member.GetValue(value);
                if (memberValue is not null)
                    WriteSectionBody(ref writer, member.Name, memberValue, options);
            }
        }

        foreach (FlatMemberDescriptor member in members)
        {
            if (!member.CanRead || member.IsScalar || IsGlobalName(member.Name, options))
                continue;

            object? memberValue = member.GetValue(value);
            if (FlatBinding.ShouldSkipOnWrite(memberValue, member.MemberType, member.IgnoreCondition, options.DefaultIgnoreCondition))
                continue;

            WriteHeader(ref writer, member.Name);
            if (memberValue is not null)
                WriteSectionBody(ref writer, member.Name, memberValue, options);
        }
    }

    /// <summary>
    /// Writes the body of one section from a section-shaped value - a string-keyed dictionary or a POCO of scalar
    /// members.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="sectionName">The section name, used for diagnostics.</param>
    /// <param name="body">The section-shaped value.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when the value nests beyond INI's two levels, or when a key or value cannot be written so that it reads
    /// back unchanged.
    /// </exception>
    /// <remarks>
    /// The section's declared type has been checked with the root type; a value of a type derived from it is checked by
    /// its own type here, before any of its members is written.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteSectionBody(ref Utf8IniWriter writer, string sectionName, object body, IniSerializerOptions options)
    {
        // The reserved global section's entries are written as global keys, so a refused entry is reported as global.
        string entrySection = IsGlobalName(sectionName, options) ? string.Empty : sectionName;
        if (FlatBinding.TryGetDictionary(body, out IDictionary? dictionary, out Type? valueType))
        {
            WriteDictionaryEntries(ref writer, entrySection, dictionary, valueType, options);
            return;
        }

        if (IsScalarOrEnumerable(body.GetType()))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, sectionName));

        ThrowIfSectionNotSupported(body.GetType(), options);

        (body as IOnSerializing)?.OnSerializing();

        foreach (FlatMemberDescriptor member in GetMembers(body.GetType(), options))
        {
            if (!member.CanRead)
                continue;

            object? memberValue = member.GetValue(body);
            if (FlatBinding.ShouldSkipOnWrite(memberValue, member.MemberType, member.IgnoreCondition, options.DefaultIgnoreCondition))
                continue;

            WriteEntry(ref writer, entrySection, member.Name, ValueToString(memberValue, member.ScalarKind));
        }

        (body as IOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes the entries of a string-keyed dictionary as <c>key=value</c> lines.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="sectionName">The section the entries are written in, or an empty string for global entries.</param>
    /// <param name="entries">The dictionary to write.</param>
    /// <param name="valueType">The dictionary's value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when an entry value is itself a dictionary, whatever its keys, which INI cannot represent inside a
    /// section, when an entry value is not a scalar, when a <see langword="null" /> entry that is written has a value
    /// type that is a string-keyed dictionary or is neither a scalar nor <see cref="object" />, or when an entry's key
    /// or value cannot be written so that it reads back unchanged.
    /// </exception>
    /// <remarks>
    /// An entry is decided by its value. A <see langword="null" /> entry has no value to decide by, so, when the ignore
    /// condition writes it, <paramref name="valueType" /> decides: a scalar or <see cref="object" /> makes it an empty
    /// key, a string-keyed dictionary type would be a third level, and any other type, which no empty key reads back
    /// as, is rejected.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteDictionaryEntries(ref Utf8IniWriter writer, string sectionName, IDictionary entries, Type valueType, IniSerializerOptions options)
    {
        bool nullNestsDeeper = FlatBinding.TryGetStringKeyedDictionary(valueType, out _);
        Type? nullUnmappedType = InvariantScalarCodec.GetUnmappedType(null, valueType, InvariantScalarCodec.Classify(valueType));

        foreach (DictionaryEntry entry in entries)
        {
            if (entry.Value is IDictionary || FlatBinding.TryGetDictionary(entry.Value, out _))
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, KeyToString(entry.Key)));

            if (FlatBinding.ShouldSkipDictionaryValue(entry.Value, options.DefaultIgnoreCondition))
                continue;

            string key = KeyToString(entry.Key);
            if (entry.Value is null)
            {
                if (nullNestsDeeper)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, key));

                if (nullUnmappedType is not null)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniValueNotScalar, sectionName, key, nullUnmappedType));
            }

            ThrowIfNotScalar(entry.Value, sectionName, key);
            WriteEntry(ref writer, sectionName, key, ValueToString(entry.Value));
        }
    }

    /// <summary>
    /// Throws when a dictionary entry's value is not a scalar, which no INI value reads back as, so that it is not
    /// written as its <see cref="object.ToString" /> text.
    /// </summary>
    /// <param name="value">The entry's value.</param>
    /// <param name="sectionName">The section the entry is written in, or an empty string for a global entry.</param>
    /// <param name="key">The entry's key.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when <paramref name="value" /> is not <see langword="null" /> and its type is not a scalar.
    /// </exception>
    /// <remarks>
    /// A dictionary entry is a section or a key by its value, not by the dictionary's declared value type, so a value
    /// is tested by its own type, as a member declared as <see cref="object" /> is: a <see langword="null" /> value and
    /// a scalar are written, and anything else is rejected.
    /// </remarks>
    private static void ThrowIfNotScalar(object? value, string sectionName, string key)
    {
        if (InvariantScalarCodec.GetUnmappedType(value, typeof(object), FlatScalarKind.None) is { } unmapped)
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniValueNotScalar, sectionName, key, unmapped));
    }

    /// <summary>
    /// Writes one <c>key=value</c> entry, reporting a key or value the writer refuses as an
    /// <see cref="IniSerializationException" /> that names the section and the key.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="sectionName">The section the entry is written in, or an empty string for a global entry.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when <see cref="Utf8IniWriter" /> refuses the key or the value because it would not read back unchanged;
    /// the writer's <see cref="ArgumentException" /> is the inner exception.
    /// </exception>
    private static void WriteEntry(ref Utf8IniWriter writer, string sectionName, string key, string value)
    {
        try
        {
            writer.WritePropertyName(key);
        }
        catch (ArgumentException ex) when (ex.GetType() == typeof(ArgumentException))
        {
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniUnwritableKey, sectionName, key), ex);
        }

        try
        {
            writer.WriteString(value);
        }
        catch (ArgumentException ex) when (ex.GetType() == typeof(ArgumentException))
        {
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniUnwritableValue, sectionName, key), ex);
        }
    }

    /// <summary>
    /// Writes a section header, reporting a section name the writer refuses as an
    /// <see cref="IniSerializationException" /> that names the section.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="sectionName">The section name.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when <see cref="Utf8IniWriter" /> refuses the name because it would not read back unchanged; the writer's
    /// <see cref="ArgumentException" /> is the inner exception.
    /// </exception>
    private static void WriteHeader(ref Utf8IniWriter writer, string sectionName)
    {
        try
        {
            writer.WriteSectionHeader(sectionName);
        }
        catch (ArgumentException ex) when (ex.GetType() == typeof(ArgumentException))
        {
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniUnwritableSectionName, sectionName), ex);
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
    /// <remarks>
    /// <see cref="ThrowIfRootNotSupported" /> has checked the target type before the document was parsed. A target type
    /// that is a <see cref="Nullable{T}" /> of a structure is read into the structure, which the caller unboxes into
    /// the nullable type.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadValue(IniNormalizedDocument document, Type targetType, IniSerializerOptions options) =>
        FlatBinding.TryGetStringKeyedDictionary(targetType, out Type? valueType)
            ? ReadRootDictionary(document, targetType, valueType, options)
            : ReadRootPoco(document, Nullable.GetUnderlyingType(targetType) ?? targetType, options);

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
    /// Thrown when the document's shape cannot map to the dictionary - global keys with no reserved name, or sections
    /// into a scalar-valued dictionary.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadRootDictionary(IniNormalizedDocument document, Type targetType, Type valueType, IniSerializerOptions options)
    {
        object dictionary = CreateInstance(FlatBinding.ConcreteDictionaryType(targetType, valueType));

        if (FlatBinding.TryGetStringKeyedDictionary(valueType, out Type? nestedValueType))
        {
            if (document.GlobalEntries.Count > 0)
            {
                if (options.GlobalSectionName is null)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniGlobalWithoutSectionName, targetType));

                FlatBinding.SetEntry(dictionary, valueType, options.GlobalSectionName, BuildEntryDictionary(document.GlobalEntries, valueType, nestedValueType));
            }

            foreach (IniNormalizedSection section in document.Sections)
                FlatBinding.SetEntry(dictionary, valueType, section.Name, BuildEntryDictionary(section.Entries, valueType, nestedValueType));

            return dictionary;
        }

        if (document.Sections.Count > 0)
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniValueConversion, document.Sections[0].Name, valueType));

        foreach ((string key, string raw) in document.GlobalEntries)
            FlatBinding.SetEntry(dictionary, valueType, key, ConvertFromString(raw, valueType, key));

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
    /// Thrown when the type or a section type cannot be created, or a required member has no matching key or section.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadRootPoco(IniNormalizedDocument document, Type targetType, IniSerializerOptions options)
    {
        object instance = CreateInstance(targetType);
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (FlatMemberDescriptor member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            if (member.IsScalar)
            {
                int found = IndexOfEntry(document.GlobalEntries, member.Name, comparison);
                if (found >= 0)
                    member.SetValue(instance, ConvertFromString(document.GlobalEntries[found].Value, member));
                else if (member.Required)
                    throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));

                continue;
            }

            // A section of a nullable structure is read into the structure, which is boxed into the member.
            Type sectionType = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;

            if (IsGlobalName(member.Name, options))
            {
                member.SetValue(instance, ReadSectionBody(document.GlobalEntries, sectionType, options));
                continue;
            }

            IniNormalizedSection? section = FindSection(document, member.Name, comparison);
            if (section is not null)
                member.SetValue(instance, ReadSectionBody(section.Entries, sectionType, options));
            else if (member.Required)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Reads one section's entries into a section-shaped value - a string-keyed dictionary or a POCO of scalar members.
    /// </summary>
    /// <param name="entries">The section's key/value entries.</param>
    /// <param name="targetType">The section-shaped target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated value.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when the type cannot be created, or a required member has no matching key.
    /// </exception>
    /// <remarks>
    /// <see cref="ThrowIfRootNotSupported" /> has checked <paramref name="targetType" /> with the root type, before the
    /// document was parsed, so every member of a section object is a scalar; one that cannot be set, such as a get-only
    /// scalar, is written and not read. A <see cref="Nullable{T}" /> of a structure reaches this method as the
    /// structure.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadSectionBody(List<KeyValuePair<string, string>> entries, Type targetType, IniSerializerOptions options)
    {
        if (FlatBinding.TryGetStringKeyedDictionary(targetType, out Type? valueType))
        {
            object dictionary = CreateInstance(FlatBinding.ConcreteDictionaryType(targetType, valueType));
            foreach ((string key, string raw) in entries)
                FlatBinding.SetEntry(dictionary, valueType, key, ConvertFromString(raw, valueType, key));

            return dictionary;
        }

        object instance = CreateInstance(targetType);
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (FlatMemberDescriptor member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            int found = IndexOfEntry(entries, member.Name, comparison);
            if (found >= 0)
                member.SetValue(instance, ConvertFromString(entries[found].Value, member));
            else if (member.Required)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniMissingRequiredKey, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Creates an instance of a type the document is read into, after checking that the type can be created.
    /// </summary>
    /// <param name="type">The type to create.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when <paramref name="type" /> is an interface or an abstract class, or a class with no public
    /// parameterless constructor.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static object CreateInstance(Type type) =>
        FlatBinding.CreateInstance(
            type,
            static type => new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniTypeNotCreatable, type)));

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
        object dictionary = CreateInstance(FlatBinding.ConcreteDictionaryType(dictionaryType, valueType));
        foreach ((string key, string raw) in entries)
            FlatBinding.SetEntry(dictionary, valueType, key, ConvertFromString(raw, valueType, key));

        return dictionary;
    }

    /// <summary>
    /// Determines whether a name is the reserved global-section name.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the name matches the configured reserved name.</returns>
    private static bool IsGlobalName(string name, IniSerializerOptions options) =>
        options.GlobalSectionName is not null && string.Equals(name, options.GlobalSectionName, StringComparison.Ordinal);

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
        InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Configuration);

    /// <summary>
    /// Converts a member's value to its INI string representation using an invariant, round-trippable format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="declaredKind">The scalar kind of the member's declared type.</param>
    /// <returns>The string representation.</returns>
    private static string ValueToString(object? value, FlatScalarKind declaredKind) =>
        InvariantScalarCodec.Format(value, declaredKind, InvariantScalarCodec.Policy.Configuration);

    /// <summary>
    /// Converts a string into a scalar while preserving this serializer's exception contract.
    /// </summary>
    /// <param name="raw">The raw text.</param>
    /// <param name="targetType">The CLR type.</param>
    /// <param name="key">The diagnostic field name.</param>
    /// <returns>The converted scalar.</returns>
    /// <exception cref="IniSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, Type targetType, string key)
    {
        try
        {
            return InvariantScalarCodec.Parse(raw, targetType, InvariantScalarCodec.Policy.Configuration);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new IniSerializationException(
                string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniValueConversion, key, targetType), ex);
        }
    }

    /// <summary>
    /// Converts a string into a member's value while preserving this serializer's exception contract.
    /// </summary>
    /// <param name="raw">The raw text.</param>
    /// <param name="member">The member the value is for.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="IniSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, FlatMemberDescriptor member)
    {
        try
        {
            return InvariantScalarCodec.Parse(raw, member.ScalarKind, member.ScalarType, member.IsNullable, InvariantScalarCodec.Policy.Configuration);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new IniSerializationException(
                string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniValueConversion, member.Name, member.MemberType), ex);
        }
    }

    /// <summary>
    /// Checks, before anything is read or written, that a root type is one INI can represent: a string-keyed
    /// dictionary, or an object whose every mapped member is a scalar or a section of scalars.
    /// </summary>
    /// <param name="rootType">The type of the document root, the value's own type when it is written.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when <paramref name="rootType" /> is a scalar or a collection other than a string-keyed dictionary, when
    /// two of its members, or two members of a section type, map to one name, when a member is declared as
    /// <see cref="object" /> or is such a collection, or when a member of a section type is not a scalar.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Every mapped member is checked, whether it can be read, set or both, and so is the declared type of every
    /// section, whether or not the instance holds one, so that a type is rejected in both directions or in neither,
    /// whatever the instance or the document holds, as Delimited and DotEnv reject a type. A member left out with
    /// <see cref="IgnoreAttribute" /> and <see cref="IgnoreCondition.Always" /> is not checked.
    /// </para>
    /// <para>
    /// A string-keyed dictionary, as the root or as a section, is not checked here: each entry is checked by its value,
    /// or for a <see langword="null" /> entry by the dictionary's value type, as it is written. For a
    /// <see cref="Nullable{T}" /> of a structure, as the root or as a section, the structure's members are checked. A
    /// type that passes is remembered by the options, so it is checked once per options instance.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void ThrowIfRootNotSupported(Type rootType, IniSerializerOptions options)
    {
        if (FlatBinding.TryGetStringKeyedDictionary(rootType, out _))
            return;

        if (IsScalarOrEnumerable(rootType))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniRootNotObject, rootType));

        // A root of a nullable structure is read into the structure, so the structure's members are checked.
        Type type = Nullable.GetUnderlyingType(rootType) ?? rootType;
        if (options.CheckedRootTypes.ContainsKey(type))
            return;

        foreach (FlatMemberDescriptor member in GetMembers(type, options))
        {
            if (member.IsScalar)
                continue;

            ThrowIfNotSection(member);
            ThrowIfSectionNotSupported(member.MemberType, options);
        }

        options.CheckedRootTypes.TryAdd(type, true);
    }

    /// <summary>
    /// Checks that a section type is one INI can represent: a string-keyed dictionary, or an object whose every mapped
    /// member is a scalar.
    /// </summary>
    /// <param name="sectionType">The section type, declared or, as a section is written, the value's own type.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when two members of <paramref name="sectionType" /> map to one name, or a member, whether it can be read,
    /// set or both, is not a scalar, which would nest beyond INI's two levels.
    /// </exception>
    /// <remarks>
    /// For a <see cref="Nullable{T}" /> of a structure, the structure's members are checked. A type that passes is
    /// remembered by the options, so it is checked once per options instance.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void ThrowIfSectionNotSupported(Type sectionType, IniSerializerOptions options)
    {
        if (FlatBinding.TryGetStringKeyedDictionary(sectionType, out _))
            return;

        Type type = Nullable.GetUnderlyingType(sectionType) ?? sectionType;
        if (options.CheckedSectionTypes.ContainsKey(type))
            return;

        foreach (FlatMemberDescriptor member in GetMembers(type, options))
        {
            if (!member.IsScalar)
                throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));
        }

        options.CheckedSectionTypes.TryAdd(type, true);
    }

    /// <summary>
    /// Throws when a root member that is not a scalar cannot be a section, whatever its accessors and whatever its
    /// value.
    /// </summary>
    /// <param name="member">The root member, which is not a scalar.</param>
    /// <exception cref="IniSerializationException">
    /// Thrown when the member is declared as <see cref="object" />, which maps to neither a key nor a section, or its
    /// type is a collection other than a string-keyed dictionary, which nests beyond INI's two levels.
    /// </exception>
    /// <remarks>
    /// A member declared as <see cref="object" /> would otherwise be written as a section of whatever its value holds
    /// and read back as a bare <see cref="object" />, with the section's keys dropped; a section type rejects such a
    /// member in the same way, as one that is not a scalar.
    /// </remarks>
    private static void ThrowIfNotSection(FlatMemberDescriptor member)
    {
        if (member.MemberType == typeof(object))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniObjectMember, member.Name));

        if (IsUnsupportedEnumerable(member.MemberType))
            throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, member.Name));
    }

    /// <summary>
    /// Determines whether a type is a scalar or a non-dictionary enumerable that cannot be an INI root.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type cannot be an INI document root.</returns>
    private static bool IsScalarOrEnumerable(Type type) =>
        InvariantScalarCodec.IsScalar(type) || IsUnsupportedEnumerable(type);

    /// <summary>
    /// Determines whether a type is a collection other than a string-keyed dictionary, which INI cannot represent at
    /// any level.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type is an unsupported enumerable.</returns>
    /// <remarks>
    /// The unsupported collections include every dictionary whose type is not keyed by <see cref="string" />, such as a
    /// <see cref="Hashtable" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />: its keys
    /// could not be read back as the type they are, and binding it as an object would read it as empty.
    /// </remarks>
    private static bool IsUnsupportedEnumerable(Type type) =>
        type != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(type)
            && !FlatBinding.TryGetStringKeyedDictionary(type, out _);

    /// <summary>
    /// Gets the mapped members of a type, applying naming, inclusion, and ordering rules, from the options' cache.
    /// </summary>
    /// <param name="type">The type whose members to map.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <returns>The ordered member descriptors.</returns>
    /// <exception cref="IniSerializationException">
    /// Thrown when two members of <paramref name="type" /> map to one name.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static FlatMemberDescriptor[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        IniSerializerOptions options) =>
        options.MemberCache.GetMembers(
            type,
            options.IncludeFields,
            options.PropertyNamingPolicy,
            static (type, name) => new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDuplicateName, type, name)));
}
