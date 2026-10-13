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
        /// The DotEnv and INI rules: invariant text, and only empty text is a missing nullable value.
        /// </summary>
        Configuration,

        /// <summary>
        /// The delimited rules: round-trip text for temporal values, and a blank cell, empty or white space, is a
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
    /// Formats a scalar using the invariant culture and the requested wire-format policy.
    /// </summary>
    /// <param name="value">The scalar value to format.</param>
    /// <param name="policy">The formatting policy to apply.</param>
    /// <returns>The scalar's serialized representation.</returns>
    /// <remarks>
    /// A <see cref="TimeOnly" /> is written in its round-trip form, <c>HH:mm:ss.fffffff</c>, under both policies, since
    /// its general invariant form holds only hours and minutes and would not read back as the value written.
    /// </remarks>
    internal static string Format(object? value, Policy policy) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dateTime when policy == Policy.Delimited => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset when policy == Policy.Delimited => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date when policy == Policy.Delimited => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan span when policy == Policy.Delimited => span.ToString("c", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
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
        FlatScalarKind kind = Classify(underlying);

        // Delimited intentionally differs: whitespace-only nullable cells are empty, except for char?.
        bool empty = raw.Length == 0 || (policy == Policy.Delimited && kind != FlatScalarKind.Char && string.IsNullOrWhiteSpace(raw));
        if (nullableUnderlying is not null && empty)
            return null;

        // The numbers IConvertible does not cover parse with the styles Convert.ChangeType gives the ones it does.
        return kind switch
        {
            FlatScalarKind.String => raw,
            FlatScalarKind.Boolean => bool.Parse(raw),
            FlatScalarKind.Enum => Enum.Parse(underlying, raw, ignoreCase: true),
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
            _ => Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture),
        };
    }
}
