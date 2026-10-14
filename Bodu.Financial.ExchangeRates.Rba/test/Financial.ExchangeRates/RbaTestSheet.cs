// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestSheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes the content of a synthetic workbook in the layout of the Reserve Bank of Australia's statistical table
/// F11.1, from which <see cref="RbaTestWorkbook" /> builds the workbook.
/// </summary>
/// <remarks>
/// The data sheet holds the <see cref="Banner" /> in row 0, the labeled <see cref="HeaderRows" /> at their F11.1
/// positions in rows 1 to 10 (rows 6 and 7 blank), and from <see cref="FirstDataRow" /> one row per entry of
/// <see cref="Dates" />: the date's serial in column 0 and each column's cell from column 1.
/// </remarks>
internal sealed record RbaTestSheet
{
    /// <summary>The zero-based row of the data sheet that holds the first dated row.</summary>
    public const int FirstDataRow = 11;

    /// <summary>
    /// The day before serial 1 of the 1900 date system, from which the serial of every later date counts.
    /// </summary>
    private static readonly DateOnly s_serialEpoch = new(1899, 12, 30);

    /// <summary>
    /// Gets the name of the data sheet.
    /// </summary>
    /// <value>The sheet name; <c>Data</c> unless set.</value>
    public string Name { get; init; } = "Data";

    /// <summary>
    /// Gets the name of the notes sheet that follows the data sheet.
    /// </summary>
    /// <value>The sheet name; <c>Notes</c> unless set.</value>
    public string NotesName { get; init; } = "Notes";

    /// <summary>
    /// Gets the text of the banner in the first cell of the data sheet.
    /// </summary>
    /// <value>The banner, or <see langword="null" /> to leave row 0 empty.</value>
    public string? Banner { get; init; }

    /// <summary>
    /// Gets the labeled header rows the data sheet carries.
    /// </summary>
    /// <value>The header rows; <see cref="RbaTestHeaderRows.All" /> unless set.</value>
    public RbaTestHeaderRows HeaderRows { get; init; } = RbaTestHeaderRows.All;

    /// <summary>
    /// Gets a value indicating whether the title and description cells of the value columns are formulas.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to write each as a <c>FORMULA</c> record whose cached text a <c>STRING</c> record
    /// carries, as the RBA's 2010-2013 workbook does; <see langword="false" /> to write each through the shared string
    /// table.
    /// </value>
    public bool HeaderFormulas { get; init; }

    /// <summary>
    /// Gets the observation dates, one per data row in order.
    /// </summary>
    /// <value>The dates of the data rows.</value>
    public required IReadOnlyList<DateOnly> Dates { get; init; }

    /// <summary>
    /// Gets the serials written in column 0 of the data rows in place of the serials of <see cref="Dates" />.
    /// </summary>
    /// <value>
    /// One serial per data row, which may be one no date represents, or <see langword="null" /> to write the serial of
    /// each date.
    /// </value>
    public IReadOnlyList<double>? DateSerials { get; init; }

    /// <summary>
    /// Gets the value columns, which occupy the data sheet from column 1 in order.
    /// </summary>
    /// <value>The value columns.</value>
    public required IReadOnlyList<RbaTestColumn> Columns { get; init; }

    /// <summary>
    /// Returns the serial a date has in the 1900 date system.
    /// </summary>
    /// <param name="date">A date after 28 February 1900.</param>
    /// <returns>The number of days from 30 December 1899 to <paramref name="date" />.</returns>
    public static double ToSerial(DateOnly date) =>
        date.DayNumber - s_serialEpoch.DayNumber;

    /// <summary>
    /// Returns the serial written in column 0 of a data row.
    /// </summary>
    /// <param name="index">The zero-based index of the data row.</param>
    /// <returns>
    /// The serial of the row's date, or the row's entry of <see cref="DateSerials" /> when that is set.
    /// </returns>
    public double GetDateSerial(int index) =>
        DateSerials is { } serials ? serials[index] : ToSerial(Dates[index]);
}
