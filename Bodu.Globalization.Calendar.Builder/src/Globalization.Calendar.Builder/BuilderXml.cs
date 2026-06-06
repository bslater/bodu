// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuilderXml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides shared constants and helpers used by the XML and JSON serializers for the v2 notable-date document schema,
/// including the document namespace and English month-name conversions.
/// </summary>
internal static class BuilderXml
{
    /// <summary>
    /// The XML namespace URI of the v2 notable-date document schema.
    /// </summary>
    internal const string NamespaceUri = "urn:bodu:globalization:calendar";

    /// <summary>
    /// The default schema version emitted when a document does not specify one.
    /// </summary>
    internal const string DefaultSchemaVersion = "1.0";

    /// <summary>
    /// The reusable <see cref="XNamespace" /> for the v2 notable-date document schema.
    /// </summary>
    internal static readonly XNamespace Namespace = XNamespace.Get(NamespaceUri);

    /// <summary>
    /// The full English month names indexed one-based from January (index 1) to December (index 12).
    /// </summary>
    private static readonly string[] s_monthNames =
    [
        string.Empty,
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December",
    ];

    /// <summary>
    /// Converts a one-based month number into its full English month name.
    /// </summary>
    /// <param name="month">The one-based month number in the range 1 to 12.</param>
    /// <returns>The full English month name, or the original number formatted as a string when out of range.</returns>
    internal static string GetMonthName(int month) =>
        month is >= 1 and <= 12 ? s_monthNames[month] : month.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a month token expressed as either a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month token.</param>
    /// <returns>The one-based month number.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="value" /> is neither a recognized month name nor a valid month number.
    /// </exception>
    internal static int ParseMonth(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && numeric is >= 1 and <= 12)
            return numeric;

        for (var i = 1; i <= 12; i++)
        {
            if (string.Equals(s_monthNames[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, BuilderResourceStrings.Format_Invalid_XmlNotWellFormed, value));
    }

    /// <summary>
    /// Formats a Boolean value as the lower-case literal expected by the XML schema.
    /// </summary>
    /// <param name="value">The Boolean value to format.</param>
    /// <returns>The string <c>"true"</c> or <c>"false"</c>.</returns>
    internal static string Bool(bool value) =>
        value ? "true" : "false";

    /// <summary>
    /// Formats an integer using the invariant culture.
    /// </summary>
    /// <param name="value">The integer to format.</param>
    /// <returns>The invariant-culture string representation of <paramref name="value" />.</returns>
    internal static string Int(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a Boolean attribute value, accepting the XML schema literals <c>"true"</c>, <c>"false"</c>, <c>"1"</c>,
    /// and <c>"0"</c>.
    /// </summary>
    /// <param name="value">The raw attribute value, or <see langword="null" /> when absent.</param>
    /// <returns>
    /// The parsed Boolean value, or <see langword="null" /> when <paramref name="value" /> is <see langword="null" />.
    /// </returns>
    internal static bool? ParseBool(string? value) =>
        value is null
            ? null
            : value is "true" or "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses an integer attribute value using the invariant culture.
    /// </summary>
    /// <param name="value">The raw attribute value, or <see langword="null" /> when absent.</param>
    /// <returns>
    /// The parsed integer, or <see langword="null" /> when <paramref name="value" /> is <see langword="null" /> or
    /// blank.
    /// </returns>
    internal static int? ParseInt(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
}
