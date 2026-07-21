// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

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
    /// Thrown when the value cannot be mapped to a DotEnv object.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static void WriteValue(ref Utf8DotEnvWriter writer, object? value, Type declaredType, DotEnvSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value is not null)
        {
            (value as IOnSerializing)?.OnSerializing();

            if (value is IDictionary dictionary)
                WriteDictionary(ref writer, dictionary, options);
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
    /// <param name="options">The serializer options.</param>
    private static void WriteDictionary(ref Utf8DotEnvWriter writer, IDictionary dictionary, DotEnvSerializerOptions options)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Value is null && options.DefaultIgnoreCondition is IgnoreCondition.WhenWritingNull or IgnoreCondition.WhenWritingDefault)
                continue;

            writer.WritePropertyName(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty);
            writer.WriteString(ValueToString(entry.Value));
        }
    }

    /// <summary>
    /// Writes the mapped public members of a POCO.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The object to write.</param>
    /// <param name="options">The serializer options.</param>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static void WritePoco(ref Utf8DotEnvWriter writer, object value, DotEnvSerializerOptions options)
    {
        foreach (Member member in GetMembers(value.GetType(), options))
        {
            if (!member.CanRead)
                continue;

            object? memberValue = member.GetValue(value);

            if (ShouldSkipOnWrite(memberValue, member, options))
                continue;

            writer.WritePropertyName(member.Name);
            writer.WriteString(ValueToString(memberValue));
        }
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
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadValue(List<KeyValuePair<string, string>> entries, Type targetType, DotEnvSerializerOptions options)
    {
        if (TryGetStringKeyedDictionary(targetType, out Type? valueType))
            return ReadDictionary(entries, targetType, valueType, options);

        if (IsScalarOrEnumerable(targetType))
            throw new DotEnvSerializationException(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvRootType, targetType));

        return ReadPoco(entries, targetType, options);
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
        Type concrete = targetType.IsInterface
            ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
            : targetType;
        var dictionary = (IDictionary)Activator.CreateInstance(concrete) !;

        foreach ((string key, string raw) in entries)
            dictionary[key] = ConvertFromString(raw, valueType, key);

        return dictionary;
    }

    /// <summary>
    /// Reads the document into a POCO by matching keys to writable members.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="targetType">The POCO type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="DotEnvSerializationException">Thrown when a required key is missing.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object ReadPoco(List<KeyValuePair<string, string>> entries, Type targetType, DotEnvSerializerOptions options)
    {
        object instance = Activator.CreateInstance(targetType) !;
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (Member member in GetMembers(targetType, options))
        {
            if (!member.CanWrite)
                continue;

            bool found = false;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(entries[i].Key, member.Name, comparison))
                {
                    member.SetValue(instance, ConvertFromString(entries[i].Value, member.MemberType, member.Name));
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
    /// Determines whether a member should be omitted from the output for the current value and options.
    /// </summary>
    /// <param name="memberValue">The member's value.</param>
    /// <param name="member">The member descriptor.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns><see langword="true" /> when the member should be skipped.</returns>
    private static bool ShouldSkipOnWrite(object? memberValue, Member member, DotEnvSerializerOptions options)
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
    /// Converts a value to its DotEnv string representation using an invariant, round-trippable format.
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
    /// Converts a DotEnv string value to the specified target type using invariant parsing.
    /// </summary>
    /// <param name="raw">The raw string value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="key">The key, used for diagnostics.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="DotEnvSerializationException">Thrown when the value cannot be converted.</exception>
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
            throw new DotEnvSerializationException(
                string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvValueConversion, key, targetType), ex);
        }
    }

    /// <summary>
    /// Determines whether a type is a scalar or a non-dictionary enumerable that cannot be a DotEnv root.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true" /> when the type cannot be a DotEnv object root.</returns>
    private static bool IsScalarOrEnumerable(Type type)
    {
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            return true;

        return typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type);
    }

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
        DotEnvSerializerOptions options)
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
