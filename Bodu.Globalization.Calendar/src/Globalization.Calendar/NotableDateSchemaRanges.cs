// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRanges.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Holds the bounds the notable-date XML schema holds integers to, as the schema declares them, so the JSON reader and
/// the document builder report a value outside them as the schema reports it in XML.
/// </summary>
internal static class NotableDateSchemaRanges
{
    /// <summary>The least value of the schema's <c>nonZeroOrdinal</c> type, shared with the strategy constructors.</summary>
    internal const int MinimumOrdinal = -366;

    /// <summary>The greatest value of the schema's <c>nonZeroOrdinal</c> type, shared with the strategy constructors.</summary>
    internal const int MaximumOrdinal = 366;

    /// <summary>
    /// Returns the least value a range allows.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The least value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="range" /> names no range.</exception>
    internal static int GetMinimum(NotableDateSchemaRange range) =>
        range switch
        {
            NotableDateSchemaRange.PositiveInteger => 1,
            NotableDateSchemaRange.UnsignedShort => 0,
            NotableDateSchemaRange.DayValue => 1,
            NotableDateSchemaRange.NonZeroOrdinal => MinimumOrdinal,
            NotableDateSchemaRange.IsoWeekValue => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(range)),
        };

    /// <summary>
    /// Returns the greatest value a range allows.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>
    /// The greatest value; <see cref="int.MaxValue" /> for the schema's unbounded <c>positiveInteger</c>, which the
    /// reader reads as a 32-bit integer.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="range" /> names no range.</exception>
    internal static int GetMaximum(NotableDateSchemaRange range) =>
        range switch
        {
            NotableDateSchemaRange.PositiveInteger => int.MaxValue,
            NotableDateSchemaRange.UnsignedShort => ushort.MaxValue,
            NotableDateSchemaRange.DayValue => 31,
            NotableDateSchemaRange.NonZeroOrdinal => MaximumOrdinal,
            NotableDateSchemaRange.IsoWeekValue => 53,
            _ => throw new ArgumentOutOfRangeException(nameof(range)),
        };

    /// <summary>
    /// Checks an integer against a range, returning the <c>BODU-CAL-SCHEMA</c> error for a value outside it.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="range">The range the schema holds the value to.</param>
    /// <param name="field">The name of the JSON property the value was read from, used in the diagnostic.</param>
    /// <returns>The diagnostic, or <see langword="null" /> when the value lies within the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="range" /> names no range.</exception>
    /// <remarks>
    /// Only the bounds are checked; the <see cref="NotableDateSchemaPattern.NonZeroOrdinal" /> pattern, which rejects
    /// zero, is checked against the value's text.
    /// </remarks>
    internal static NotableDateValidationDiagnostic? Check(int value, NotableDateSchemaRange range, string field)
    {
        int minimum = GetMinimum(range);
        int maximum = GetMaximum(range);

        if (value < minimum)
            return new(NotableDateValidationSeverity.Error, "BODU-CAL-SCHEMA", string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaValueBelowMinimum, field, value, minimum));

        if (value > maximum)
            return new(NotableDateValidationSeverity.Error, "BODU-CAL-SCHEMA", string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaValueAboveMaximum, field, value, maximum));

        return null;
    }
}
