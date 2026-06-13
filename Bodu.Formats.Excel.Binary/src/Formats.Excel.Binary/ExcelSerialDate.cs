// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Converts Excel serial date numbers into calendar dates.
/// </summary>
/// <remarks>
/// <para>
/// Excel stores dates as floating-point serial numbers measured from the epoch <c>1899-12-30</c>, deliberately
/// preserving the historical 1900 leap-year bug so that values round-trip with Excel. The reader never infers that a
/// numeric cell is a date; this helper is provided so a caller that knows a column holds dates can convert its values.
/// </para>
/// </remarks>
public static class ExcelSerialDate
{
    /// <summary>
    /// Converts an Excel serial date number to a <see cref="DateOnly" />, discarding any fractional time-of-day
    /// component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <returns>The calendar date represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateOnly FromSerialDate(double serial) =>
        DateOnly.FromDateTime(DateTime.FromOADate(serial));

    /// <summary>
    /// Converts an Excel serial date number to a <see cref="DateTime" />, preserving any fractional time-of-day
    /// component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <returns>The date and time represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateTime ToDateTime(double serial) =>
        DateTime.FromOADate(serial);
}
