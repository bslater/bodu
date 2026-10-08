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
    /// Writes the header row of a record type, the names of its readable members, as a row of its own; a record type
    /// with no readable member has no header row.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="members">The mapped members.</param>
    private static void WriteHeaderRow(ref Utf8DelimitedWriter writer, Member[] members)
    {
        if (!Array.Exists(members, static member => member.CanRead))
            return;

        writer.WriteStartArray();
        foreach (Member member in members)
        {
            if (member.CanRead)
                writer.WriteString(member.Name);
        }

        writer.WriteEndArray();
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
        object instance = Activator.CreateInstance(recordType)!;
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
    /// <remarks>
    /// Temporal values are written in their invariant round-trip forms, so they read back equal: <c>O</c> for
    /// <see cref="DateTime" /> and <see cref="DateTimeOffset" />, which keeps every tick and the kind or the offset;
    /// the ISO 8601 forms <c>yyyy-MM-dd</c> for <see cref="DateOnly" /> and <c>HH:mm:ss.fffffff</c> for
    /// <see cref="TimeOnly" />; and the constant <c>c</c> form for <see cref="TimeSpan" />.
    /// </remarks>
    private static string ValueToString(object? value) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
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
    /// <remarks>
    /// An empty value, or one of white space only, converts to <see langword="null" /> for a nullable value type,
    /// except that a nullable <see cref="char" /> converts to <see langword="null" /> for an empty value alone and
    /// reads white space as a <see cref="char" /> does. Every other target converts the text as it is. Temporal values
    /// parse leniently with the invariant culture, so the round-trip forms written by
    /// <see cref="ValueToString(object?)" /> read back, and so do the invariant general forms that earlier versions
    /// wrote.
    /// </remarks>
    private static object? ConvertFromString(string raw, Type targetType, string column)
    {
        Type? nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        Type underlying = nullableUnderlying ?? targetType;

        // A blank cell is often padded with spaces, so white space holds no value either; but one space is a character,
        // so a char? holds no value only when the text is empty.
        bool holdsNoValue = raw.Length == 0 || (underlying != typeof(char) && string.IsNullOrWhiteSpace(raw));
        if (nullableUnderlying is not null && holdsNoValue)
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
            if (underlying == typeof(DateOnly))
                return DateOnly.Parse(raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(TimeOnly))
                return TimeOnly.Parse(raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(TimeSpan))
                return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(Uri))
                return new Uri(raw, UriKind.RelativeOrAbsolute);

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
    /// <remarks>
    /// Each member name is mapped once, to its most derived declaration: a property or field hidden with the
    /// <see langword="new" /> modifier is not mapped, for writing or for reading. When that most derived declaration is
    /// ignored, the name is not mapped at all.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static Member[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        DelimitedSerializerOptions options)
    {
        // Reflection returns a member hidden with new beside the member that hides it, so only the most derived
        // declaration of each name is kept, at the position its name first appeared.
        var declarations = new List<MemberInfo>();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0)
                KeepMostDerived(declarations, property);
        }

        if (options.IncludeFields)
        {
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                KeepMostDerived(declarations, fieldInfo);
        }

        var members = new List<Member>(declarations.Count);
        foreach (MemberInfo declaration in declarations)
        {
            if (declaration.GetCustomAttribute<IgnoreAttribute>() is { Condition: IgnoreCondition.Always })
                continue;

            members.Add(declaration is PropertyInfo property ? Member.FromProperty(property, options) : Member.FromField((FieldInfo)declaration, options));
        }

        return [.. members.OrderBy(static m => m.Order)];
    }

    /// <summary>
    /// Adds a member declaration to the list unless the list already holds the same name from a more derived type, and
    /// replaces, in place, a declaration of the same name that it hides.
    /// </summary>
    /// <param name="declarations">The declarations kept so far, one per name.</param>
    /// <param name="candidate">The declaration to consider.</param>
    private static void KeepMostDerived(List<MemberInfo> declarations, MemberInfo candidate)
    {
        int existing = declarations.FindIndex(declaration => string.Equals(declaration.Name, candidate.Name, StringComparison.Ordinal));
        if (existing < 0)
        {
            declarations.Add(candidate);
            return;
        }

        if (candidate.DeclaringType is { } declaringType &&
            declarations[existing].DeclaringType is { } keptType &&
            declaringType.IsSubclassOf(keptType))
        {
            declarations[existing] = candidate;
        }
    }
}
