// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentParseHelpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the format-agnostic scalar parsing shared by the XML and JSON notable-date document parsers, operating on
/// raw string tokens so that both surfaces apply identical enumeration, month, and working-week semantics.
/// </summary>
internal static class NotableDateDocumentParseHelpers
{
    /// <summary>
    /// The full English month names, indexed so that January is at index zero.
    /// </summary>
    private static readonly string[] s_monthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    /// <summary>
    /// Parses a Sunday-first seven-character working-week pattern, falling back to the default working week when the
    /// value is absent or blank.
    /// </summary>
    /// <param name="value">The working-week pattern, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="WeekPattern" />, or <see langword="null" /> when unspecified.</returns>
    public static WeekPattern? ParseWorkingWeek(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WeekPattern.Parse(value);

    /// <summary>
    /// Parses a trigger comparison month expressed as a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month value, or <see langword="null" />.</param>
    /// <returns>The one-based month, or <see langword="null" /> when absent or invalid.</returns>
    public static int? ParseTriggerMonth(string? value)
    {
        if (value is null)
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
            return numeric;

        int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : null;
    }

    /// <summary>
    /// Parses a month value expressed as a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The one-based month, or 1 when the value is invalid.</returns>
    public static int ParseMonth(string? value, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
                return numeric;

            int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                return index + 1;
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-MONTH",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return 1;
    }

    /// <summary>
    /// Parses a fixed-strategy month for a given calendar system, returning either a numeric month or a Hebrew alias
    /// whose number is resolved at calculation time.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="calendar">The calendar system the month is expressed in.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// A tuple of the one-based month (or <c>0</c> when an alias supplies it) and an optional Hebrew month alias.
    /// </returns>
    public static (int Month, string? Alias) ParseFixedMonth(
        string? value,
        CalendarSystem calendar,
        string notableDateId,
        string ruleId,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (calendar == CalendarSystem.Gregorian)
            return (ParseMonth(value, notableDateId, ruleId, diagnostics), null);

        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 13)
                return (numeric, null);

            switch (value)
            {
                case "Tishri": return (1, null);
                case "Heshvan": return (2, null);
                case "Kislev": return (3, null);
                case "Tevet": return (4, null);
                case "Shevat": return (5, null);
                case "AdarI": return (6, null);
                case "AdarII":
                case "LastAdar":
                case "Nisan":
                case "Iyar":
                case "Sivan":
                case "Tammuz":
                case "Av":
                case "Elul":
                    return (0, value);
                default:
                    break;
            }
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-MONTH",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return (1, null);
    }

    /// <summary>
    /// Parses an enumeration value case-insensitively, falling back to a default when the value is absent or
    /// unrecognized.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed enumeration value, or <paramref name="fallback" />.</returns>
    public static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result)
            ? result
            : fallback;

    /// <summary>
    /// Parses an optional enumeration value case-insensitively.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed value, or <see langword="null" /> when absent or unrecognized.</returns>
    public static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result)
            ? result
            : null;

    /// <summary>
    /// Parses an integer value, falling back to a default when parsing fails.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed integer, or <paramref name="fallback" />.</returns>
    public static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;

    /// <summary>
    /// Parses an optional integer value.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed integer, or <see langword="null" /> when absent or invalid.</returns>
    public static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : null;

    /// <summary>
    /// Parses a boolean value expressed in XML-schema form (<c>true</c>/<c>false</c>/<c>1</c>/<c>0</c>), falling back
    /// to a default when parsing fails.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed boolean, or <paramref name="fallback" />.</returns>
    public static bool ParseBool(string? value, bool fallback)
    {
        if (value is null)
            return fallback;

        try
        {
            return XmlConvert.ToBoolean(value);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    /// <summary>
    /// Parses an optional boolean value expressed in XML-schema form.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed boolean, or <see langword="null" /> when absent or invalid.</returns>
    public static bool? ParseNullableBool(string? value)
    {
        if (value is null)
            return null;

        try
        {
            return XmlConvert.ToBoolean(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
