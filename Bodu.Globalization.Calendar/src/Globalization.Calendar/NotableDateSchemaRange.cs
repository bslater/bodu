// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies a range the notable-date XML schema holds an integer to, which the JSON reader, validated by no schema,
/// checks itself.
/// </summary>
internal enum NotableDateSchemaRange
{
    /// <summary>
    /// The schema's <c>positiveInteger</c> type, read as a 32-bit integer: a day count, an interval, or a number of
    /// days or years.
    /// </summary>
    PositiveInteger,

    /// <summary>
    /// The schema's <c>unsignedShort</c> type: a year from 0 to 65535.
    /// </summary>
    UnsignedShort,

    /// <summary>
    /// The schema's <c>dayValue</c> type: a day of the month from 1 to 31.
    /// </summary>
    DayValue,

    /// <summary>
    /// The schema's <c>nonZeroOrdinal</c> type: an ordinal from -366 to 366, which its pattern holds to a value other
    /// than zero written with no plus sign or leading zero.
    /// </summary>
    NonZeroOrdinal,

    /// <summary>
    /// The schema's <c>isoWeekValue</c> type: an ISO week from 1 to 53.
    /// </summary>
    IsoWeekValue,
}
