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
    /// <summary>The Hebrew month names in their canonical spelling, each with its number, or <c>0</c> for a name whose number shifts in a leap year and is read as a month alias.</summary>
    private static readonly (string Name, int Month)[] s_hebrewMonthNames =
    [
        ("Tishri", 1), ("Heshvan", 2), ("Kislev", 3), ("Tevet", 4), ("Shevat", 5), ("AdarI", 6),
        ("AdarII", 0), ("LastAdar", 0), ("Nisan", 0), ("Iyar", 0), ("Sivan", 0), ("Tammuz", 0), ("Av", 0), ("Elul", 0),
    ];

    /// <summary>The full English month names, indexed so that January is at index zero.</summary>
    private static readonly string[] s_monthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    /// <summary>The names of the override operations, as the schema writes them.</summary>
    private static readonly string[] s_overrideOperations = ["AddRule", "PatchRule", "RemoveRule"];

    /// <summary>
    /// Parses the name of an override operation, ignoring case as every other name is matched, recording an error
    /// diagnostic when it names no operation.
    /// </summary>
    /// <param name="value">The operation's name: an XML element's local name or a JSON <c>operation</c> value.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for an unrecognized name.</param>
    /// <returns>
    /// <c>AddRule</c>, <c>PatchRule</c> or <c>RemoveRule</c>, as the schema writes it, or <see langword="null" /> when
    /// <paramref name="value" /> names no operation. A <c>BODU-CAL-ENUM</c> error has then been recorded, so the
    /// override the caller leaves out never goes unreported.
    /// </returns>
    public static string? ParseOverrideOperation(string value, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (string operation in s_overrideOperations)
        {
            if (string.Equals(operation, value, StringComparison.OrdinalIgnoreCase))
                return operation;
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-ENUM",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidEnumValue, "override operation", value, string.Join(", ", s_overrideOperations))));

        return null;
    }

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
    /// Parses a trigger comparison month expressed as a full English month name or an integer between 1 and 12,
    /// recording an error diagnostic when a present value is neither.
    /// </summary>
    /// <param name="value">The raw month value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>
    /// The one-based month, or <see langword="null" /> when the value is absent or malformed. A malformed value has
    /// then recorded a <c>BODU-CAL-VALUE</c> error, so the absent month never reaches a loaded resource.
    /// </returns>
    public static int? ParseTriggerMonth(string? value, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is null)
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
            return numeric;

        int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            return index + 1;

        AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidTriggerMonth, field, value);
        return null;
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
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// A tuple of the one-based month (or <c>0</c> when an alias supplies it) and an optional Hebrew month alias.
    /// </returns>
    /// <remarks>
    /// A Gregorian month is a full English month name or an integer from 1 to 12. In another calendar it is an integer
    /// from 1 to the number <see cref="CalendarSystems.GetFixedMonthCount" /> gives for the calendar, and, in the
    /// Hebrew calendar alone, a Hebrew month name. A name matches ignoring case. Any other value records
    /// <c>BODU-CAL-MONTH</c>, worded for the calendar.
    /// </remarks>
    public static (int Month, string? Alias) ParseFixedMonth(
        string? value,
        CalendarSystem calendar,
        bool skipLeapMonth,
        string notableDateId,
        string ruleId,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (calendar == CalendarSystem.Gregorian)
            return (ParseMonth(value, notableDateId, ruleId, diagnostics), null);

        int months = CalendarSystems.GetFixedMonthCount(calendar, skipLeapMonth);
        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric >= 1 && numeric <= months)
                return (numeric, null);

            // The month names, and the aliases whose number shifts in a leap year, are the Hebrew calendar's alone. They
            // match ignoring case, as the English month names and the enumeration values do, and an alias is kept in
            // its canonical spelling, which the strategy and the writers hold.
            if (calendar == CalendarSystem.Hebrew)
            {
                foreach ((string name, int number) in s_hebrewMonthNames)
                {
                    if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                        return number > 0 ? (number, null) : (0, name);
                }
            }
        }

        string message = calendar == CalendarSystem.Hebrew
            ? string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidHebrewMonthValue, notableDateId, ruleId, value ?? string.Empty)
            : string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidCalendarMonthValue, notableDateId, ruleId, value ?? string.Empty, calendar, months);

        diagnostics.Add(new NotableDateValidationDiagnostic(NotableDateValidationSeverity.Error, "BODU-CAL-MONTH", message));
        return (1, null);
    }

    /// <summary>
    /// Checks a fixed-strategy day against the most days its month can have in its calendar, recording
    /// <c>BODU-CAL-DAY</c> for a day beyond them.
    /// </summary>
    /// <param name="day">The day read from the document.</param>
    /// <param name="month">The one-based month, or <c>0</c> when a Hebrew month alias supplies it.</param>
    /// <param name="calendar">The calendar system the month and day are expressed in.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// <paramref name="day" />, or 1 when it is beyond the month's days. The caller has then recorded an error, so the
    /// substitute never reaches a loaded resource.
    /// </returns>
    /// <remarks>
    /// The bound is <see cref="CalendarSystems.GetFixedDayCount" />, which <see cref="Algorithms.FixedDateStrategy" />
    /// also holds a day to, so a day such as February 30 is reported here, as <c>BODU-CAL-DAY</c>, rather than by the
    /// constructor. A day outside 1 to 31 is returned unchanged, for the constructor to refuse.
    /// </remarks>
    public static int CheckFixedDay(
        int day,
        int month,
        CalendarSystem calendar,
        string notableDateId,
        string ruleId,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (day > 31 || day <= CalendarSystems.GetFixedDayCount(calendar, month))
            return day;

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-DAY",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidDayValue, notableDateId, ruleId, day, month)));

        return 1;
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
    /// Parses an integer value, falling back to a default when the value is absent and recording an error diagnostic
    /// when a present value is not a 32-bit integer.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="fallback">The value returned when the value is absent or malformed.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>The parsed integer, or <paramref name="fallback" />.</returns>
    public static int ParseInt(string? value, int fallback, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ParseNullableInt(value, field, diagnostics) ?? fallback;

    /// <summary>
    /// Parses an optional integer value, recording an error diagnostic when a present value is not a 32-bit integer.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>
    /// The parsed integer, or <see langword="null" /> when the value is absent or malformed. A malformed value has then
    /// recorded a <c>BODU-CAL-VALUE</c> error, so the absent value never reaches a loaded resource.
    /// </returns>
    /// <remarks>
    /// The value is read with the invariant culture, allowing a leading sign and surrounding white space, as the
    /// schema's integer types do.
    /// </remarks>
    public static int? ParseNullableInt(string? value, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is null)
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;

        AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidIntegerValue, field, value);
        return null;
    }

    /// <summary>
    /// Parses a Boolean value expressed in XML-schema form (<c>true</c>/<c>false</c>/<c>1</c>/<c>0</c>), falling back
    /// to a default when the value is absent and recording an error diagnostic when a present value is not such a
    /// literal.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="fallback">The value returned when the value is absent or malformed.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>The parsed Boolean, or <paramref name="fallback" />.</returns>
    public static bool ParseBool(string? value, bool fallback, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ParseNullableBool(value, field, diagnostics) ?? fallback;

    /// <summary>
    /// Parses an optional Boolean value expressed in XML-schema form, recording an error diagnostic when a present
    /// value is not such a literal.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>
    /// The parsed Boolean, or <see langword="null" /> when the value is absent or malformed. A malformed value has then
    /// recorded a <c>BODU-CAL-VALUE</c> error, so the absent value never reaches a loaded resource.
    /// </returns>
    public static bool? ParseNullableBool(string? value, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is null)
            return null;

        try
        {
            return XmlConvert.ToBoolean(value);
        }
        catch (FormatException)
        {
            AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidBooleanValue, field, value);
            return null;
        }
    }

    /// <summary>
    /// Parses an optional ISO-8601 <c>yyyy-MM-dd</c> date, recording an error diagnostic when a present value is not
    /// such a date.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for a malformed value.</param>
    /// <returns>
    /// The parsed date, or <see langword="null" /> when the value is absent or malformed. A malformed value has then
    /// recorded a <c>BODU-CAL-VALUE</c> error, so the absent value never reaches a loaded resource.
    /// </returns>
    public static DateOnly? ParseNullableDate(string? value, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is null)
            return null;

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result))
            return result;

        AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidDateValue, field, value);
        return null;
    }

    /// <summary>
    /// Records the <c>BODU-CAL-VALUE</c> error diagnostic for a present value that does not parse as its field's type.
    /// </summary>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    /// <param name="format">The resource format string, taking the field name and the value.</param>
    /// <param name="field">The name of the field the value belongs to.</param>
    /// <param name="value">The malformed value.</param>
    public static void AddValueError(ICollection<NotableDateValidationDiagnostic> diagnostics, string format, string field, string value) =>
        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-VALUE",
            string.Format(CultureInfo.CurrentCulture, format, field, value)));
}
