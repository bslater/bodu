// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
        /// Original DotEnv/INI invariant text and empty-only nullable semantics.
        /// </summary>
        Configuration,

        /// <summary>
        /// Delimited round-trip temporal text and blank-cell nullable semantics.
        /// </summary>
        Delimited,
    }

    /// <summary>
    /// Formats a scalar using the invariant culture and the requested wire-format policy.
    /// </summary>
    /// <param name="value">The scalar value to format.</param>
    /// <param name="policy">The formatting policy to apply.</param>
    /// <returns>The scalar's serialized representation.</returns>
    internal static string Format(object? value, Policy policy) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dateTime when policy == Policy.Delimited => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset when policy == Policy.Delimited => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date when policy == Policy.Delimited => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time when policy == Policy.Delimited => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan span when policy == Policy.Delimited => span.ToString("c", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// <summary>
    /// Converts a scalar. May throw the underlying parsing exception, which the format-specific caller must wrap to
    /// preserve its existing public error type and message.
    /// </summary>
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
        if (underlying == typeof(DateOnly) && policy == Policy.Delimited)
            return DateOnly.Parse(raw, CultureInfo.InvariantCulture);
        if (underlying == typeof(TimeOnly) && policy == Policy.Delimited)
            return TimeOnly.Parse(raw, CultureInfo.InvariantCulture);
        if (underlying == typeof(TimeSpan))
            return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
        if (underlying == typeof(Uri))
            return new Uri(raw, UriKind.RelativeOrAbsolute);
        return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
    }
}
