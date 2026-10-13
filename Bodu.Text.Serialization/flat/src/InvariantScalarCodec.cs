// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;
using System.Numerics;

namespace Bodu.Text.Serialization;

/// <summary>
/// Shares invariant scalar conversion, but leaves each wire format responsible for its null and temporal policies.
/// </summary>
internal static class InvariantScalarCodec
{
    /// <summary>The scalar kind of every scalar type other than an enumeration: the one list of the types the line formats map to a single value.</summary>
    private static readonly FrozenDictionary<Type, FlatScalarKind> s_kinds = new Dictionary<Type, FlatScalarKind>
    {
        [typeof(string)] = FlatScalarKind.String,
        [typeof(bool)] = FlatScalarKind.Boolean,
        [typeof(char)] = FlatScalarKind.Char,
        [typeof(sbyte)] = FlatScalarKind.ConvertibleNumber,
        [typeof(byte)] = FlatScalarKind.ConvertibleNumber,
        [typeof(short)] = FlatScalarKind.ConvertibleNumber,
        [typeof(ushort)] = FlatScalarKind.ConvertibleNumber,
        [typeof(int)] = FlatScalarKind.ConvertibleNumber,
        [typeof(uint)] = FlatScalarKind.ConvertibleNumber,
        [typeof(long)] = FlatScalarKind.ConvertibleNumber,
        [typeof(ulong)] = FlatScalarKind.ConvertibleNumber,
        [typeof(float)] = FlatScalarKind.ConvertibleNumber,
        [typeof(double)] = FlatScalarKind.ConvertibleNumber,
        [typeof(decimal)] = FlatScalarKind.ConvertibleNumber,
        [typeof(nint)] = FlatScalarKind.NativeInteger,
        [typeof(nuint)] = FlatScalarKind.NativeUnsignedInteger,
        [typeof(Int128)] = FlatScalarKind.Int128,
        [typeof(UInt128)] = FlatScalarKind.UInt128,
        [typeof(Half)] = FlatScalarKind.Half,
        [typeof(BigInteger)] = FlatScalarKind.BigInteger,
        [typeof(Guid)] = FlatScalarKind.Guid,
        [typeof(DateTime)] = FlatScalarKind.DateTime,
        [typeof(DateTimeOffset)] = FlatScalarKind.DateTimeOffset,
        [typeof(DateOnly)] = FlatScalarKind.DateOnly,
        [typeof(TimeOnly)] = FlatScalarKind.TimeOnly,
        [typeof(TimeSpan)] = FlatScalarKind.TimeSpan,
        [typeof(Uri)] = FlatScalarKind.Uri,
    }.ToFrozenDictionary();

    /// <summary>
    /// Defines the format-specific scalar text policy.
    /// </summary>
    internal enum Policy
    {
        /// <summary>
        /// The DotEnv and INI rules: invariant text, round-trip text for <see cref="DateTime" />,
        /// <see cref="DateTimeOffset" /> and <see cref="TimeOnly" />, and only empty text is a missing nullable value.
        /// </summary>
        Configuration,

        /// <summary>
        /// The delimited rules: round-trip text for every temporal value, and a blank cell, empty or white space, is a
        /// missing nullable value, except that one space is a value for a nullable <see cref="char" />.
        /// </summary>
        Delimited,
    }

    /// <summary>
    /// Gets the scalar types other than enumerations, each of which <see cref="Classify(Type)" /> gives a kind other
    /// than <see cref="FlatScalarKind.None" />.
    /// </summary>
    /// <value>The scalar types, in no particular order.</value>
    internal static IReadOnlyCollection<Type> ScalarTypes => s_kinds.Keys;

    /// <summary>
    /// Classifies a type as the scalar the line formats convert it as.
    /// </summary>
    /// <param name="type">The type to classify, which may be a <see cref="Nullable{T}" />.</param>
    /// <returns>
    /// The scalar kind of <paramref name="type" />, or of the type it makes nullable;
    /// <see cref="FlatScalarKind.None" /> when it is not a scalar.
    /// </returns>
    /// <remarks>
    /// The scalars are <see cref="string" />, every primitive (<see cref="nint" /> and <see cref="nuint" /> included),
    /// <see cref="decimal" />, an enumeration, <see cref="Guid" />, <see cref="DateTime" />,
    /// <see cref="DateTimeOffset" />, <see cref="DateOnly" />, <see cref="TimeOnly" />, <see cref="TimeSpan" />,
    /// <see cref="Uri" />, <see cref="Int128" />, <see cref="UInt128" />, <see cref="Half" /> and
    /// <see cref="BigInteger" />. A scalar maps to one field, key or value; any other type maps to members.
    /// </remarks>
    internal static FlatScalarKind Classify(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
            return FlatScalarKind.Enum;

        return s_kinds.TryGetValue(underlying, out FlatScalarKind kind) ? kind : FlatScalarKind.None;
    }

