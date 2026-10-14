// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.Binder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Concurrent;
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

    /// <summary>The constructor of each <see cref="KeyValuePair{TKey, TValue}" /> record type read.</summary>
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> s_keyValuePairConstructors = new();

    /// <summary>
    /// Writes a single POCO record as a delimited object.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="record">The record instance.</param>
    /// <param name="members">The mapped members.</param>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when a member declared as <see cref="object" /> holds a value that is not a scalar.
    /// </exception>
    private static void WriteRecord(ref Utf8DelimitedWriter writer, object record, FlatMemberDescriptor[] members)
    {
        (record as IOnSerializing)?.OnSerializing();

        writer.WriteStartObject();
        foreach (FlatMemberDescriptor member in members)
        {
            if (!member.CanRead)
                continue;

            writer.WritePropertyName(member.Name);
            writer.WriteString(MemberValueToString(record, member));
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
    private static void WriteHeaderRow(ref Utf8DelimitedWriter writer, FlatMemberDescriptor[] members)
    {
        if (!Array.Exists(members, static member => member.CanRead))
            return;

        writer.WriteStartArray();
        foreach (FlatMemberDescriptor member in members)
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
    /// Maps each member of a record type to the column of a header that it binds to, once for every row read under the
    /// header.
    /// </summary>
    /// <param name="headers">The header column names, or an empty list for a headerless document.</param>
    /// <param name="members">The mapped members.</param>
    /// <param name="recordType">The record type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// For each member, the index of the first column whose header has its name, or <c>-1</c> when it has none or is
    /// not bound; <see langword="null" /> for a headerless document, whose fields bind by position.
    /// </returns>
    /// <remarks>
    /// A writable member is bound, and so are both members of a <see cref="KeyValuePair{TKey, TValue}" /> record, which
    /// are get-only but are set through its constructor.
    /// </remarks>
    private static int[]? CreateColumnMap(List<string> headers, FlatMemberDescriptor[] members, Type recordType, DelimitedSerializerOptions options)
    {
        if (headers.Count == 0)
            return null;

        bool constructed = IsKeyValuePair(StructTypeOf(recordType));
        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int[] columns = new int[members.Length];
        for (int i = 0; i < members.Length; i++)
            columns[i] = members[i].CanWrite || constructed ? IndexOfHeader(headers, members[i].Name, comparison) : -1;

        return columns;
    }

    /// <summary>
    /// Binds a single decoded row to a new record instance of the target type.
    /// </summary>
    /// <param name="fields">The decoded field values.</param>
    /// <param name="columns">
    /// The column of each member, from <see cref="CreateColumnMap" />, or <see langword="null" /> for a headerless
    /// document.
    /// </param>
    /// <param name="members">The mapped members.</param>
    /// <param name="recordType">The record type.</param>
    /// <returns>The bound record.</returns>
    /// <exception cref="DelimitedSerializationException">Thrown when a field cannot be converted.</exception>
    /// <remarks>
    /// With a header, each writable member binds to the first column whose header has its name. Without one, the fields
    /// bind by position, as a generated <see cref="IDelimitedRecordFactory{TRecord}" /> binds them: the columns are
    /// those <c>Serialize</c> writes, one for each readable member in member order, so a field binds to the member
    /// written in its position, a get-only member's field is skipped, and a member past the last field keeps its
    /// default. A <see cref="KeyValuePair{TKey, TValue}" /> record, whose <c>Key</c> and <c>Value</c> are get-only, is
    /// created through its constructor from the fields of both instead. A record type that is a
    /// <see cref="Nullable{T}" /> of a structure is bound as the structure, which the caller unboxes into the nullable
    /// type.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static object BindRecord(string[] fields, int[]? columns, FlatMemberDescriptor[] members, Type recordType)
    {
        recordType = StructTypeOf(recordType);
        if (IsKeyValuePair(recordType))
            return BindKeyValuePair(fields, columns, members, recordType);

        object instance = Activator.CreateInstance(recordType)!;
        (instance as IOnDeserializing)?.OnDeserializing();

        if (columns is null)
        {
            BindPositionally(instance, fields, members);
        }
        else
        {
            for (int i = 0; i < members.Length; i++)
            {
                int column = columns[i];
                if (column >= 0 && column < fields.Length)
                    members[i].SetValue(instance, ConvertFromString(fields[column], members[i]));
            }
        }

        (instance as IOnDeserialized)?.OnDeserialized();

        return instance;
    }

    /// <summary>
    /// Binds a row to a new <see cref="KeyValuePair{TKey, TValue}" /> record through its constructor, from the fields
    /// of its <c>Key</c> and <c>Value</c> columns, which are matched by header name, or without a header by position.
    /// </summary>
    /// <param name="fields">The decoded field values.</param>
    /// <param name="columns">
    /// The column of each member, from <see cref="CreateColumnMap" />, or <see langword="null" /> for a headerless
    /// document.
    /// </param>
    /// <param name="members">
    /// The mapped members, <c>Key</c> and <c>Value</c>, in the order their columns are written.
    /// </param>
    /// <param name="recordType">The <see cref="KeyValuePair{TKey, TValue}" /> record type.</param>
    /// <returns>The bound record.</returns>
    /// <exception cref="DelimitedSerializationException">Thrown when a field cannot be converted.</exception>
    /// <remarks>
    /// Each field converts as a field of any other member does. A half whose column the document lacks, or which a
    /// short row leaves out, is its type's default, as a missing column leaves any member at its default.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static object BindKeyValuePair(string[] fields, int[]? columns, FlatMemberDescriptor[] members, Type recordType)
    {
        // A null argument for a value-type half is passed to the constructor as that type's default.
        object?[] arguments = new object?[2];
        for (int i = 0; i < members.Length; i++)
        {
            int column = columns is null ? i : columns[i];
            if (column >= 0 && column < fields.Length)
            {
                int argument = string.Equals(members[i].MemberName, nameof(KeyValuePair<int, int>.Key), StringComparison.Ordinal) ? 0 : 1;
                arguments[argument] = ConvertFromString(fields[column], members[i]);
            }
        }

        ConstructorInfo constructor = s_keyValuePairConstructors.GetOrAdd(
            recordType,
            static recordType => recordType.GetConstructor(recordType.GetGenericArguments())!);

        return constructor.Invoke(arguments);
    }

    /// <summary>
    /// Determines whether a record type is a <see cref="KeyValuePair{TKey, TValue}" />, the record a dictionary is
    /// written as, whose get-only <c>Key</c> and <c>Value</c> are set through its constructor.
    /// </summary>
    /// <param name="recordType">The record type.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="recordType" /> is a <see cref="KeyValuePair{TKey, TValue}" />.
    /// </returns>
    private static bool IsKeyValuePair(Type recordType) =>
        recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

    /// <summary>
    /// Gets the type a record of the specified type is written and read as: the structure of a
    /// <see cref="Nullable{T}" />, or the type itself.
    /// </summary>
    /// <param name="recordType">The record type.</param>
    /// <returns>The underlying structure of a nullable record type; otherwise <paramref name="recordType" />.</returns>
    /// <remarks>
    /// A record of a nullable structure, a <see cref="KeyValuePair{TKey, TValue}" /> among them, has the structure's
    /// columns rather than the <see cref="Nullable{T}.HasValue" /> and <see cref="Nullable{T}.Value" /> of the nullable
    /// type, and a <see langword="null" /> one is left out when records are written, as a <see langword="null" />
    /// record of a class is.
    /// </remarks>
    private static Type StructTypeOf(Type recordType) =>
        Nullable.GetUnderlyingType(recordType) ?? recordType;

    /// <summary>
    /// Binds the fields of a headerless row to a record by position, each to the member written in its column.
    /// </summary>
    /// <param name="instance">The record instance.</param>
    /// <param name="fields">The decoded field values.</param>
    /// <param name="members">The mapped members, in the order their columns are written.</param>
    /// <exception cref="DelimitedSerializationException">Thrown when a field cannot be converted.</exception>
    private static void BindPositionally(object instance, string[] fields, FlatMemberDescriptor[] members)
    {
        int column = 0;
        foreach (FlatMemberDescriptor member in members)
        {
            // Only a readable member is written, so only a readable member has a column.
            if (!member.CanRead)
                continue;

            if (column >= fields.Length)
                return;

            if (member.CanWrite)
                member.SetValue(instance, ConvertFromString(fields[column], member));

            column++;
        }
    }

    /// <summary>
    /// Finds the column index of a header by name under the supplied comparison.
    /// </summary>
    /// <param name="headers">The header column names.</param>
    /// <param name="name">The column name to find.</param>
    /// <param name="comparison">The name comparison.</param>
    /// <returns>The zero-based column index, or <c>-1</c> when absent.</returns>
    private static int IndexOfHeader(List<string> headers, string name, StringComparison comparison)
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
        InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Delimited);

    /// <summary>
    /// Converts a member's value to its delimited string representation, as <see cref="ValueToString(object?)" />
    /// converts a value of the member's declared type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="declaredKind">The scalar kind of the member's declared type.</param>
    /// <returns>The string representation.</returns>
    private static string ValueToString(object? value, FlatScalarKind declaredKind) =>
        InvariantScalarCodec.Format(value, declaredKind, InvariantScalarCodec.Policy.Delimited);

    /// <summary>
    /// Gets a record member's value and converts it to its field text, rejecting a value that is not a scalar.
    /// </summary>
    /// <param name="record">The record instance.</param>
    /// <param name="member">The readable member to write.</param>
    /// <returns>The field text.</returns>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when the member's value is not a scalar, which no field reads back as: a member declared as
    /// <see cref="object" /> that holds such a value, since <see cref="GetRecordMembers" /> has rejected every other
    /// declared type that is not a scalar.
    /// </exception>
    private static string MemberValueToString(object record, FlatMemberDescriptor member)
    {
        object? value = member.GetValue(record);
        ThrowIfNotScalar(value, member);

        return ValueToString(value, member.ScalarKind);
    }

    /// <summary>
    /// Throws when a record member, or its value, is not a scalar, which no field reads back as, so that it is not
    /// written as its <see cref="object.ToString" /> text.
    /// </summary>
    /// <param name="value">
    /// The member's value, or <see langword="null" /> to test the member's declared type alone.
    /// </param>
    /// <param name="member">The member.</param>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when the member's declared type is not a scalar and not <see cref="object" />, whatever the value, or is
    /// <see cref="object" /> and the value is not <see langword="null" /> or a scalar.
    /// </exception>
    private static void ThrowIfNotScalar(object? value, FlatMemberDescriptor member)
    {
        if (InvariantScalarCodec.GetUnmappedType(value, member.MemberType, member.ScalarKind) is { } unmapped)
            throw new DelimitedSerializationException(string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Op_Invalid_DelimitedValueNotScalar, member.Name, unmapped));
    }

    /// <summary>
    /// Converts a string into a scalar while preserving this serializer's exception contract.
    /// </summary>
    /// <param name="raw">The raw text.</param>
    /// <param name="targetType">The CLR type.</param>
    /// <param name="column">The diagnostic field name.</param>
    /// <returns>The converted scalar.</returns>
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
        try
        {
            return InvariantScalarCodec.Parse(raw, targetType, InvariantScalarCodec.Policy.Delimited);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new DelimitedSerializationException(
                string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedValueConversion, column, targetType), ex);
        }
    }

    /// <summary>
    /// Converts a field into a member's value, as <see cref="ConvertFromString(string, Type, string)" /> converts it
    /// for the member's declared type and column.
    /// </summary>
    /// <param name="raw">The raw text.</param>
    /// <param name="member">The member the value is for.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="DelimitedSerializationException">Thrown when the value cannot be converted.</exception>
    private static object? ConvertFromString(string raw, FlatMemberDescriptor member)
    {
        try
        {
            return InvariantScalarCodec.Parse(raw, member.ScalarKind, member.ScalarType, member.IsNullable, InvariantScalarCodec.Policy.Delimited);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new DelimitedSerializationException(
                string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedValueConversion, member.Name, member.MemberType), ex);
        }
    }

    /// <summary>
    /// Determines the record (element) type for a collection type, or <see langword="null" /> when it is not an
    /// enumerable of records.
    /// </summary>
    /// <param name="collectionType">The collection type.</param>
    /// <returns>The record type, or <see langword="null" />.</returns>
    /// <remarks>
    /// A <see cref="string" /> is a scalar rather than a collection, although it enumerates its characters, so it has
    /// no record type.
    /// </remarks>
    private static Type? GetRecordType(Type collectionType)
    {
        if (collectionType == typeof(string))
            return null;

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
    /// Gets the mapped members of a record type, and rejects a type that cannot be a record.
    /// </summary>
    /// <param name="recordType">The record type, other than a <see cref="string" /> array.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="reading">
    /// Whether records of the type are created, which needs a public parameterless constructor.
    /// </param>
    /// <returns>The ordered member descriptors.</returns>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when <paramref name="recordType" /> is a scalar or a collection, when <paramref name="reading" /> is
    /// <see langword="true" /> and the type has no public parameterless constructor, or when a member's declared type
    /// is neither a scalar nor <see cref="object" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A scalar converts as one field and a collection holds values rather than columns, so neither is a record,
    /// although both have public properties (<see cref="string.Length" />, <see cref="DateTime.Year" />,
    /// <see cref="List{T}.Capacity" />) that would otherwise be mapped as columns. The check is made before anything is
    /// read or written, so a type that is not a record is rejected whether or not there are records.
    /// </para>
    /// <para>
    /// Every member is one field, so a record type with a member that is not a scalar, such as a dictionary or a class
    /// with members of its own, is rejected in the same way, for reading and for writing alike, whether the member is
    /// readable, writable or both: its field would be the value's <see cref="object.ToString" /> text, which does not
    /// read back, and no field converts to its type. The check is made before the header row is written or the input
    /// read, so that a type rejected in one direction is rejected in the other.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static FlatMemberDescriptor[] GetRecordMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type recordType,
        DelimitedSerializerOptions options,
        bool reading)
    {
        if (InvariantScalarCodec.IsScalar(recordType) || typeof(IEnumerable).IsAssignableFrom(recordType))
        {
            throw new DelimitedSerializationException(
                string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Op_Invalid_DelimitedRecordTypeNotSupported, recordType));
        }

        if (reading && !recordType.IsValueType && (recordType.IsAbstract || recordType.GetConstructor(Type.EmptyTypes) is null))
        {
            throw new DelimitedSerializationException(
                string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Op_Invalid_DelimitedRecordTypeNotCreatable, recordType));
        }

        // A member declared as object is tested again, on its value, as each record is written. A record of a nullable
        // structure has the structure's members.
        FlatMemberDescriptor[] members = GetMembers(StructTypeOf(recordType), options);
        foreach (FlatMemberDescriptor member in members)
            ThrowIfNotScalar(null, member);

        return members;
    }

    /// <summary>
    /// Gets the mapped members of a record type, ordered by the property-order attribute, from the options' cache.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="options">The read-only serializer options.</param>
    /// <returns>The ordered member descriptors.</returns>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when two members of <paramref name="type" /> map to one column.
    /// </exception>
    /// <remarks>
    /// Each member name is mapped once, to its most derived declaration: a property or field hidden with the
    /// <see langword="new" /> modifier is not mapped, for writing or for reading. When that most derived declaration is
    /// ignored, the name is not mapped at all.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static FlatMemberDescriptor[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        DelimitedSerializerOptions options) =>
        options.MemberCache.GetMembers(
            type,
            options.IncludeFields,
            options.PropertyNamingPolicy,
            static (type, column) => new DelimitedSerializationException(string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Op_Invalid_DelimitedDuplicateColumn, type, column)));
}
