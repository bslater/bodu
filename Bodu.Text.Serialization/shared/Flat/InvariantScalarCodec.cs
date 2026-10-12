// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Text.Serialization;

/// <summary>
/// Shares invariant scalar conversion, but leaves each wire format responsible for its null and temporal policies.
/// </summary>
internal static class InvariantScalarCodec
{
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
    /// policies, <see cref="DateOnly" />, <see cref="TimeOnly" />, <see cref="Int128" />, <see cref="UInt128" />,
    /// <see cref="Half" /> and <see cref="BigInteger" /> included; a type with no case of its own converts through
    /// <see cref="Convert.ChangeType(object?, Type, IFormatProvider?)" />.
    /// </remarks>
    /// <param name="raw">The text to convert.</param>
    /// <param name="targetType">The desired result type, including nullable wrappers.</param>
    /// <param name="policy">The parsing policy to apply.</param>
    /// <returns>The parsed value, or <see langword="null" /> for a permitted empty nullable value.</returns>
    internal static object? Parse(string raw, Type targetType, Policy policy)
    {
        Type? nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        Type underlying = nullableUnderlying ?? targetType;

        // Delimited intentionally differs: whitespace-only nullable cells are empty, except for char?.
        bool empty = raw.Length == 0 || (policy == Policy.Delimited && underlying != typeof(char) && string.IsNullOrWhiteSpace(raw));
        if (nullableUnderlying is not null && underlying != typeof(string) && empty)
            return null;

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

        // The numbers IConvertible does not cover parse with the styles Convert.ChangeType gives the ones it does.
        if (underlying == typeof(Int128))
            return Int128.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (underlying == typeof(UInt128))
            return UInt128.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (underlying == typeof(Half))
            return Half.Parse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
        if (underlying == typeof(BigInteger))
            return BigInteger.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);

        return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
    }
}