    /// <summary>
    /// Determines whether a type is a scalar, which maps to one field, key or value rather than to members.
    /// </summary>
    /// <param name="type">The type to test, which may be a <see cref="Nullable{T}" />.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Classify(Type)" /> gives <paramref name="type" /> a kind; otherwise
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsScalar(Type type) =>
        Classify(type) != FlatScalarKind.None;

    /// <summary>
    /// Gets the type that keeps the value of a member or dictionary entry from being written as one scalar that reads
    /// back.
    /// </summary>
    /// <param name="value">The value to write, or <see langword="null" />.</param>
    /// <param name="declaredType">The declared type of the member or of the dictionary's values.</param>
    /// <param name="declaredKind">The scalar kind of <paramref name="declaredType" />.</param>
    /// <returns>
    /// <see langword="null" /> when the value maps to a scalar; otherwise <paramref name="declaredType" />, or for a
    /// member declared as <see cref="object" />, the type of its value.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A value of a scalar declared type maps, as does one declared as <see cref="object" /> when it is
    /// <see langword="null" /> or a scalar, which is written by its own type and reads back as its text.
    /// </para>
    /// <para>
    /// Any other declared type, such as a dictionary, a collection or a class with members of its own, maps to no
    /// single value: <see cref="Parse(string, Type, Policy)" /> converts no text to it, an empty value included, so the
    /// type is reported whatever the value, <see langword="null" /> included, rather than written as the value's
    /// <see cref="object.ToString" /> text.
    /// </para>
    /// </remarks>
    internal static Type? GetUnmappedType(object? value, Type declaredType, FlatScalarKind declaredKind)
    {
        if (declaredKind != FlatScalarKind.None)
            return null;

        if (declaredType != typeof(object))
            return declaredType;

        return value is null || IsScalar(value.GetType()) ? null : value.GetType();
    }

    /// <summary>
    /// Formats a scalar using the invariant culture and the requested wire-format policy.
    /// </summary>
    /// <param name="value">The scalar value to format.</param>
    /// <param name="policy">The formatting policy to apply.</param>
    /// <returns>The scalar's serialized representation.</returns>
    /// <remarks>
    /// A <see cref="DateTime" />, a <see cref="DateTimeOffset" /> and a <see cref="TimeOnly" /> are written in their
    /// round-trip forms under both policies: <c>O</c>, which keeps every tick and the kind or the offset, and
    /// <c>HH:mm:ss.fffffff</c>. Their general invariant forms drop the fractions of a second, the kind of a
    /// <see cref="DateTime" />, and for a <see cref="TimeOnly" /> the seconds, so they would not read back as the value
    /// written. The <see cref="Policy.Delimited" /> policy also writes a <see cref="DateOnly" /> as <c>yyyy-MM-dd</c>
    /// and a <see cref="TimeSpan" /> in the constant <c>c</c> form.
    /// </remarks>
    internal static string Format(object? value, Policy policy) =>
        value is null ? string.Empty : FormatScalar(value, Classify(value.GetType()), policy);

    /// <summary>
    /// Formats a value of a member whose declared type is known, using the invariant culture and the requested
    /// wire-format policy.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="declaredKind">
    /// The scalar kind of the member's declared type, or <see cref="FlatScalarKind.None" /> when it is not a scalar.
    /// </param>
    /// <param name="policy">The formatting policy to apply.</param>
    /// <returns>The value's serialized representation.</returns>
    /// <remarks>
    /// A value whose declared type is a scalar is formatted as that scalar, as <see cref="Format(object?, Policy)" />
    /// formats it; any other value, such as one declared as <see cref="object" />, is formatted by its own type. The
    /// Delimited and DotEnv serializers first reject, through
    /// <see cref="GetUnmappedType(object?, Type, FlatScalarKind)" />, a value that maps to no scalar.
    /// </remarks>
    internal static string Format(object? value, FlatScalarKind declaredKind, Policy policy) =>
        value is null ? string.Empty
            : FormatScalar(value, declaredKind == FlatScalarKind.None ? Classify(value.GetType()) : declaredKind, policy);

