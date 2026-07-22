// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Bodu.Text.Delimited.Writer;
using Bodu.Text.Serialization;

namespace Bodu.Text.Delimited;

public static partial class DelimitedSerializer
{
    /// <summary>The message describing why the serializer requires unreferenced code.</summary>
    internal const string RequiresUnreferencedCodeMessage =
        "Reflection-based delimited serialization may require members that trimming cannot statically determine.";

    /// <summary>The message describing why the serializer requires dynamic code.</summary>
    internal const string RequiresDynamicCodeMessage =
        "Reflection-based delimited serialization may require runtime code generation.";

    /// <summary>
    /// Writes a single POCO record as a delimited object.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="record">The record instance.</param>
    /// <param name="members">The mapped members.</param>
    private static void WriteRecord(ref Utf8DelimitedWriter writer, object record, Member[] members)
    {
        (record as IOnSerializing)?.OnSerializing();

        writer.WriteStartObject();
        foreach (Member member in members)
        {
            if (!member.CanRead)
                continue;

            writer.WritePropertyName(member.Name);
            writer.WriteString(ValueToString(member.GetValue(record)));
        }

        writer.WriteEndObject();

        (record as IOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes a positional <see cref="string" /> array record as a delimited array.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="fields">The field values.</param>
    private static void WriteStringArrayRecord(ref Utf8DelimitedWriter writer, string[] fields)
    {
        writer.WriteStartArray();
        foreach (string field in fields)
            writer.WriteString(field ?? string.Empty);

        writer.WriteEndArray();
    }

    /// <summary>
    /// Binds a single decoded row to a new record instance of the target type.
    /// </summary>
    /// <param name="fields">The decoded field values.</param>
    /// <param name="headers">The header column names.</param>
    /// <param name="members">The mapped members.</param>
    /// <param name="recordType">The record type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound record.</returns>
    /// <exception cref="DelimitedSerializationException">Thrown when a field cannot be converted.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object BindRecord(string[] fields, IReadOnlyList<string> headers, Member[] members, Type recordType, DelimitedSerializerOptions options)
    {
        object instance = Activator.CreateInstance(recordType) !;
        (instance as IOnDeserializing)?.OnDeserializing();

        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (Member member in members)
        {
            if (!member.CanWrite)
                continue;

            int column = IndexOfHeader(headers, member.Name, comparison);
            if (column >= 0 && column < fields.Length)
                member.SetValue(instance, ConvertFromString(fields[column], member.MemberType, member.Name));
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Finds the column index of a header by name under the supplied comparison.
    /// </summary>
    /// <param name="headers">The header column names.</param>
    /// <param name="name">The column name to find.</param>
    /// <param name="comparison">The name comparison.</param>
    /// <returns>The zero-based column index, or <c>-1</c> when absent.</returns>
    private static int IndexOfHeader(IReadOnlyList<string> headers, string name, StringComparison comparison)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], name, comparison))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Converts a value to its delimited string representation using an invariant, round-trippable format.
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
    /// Converts a delimited string value to the specified target type using invariant parsing.
    /// </summary>
    /// <param name="raw">The raw string value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="column">The column name, used for diagnostics.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="DelimitedSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, Type targetType, string column)
    {
        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (Nullable.GetUnderlyingType(targetType) is not null && raw.Length == 0)
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

            return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new DelimitedSerializationException(
                string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedValueConversion, column, targetType), ex);
        }
    }

    /// <summary>
    /// Determines the record (element) type for a collection type, or <see langword="null" /> when it is not an
    /// enumerable of records.
    /// </summary>
    /// <param name="collectionType">The collection type.</param>
    /// <returns>The record type, or <see langword="null" />.</returns>
    private static Type? GetRecordType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        foreach (Type candidate in collectionType.GetInterfaces())
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return candidate.GetGenericArguments()[0];
        }

        return null;
    }

    /// <summary>
    /// Enumerates the mapped members of a record type, ordered by the property-order attribute.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The ordered member descriptors.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static Member[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        DelimitedSerializerOptions options)
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

        return [.. members.OrderBy(static m => m.Order)];
    }
}
