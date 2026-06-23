// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

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
    /// <summary>The day offset between the 1904 and 1900 date systems.</summary>
    private const double Excel1904Offset = 1462.0;

    /// <summary>
    /// Converts an Excel serial date number in the 1900 date system to a <see cref="DateOnly" />, discarding any
    /// fractional time-of-day component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <returns>The calendar date represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateOnly FromSerialDate(double serial) =>
        FromSerialDate(serial, ExcelDateSystem.Excel1900);

    /// <summary>
    /// Converts an Excel serial date number to a <see cref="DateOnly" /> using the specified date system, discarding
    /// any fractional time-of-day component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <param name="dateSystem">The date system that establishes the serial number's epoch.</param>
    /// <returns>The calendar date represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateOnly FromSerialDate(double serial, ExcelDateSystem dateSystem) =>
        DateOnly.FromDateTime(ToDateTime(serial, dateSystem));

    /// <summary>
    /// Converts an Excel serial date number in the 1900 date system to a <see cref="DateTime" />, preserving any
    /// fractional time-of-day component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <returns>The date and time represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateTime ToDateTime(double serial) =>
        ToDateTime(serial, ExcelDateSystem.Excel1900);

    /// <summary>
    /// Converts an Excel serial date number to a <see cref="DateTime" /> using the specified date system, preserving
    /// any fractional time-of-day component.
    /// </summary>
    /// <param name="serial">The Excel serial date number.</param>
    /// <param name="dateSystem">The date system that establishes the serial number's epoch.</param>
    /// <returns>The date and time represented by <paramref name="serial" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serial" /> is outside the range of representable OLE Automation dates.
    /// </exception>
    public static DateTime ToDateTime(double serial, ExcelDateSystem dateSystem) =>
        DateTime.FromOADate(dateSystem == ExcelDateSystem.Excel1904 ? serial + Excel1904Offset : serial);
}