    /// <summary>
    /// Formats a non-<see langword="null" /> value of a known scalar kind.
    /// </summary>
    /// <param name="value">The value to format, whose type has <paramref name="kind" />.</param>
    /// <param name="kind">The scalar kind of the value's type, or <see cref="FlatScalarKind.None" />.</param>
    /// <param name="policy">The formatting policy to apply.</param>
    /// <returns>The value's serialized representation.</returns>
    private static string FormatScalar(object value, FlatScalarKind kind, Policy policy) =>
        kind switch
        {
            FlatScalarKind.String => (string)value,
            FlatScalarKind.Boolean => (bool)value ? "true" : "false",
            FlatScalarKind.DateTime => ((DateTime)value).ToString("O", CultureInfo.InvariantCulture),
            FlatScalarKind.DateTimeOffset => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture),
            FlatScalarKind.DateOnly when policy == Policy.Delimited => ((DateOnly)value).ToString("O", CultureInfo.InvariantCulture),
            FlatScalarKind.TimeOnly => ((TimeOnly)value).ToString("O", CultureInfo.InvariantCulture),
            FlatScalarKind.TimeSpan when policy == Policy.Delimited => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture),
            _ when value is IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// <summary>
    /// Converts a scalar. A malformed value throws the underlying parsing exception, which each serializer wraps in its
    /// own serialization exception.
    /// </summary>
    /// <remarks>
    /// Every scalar <see cref="Format(object?, Policy)" /> writes reads back with the invariant culture under both
    /// policies, <see cref="DateOnly" />, <see cref="TimeOnly" />, <see cref="nint" />, <see cref="nuint" />,
    /// <see cref="Int128" />, <see cref="UInt128" />, <see cref="Half" /> and <see cref="BigInteger" /> included; a
    /// type with no case of its own converts through <see cref="Convert.ChangeType(object?, Type, IFormatProvider?)" />.
    /// </remarks>
    /// <param name="raw">The text to convert.</param>
    /// <param name="targetType">The desired result type, including nullable wrappers.</param>
    /// <param name="policy">The parsing policy to apply.</param>
    /// <returns>The parsed value, or <see langword="null" /> for a permitted empty nullable value.</returns>
    internal static object? Parse(string raw, Type targetType, Policy policy)
    {
        Type? nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        Type underlying = nullableUnderlying ?? targetType;

        return Parse(raw, Classify(underlying), underlying, nullableUnderlying is not null, policy);
    }

    /// <summary>
    /// Converts a scalar for a member whose scalar kind is already known, as <see cref="Parse(string, Type, Policy)" />
    /// converts it for the member's declared type.
    /// </summary>
    /// <param name="raw">The text to convert.</param>
    /// <param name="kind">The scalar kind of <paramref name="scalarType" />.</param>
    /// <param name="scalarType">
    /// The declared type, or the type it makes nullable when it is a <see cref="Nullable{T}" />.
    /// </param>
    /// <param name="nullable">Whether the declared type is a <see cref="Nullable{T}" />.</param>
    /// <param name="policy">The parsing policy to apply.</param>
    /// <returns>The parsed value, or <see langword="null" /> for a permitted empty nullable value.</returns>
    internal static object? Parse(string raw, FlatScalarKind kind, Type scalarType, bool nullable, Policy policy)
    {
        // Delimited intentionally differs: whitespace-only nullable cells are empty, except for char?.
        bool empty = raw.Length == 0 || (policy == Policy.Delimited && kind != FlatScalarKind.Char && string.IsNullOrWhiteSpace(raw));
        if (nullable && empty)
            return null;

        // The numbers IConvertible does not cover parse with the styles Convert.ChangeType gives the ones it does.
        return kind switch
        {
            FlatScalarKind.String => raw,
            FlatScalarKind.Boolean => bool.Parse(raw),
            FlatScalarKind.Enum => Enum.Parse(scalarType, raw, ignoreCase: true),
            FlatScalarKind.Guid => Guid.Parse(raw),
            FlatScalarKind.DateTime => DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            FlatScalarKind.DateTimeOffset => DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            FlatScalarKind.DateOnly => DateOnly.Parse(raw, CultureInfo.InvariantCulture),
            FlatScalarKind.TimeOnly => TimeOnly.Parse(raw, CultureInfo.InvariantCulture),
            FlatScalarKind.TimeSpan => TimeSpan.Parse(raw, CultureInfo.InvariantCulture),
            FlatScalarKind.Uri => new Uri(raw, UriKind.RelativeOrAbsolute),
            FlatScalarKind.NativeInteger => nint.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            FlatScalarKind.NativeUnsignedInteger => nuint.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            FlatScalarKind.Int128 => Int128.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            FlatScalarKind.UInt128 => UInt128.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            FlatScalarKind.Half => Half.Parse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture),
            FlatScalarKind.BigInteger => BigInteger.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            _ => Convert.ChangeType(raw, scalarType, CultureInfo.InvariantCulture),
        };
    }
}
