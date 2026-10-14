// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Bodu.Text.DotEnv.Writer;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

public static partial class DotEnvSerializer
{
    /// <summary>The message describing why the serializer requires unreferenced code.</summary>
    internal const string RequiresUnreferencedCodeMessage =
        "Reflection-based DotEnv serialization may require members that trimming cannot statically determine.";

    /// <summary>The message describing why the serializer requires dynamic code.</summary>
    internal const string RequiresDynamicCodeMessage =
        "Reflection-based DotEnv serialization may require runtime code generation.";

    /// <summary>
    /// Writes the DotEnv object representation of a value, dispatching between dictionary and POCO shapes.
    /// </summary>
    /// <param name="writer">The writer that receives the DotEnv bytes.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="declaredType">The declared type of the value.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the value cannot be mapped to a DotEnv object, or a member or entry holds a value that is not a
    /// scalar.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteValue(ref Utf8DotEnvWriter writer, object? value, Type declaredType, DotEnvSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value is not null)
        {
            (value as IOnSerializing)?.OnSerializing();

            if (FlatBinding.TryGetDictionary(value, out IDictionary? dictionary, out Type? valueType))
                WriteDictionary(ref writer, dictionary, valueType, options);
            else if (IsScalarOrEnumerable(value.GetType()))
                throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvRootType, value.GetType()));
            else
                WritePoco(ref writer, value, options);

            (value as IOnSerialized)?.OnSerialized();
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes the entries of a dictionary keyed by <see cref="string" />.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="dictionary">The dictionary to write.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when an entry that is written holds a value that is not a scalar, or has a key the reader would not read
    /// back.
    /// </exception>
    private static void WriteDictionary(ref Utf8DotEnvWriter writer, IDictionary dictionary, Type valueType, DotEnvSerializerOptions options)
    {
        FlatScalarKind valueKind = InvariantScalarCodec.Classify(valueType);

        foreach (DictionaryEntry entry in dictionary)
        {
            if (FlatBinding.ShouldSkipDictionaryValue(entry.Value, options.DefaultIgnoreCondition))
                continue;

            string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
            ThrowIfNotScalar(entry.Value, valueType, valueKind, key);

            WriteKey(ref writer, key);
            writer.WriteString(ValueToString(entry.Value));
        }
    }

    /// <summary>
    /// Throws when the value of a member or dictionary entry is not a scalar, which no DotEnv value reads back as, so
    /// that it is not written as its <see cref="object.ToString" /> text.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="declaredType">The declared type of the member or of the dictionary's values.</param>
    /// <param name="declaredKind">The scalar kind of <paramref name="declaredType" />.</param>
    /// <param name="key">The key the value would be written under.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <paramref name="declaredType" /> is not a scalar and not <see cref="object" />, whatever the value,
    /// or is <see cref="object" /> and the value is not <see langword="null" /> or a scalar.
    /// </exception>
    private static void ThrowIfNotScalar(object? value, Type declaredType, FlatScalarKind declaredKind, string key)
    {
        if (InvariantScalarCodec.GetUnmappedType(value, declaredType, declaredKind) is { } unmapped)
            throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvValueNotScalar, key, unmapped));
    }

    /// <summary>
    /// Writes a key, translating the writer's refusal of a key that would not read back into the serializer's
    /// exception.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="key">The key to write.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <paramref name="key" /> is not a key the reader reads back; the writer's
    /// <see cref="ArgumentException" /> is the inner exception.
    /// </exception>
    private static void WriteKey(ref Utf8DotEnvWriter writer, string key)
    {
        try
        {
            writer.WritePropertyName(key);
        }
        catch (ArgumentException ex) when (ex.GetType() == typeof(ArgumentException))
        {
            throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvUnwritableKey, key), ex);
        }
    }

    /// <summary>
    /// Writes the mapped public members of a POCO.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The object to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when two members map to one key, when a member's type is not a scalar, or when a member that is written
    /// holds a value that is not a scalar or has a name the reader would not read back.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void WritePoco(ref Utf8DotEnvWriter writer, object value, DotEnvSerializerOptions options)
    {
        // The type is checked whole before any member is written, whatever the members hold, as reading checks it.
        ThrowIfAnyMemberIsNotScalar(value.GetType(), options);

        foreach (FlatMemberDescriptor member in GetMembers(value.GetType(), options))
        {
            if (!member.CanRead)
                continue;

            object? memberValue = member.GetValue(value);

            if (FlatBinding.ShouldSkipOnWrite(memberValue, member.MemberType, member.IgnoreCondition, options.DefaultIgnoreCondition))
                continue;

            ThrowIfNotScalar(memberValue, member.MemberType, member.ScalarKind, member.Name);

            WriteKey(ref writer, member.Name);
            writer.WriteString(ValueToString(memberValue, member.ScalarKind));
        }
    }

    /// <summary>
    /// Checks, before any input is read, that a type can hold a DotEnv document: that it is a string-keyed dictionary,
    /// or an object every member of which is a scalar or <see cref="object" />.
    /// </summary>
    /// <param name="targetType">The type to read the document into.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <paramref name="targetType" /> is a scalar or a collection, when two of its members map to one key,
    /// or when a member's type is neither a scalar nor <see cref="object" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void ThrowIfNotReadable(Type targetType, DotEnvSerializerOptions options)
    {
        if (FlatBinding.TryGetStringKeyedDictionary(targetType, out _))
            return;

        if (IsScalarOrEnumerable(targetType))
            throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvRootType, targetType));

        // A nullable structure is read into the structure, so the structure's members are checked.
        ThrowIfAnyMemberIsNotScalar(Nullable.GetUnderlyingType(targetType) ?? targetType, options);
    }

    /// <summary>
    /// Throws when a member of a type, readable, writable or both, is of a type that is neither a scalar nor
    /// <see cref="object" />, so that the type is rejected whole, by <c>Serialize</c> and <c>Deserialize</c> alike,
    /// whatever its members hold and whatever the ignore condition.
    /// </summary>
    /// <param name="type">The object type.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when two members of <paramref name="type" /> map to one key, or a member's type is neither a scalar nor
    /// <see cref="object" />.
    /// </exception>
    /// <remarks>
    /// Such a member has no DotEnv value that reads back as it, so writing it would give the value's
    /// <see cref="object.ToString" /> text and reading its key would fail, as INI rejects such a member, and as
    /// <c>System.Text.Json</c> rejects a member type it cannot convert, in both directions. A member declared as
    /// <see cref="object" /> passes here and is tested on its value as it is written.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void ThrowIfAnyMemberIsNotScalar(Type type, DotEnvSerializerOptions options)
    {
        foreach (FlatMemberDescriptor member in GetMembers(type, options))
            ThrowIfNotScalar(null, member.MemberType, member.ScalarKind, member.Name);
    }

    /// <summary>
    /// Reads a DotEnv document into a value of the target type, dispatching between dictionary and POCO shapes.
    /// </summary>
    /// <param name="entries">The decoded key/value entries.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The materialized value.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the document cannot be mapped to the target type.
    /// </exception>
    /// <remarks>
    /// A target type that is a <see cref="Nullable{T}" /> of a structure is read into the structure, which the caller
    /// unboxes into the nullable type, as a root of the structure itself is read.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadValue(List<KeyValuePair<string, string>> entries, Type targetType, DotEnvSerializerOptions options)
    {
        if (FlatBinding.TryGetStringKeyedDictionary(targetType, out Type? valueType))
            return ReadDictionary(entries, targetType, valueType, options);

        if (IsScalarOrEnumerable(targetType))
            throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvRootType, targetType));

        return ReadPoco(entries, Nullable.GetUnderlyingType(targetType) ?? targetType, options);
    }

    /// <summary>
    /// Reads the document into a string-keyed dictionary.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="targetType">The dictionary type.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated dictionary.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadDictionary(List<KeyValuePair<string, string>> entries, Type targetType, Type valueType, DotEnvSerializerOptions options)
    {
        object dictionary = CreateInstance(FlatBinding.ConcreteDictionaryType(targetType, valueType));

        foreach ((string key, string raw) in entries)
            FlatBinding.SetEntry(dictionary, valueType, key, ConvertFromString(raw, valueType, key));

        return dictionary;
    }

    /// <summary>
    /// Reads the document into a POCO by matching keys to writable members.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="targetType">The POCO type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when the type cannot be created or a required key is missing.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadPoco(List<KeyValuePair<string, string>> entries, Type targetType, DotEnvSerializerOptions options)
    {
        object instance = CreateInstance(targetType);
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (FlatMemberDescriptor member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            bool found = false;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(entries[i].Key, member.Name, comparison))
                {
                    member.SetValue(instance, ConvertFromString(entries[i].Value, member));
                    found = true;
                    break;
                }
            }

            if (!found && member.Required)
                throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvMissingRequiredKey, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Creates an instance of a type the document is read into, after checking that the type can be created.
    /// </summary>
    /// <param name="type">The type to create.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when <paramref name="type" /> is an interface or an abstract class, or a class with no public
    /// parameterless constructor.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static object CreateInstance(Type type) =>
        FlatBinding.CreateInstance(
            type,
            static type => new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvTypeNotCreatable, type)));

    /// <summary>
    /// Converts a value to its DotEnv string representation using an invariant, round-trippable format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string representation.</returns>
    private static string ValueToString(object? value) =>
        InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Configuration);

    /// <summary>
    /// Converts a member's value to its DotEnv string representation using an invariant, round-trippable format.
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
    /// <exception cref="DotEnvSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, Type targetType, string key)
    {
        try
        {
            return InvariantScalarCodec.Parse(raw, targetType, InvariantScalarCodec.Policy.Configuration);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new DotEnvSerializationException(
                string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvValueConversion, key, targetType), ex);
        }
    }

    /// <summary>
    /// Converts a string into a member's value while preserving this serializer's exception contract.
    /// </summary>
    /// <param name="raw">The raw text.</param>
    /// <param name="member">The member the value is for.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="DotEnvSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, FlatMemberDescriptor member)
    {
        try
        {
            return InvariantScalarCodec.Parse(raw, member.ScalarKind, member.ScalarType, member.IsNullable, InvariantScalarCodec.Policy.Configuration);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new DotEnvSerializationException(
                string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvValueConversion, member.Name, member.MemberType), ex);
        }
    }

    /// <summary>
    /// Determines whether a type is a scalar or a collection, neither of which can be a DotEnv root.
    /// </summary>
    /// <param name="type">The type to test, which is not a string-keyed dictionary.</param>
    /// <returns><see langword="true" /> when the type cannot be a DotEnv object root.</returns>
    /// <remarks>
    /// A scalar cannot be a DotEnv root, although many scalars have public properties that would otherwise be bound as
    /// keys; <see cref="InvariantScalarCodec.IsScalar(Type)" /> lists the scalars. The callers test for a string-keyed
    /// dictionary first, so the collections this rejects include every dictionary whose type is not keyed by
    /// <see cref="string" />, such as a <see cref="Hashtable" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by
    /// <see cref="int" />, which would otherwise be bound as an object with no keys.
    /// </remarks>
    private static bool IsScalarOrEnumerable(Type type) =>
        InvariantScalarCodec.IsScalar(type) || typeof(IEnumerable).IsAssignableFrom(type);

    /// <summary>
    /// Gets the mapped members of a type, applying naming, inclusion, and ordering rules, from the options' cache.
    /// </summary>
    /// <param name="type">The type whose members to map.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <returns>The ordered member descriptors.</returns>
    /// <exception cref="DotEnvSerializationException">
    /// Thrown when two members of <paramref name="type" /> map to one key.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static FlatMemberDescriptor[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        DotEnvSerializerOptions options) =>
        options.MemberCache.GetMembers(
            type,
            options.IncludeFields,
            options.PropertyNamingPolicy,
            static (type, key) => new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvDuplicateKey, type, key)));
}
