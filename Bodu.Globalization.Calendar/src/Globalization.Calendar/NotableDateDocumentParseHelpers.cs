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
    /// <summary>The full English month names, indexed so that January is at index zero.</summary>
    private static readonly string[] s_monthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    /// <summary>
    /// Parses a Sunday-first seven-character binary working-week pattern, falling back to the default working week when
    /// the value is absent or blank, and recording an error diagnostic when it is not such a pattern.
    /// </summary>
    /// <param name="value">The working-week pattern, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed pattern.</param>
    /// <returns>
    /// The parsed <see cref="DayOfWeekSet" />, or <see langword="null" /> when unspecified or malformed. A malformed
    /// pattern has then recorded a <c>BODU-CAL-WORKING-DAYS</c> error, so the default never reaches a loaded resource.
    /// </returns>
    public static DayOfWeekSet? ParseWorkingWeek(string? value, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Only the binary form the XSD and the JSON schema define is accepted; the letter masks DayOfWeekSet also reads
        // are not part of the document format.
        if (DayOfWeekSet.TryParseExact(value, "0", out DayOfWeekSet workingWeek))
            return workingWeek;

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-WORKING-DAYS",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidWorkingDays, value)));

        return null;
    }

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
    /// Parses an enumeration value by member name, ignoring case, falling back to a default when the value is absent
    /// and recording an error diagnostic when it names no member.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="fallback">The value returned when the value is absent or names no member.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for an unrecognized value.</param>
    /// <returns>The named member, or <paramref name="fallback" />.</returns>
    public static TEnum ParseEnum<TEnum>(string? value, TEnum fallback, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
        where TEnum : struct, Enum =>
        ParseNullableEnum<TEnum>(value, field, diagnostics) ?? fallback;

    /// <summary>
    /// Parses an optional enumeration value by member name, ignoring case, recording an error diagnostic when a present
    /// value names no member.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for an unrecognized value.</param>
    /// <returns>
    /// The named member, or <see langword="null" /> when the value is absent or names no member. A value that names no
    /// member has then recorded a <c>BODU-CAL-ENUM</c> error, so the absent value never reaches a loaded resource.
    /// </returns>
    /// <remarks>
    /// Only a member's name is recognized: a number, a comma-separated list, or surrounding white space is not, as the
    /// XSD and the JSON schema accept none of them.
    /// </remarks>
    public static TEnum? ParseNullableEnum<TEnum>(string? value, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
        where TEnum : struct, Enum
    {
        if (value is null)
            return null;

        string[] names = Enum.GetNames<TEnum>();
        foreach (string name in names)
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                return Enum.Parse<TEnum>(name);
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-ENUM",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidEnumValue, field, value, string.Join(", ", names))));

        return null;
    }

    /// <summary>
    /// Parses a list of enumeration values by member name, ignoring case, substituting a default for, and recording an
    /// error diagnostic for, each value that names no member.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="values">The raw values.</param>
    /// <param name="fallback">The value substituted for a value that names no member.</param>
    /// <param name="field">The name of the field the values belong to, used in the diagnostics.</param>
    /// <param name="diagnostics">The collection that receives the diagnostics for unrecognized values.</param>
    /// <returns>The parsed values, in order.</returns>
    public static List<TEnum> ParseEnumList<TEnum>(IEnumerable<string> values, TEnum fallback, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
        where TEnum : struct, Enum =>
        [.. values.Select(value => ParseEnum(value, fallback, field, diagnostics))];

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
