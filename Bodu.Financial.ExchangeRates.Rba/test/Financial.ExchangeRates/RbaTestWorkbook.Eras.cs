// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestWorkbook.Eras.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <content>
/// <para>
/// Generates the synthetic workbook of each era of <see cref="RbaEraWorkbook.Default" />: deterministic, laid out as
/// F11.1 was in that era, and holding rates of plausible magnitude that are not rates the RBA published.
/// </para>
/// <para>
/// Each era's data sheet has the columns F11.1 had then: 27 from 1983 to 1998, the last 11 of them legacy currencies
/// with no series identifier; the first 16 of those from 1999 to 2009; 25 from 2010 to 2013, whose title and
/// description cells are formulas; and the same less the riyal and the krona from 2014. Its rows are the weekdays of
/// the era, without New Year's Day and Christmas (or the weekdays that stand for them), to <see cref="LastDate" /> for
/// the open-ended era. Each rate is drawn from a function of its currency and date alone, in whole units of the last
/// decimal place the column shows, so every era is the same on every run and every platform.
/// </para>
/// <para>
/// The workbooks carry what the parser must cope with in the RBA's files. Closed days (Australia Day and Anzac Day) are
/// rows whose US dollar cell holds <c>Closed</c> or <c>CLOSED</c> and whose other cells are blank, from 1983 to 1994
/// and from 1999 to 2006; the other eras leave those days out. Single cells hold <see cref="MissingValuePlaceholder" />
/// to 1998 and <see cref="UnavailablePlaceholder" /> once in 2014-2017, every era has a pound sterling rate missing
/// with no placeholder at all, and zeros stand in the 1990s. Some 2010-2013 cells store a double one unit in the last
/// place from the published rate, among them the parser's documented example, the rand on 4 January 2010 stored as
/// <c>6.6754999999999995</c> for <c>6.6755</c>. Columns are empty, start late, end early, or stop and resume, and the
/// trade-weighted index and SDR columns stand in every era. The 2023-current workbook holds the rates the tests and
/// documentation name: US dollar <c>0.6828</c> on 3 January 2023, and on its last day, 12 June 2026, US dollar
/// <c>0.7029</c> and yen <c>112.71</c>.
/// </para>
/// </content>
internal static partial class RbaTestWorkbook
{
    /// <summary>The banner of the data sheet of the eras before 2014.</summary>
    public const string HistoricalBanner = "HISTORICAL DAILY EXCHANGE RATES OF THE AUSTRALIAN DOLLAR AGAINST:";

    /// <summary>The banner of the data sheet of the eras from 2014.</summary>
    public const string TableBanner = "F11.1  EXCHANGE RATES ";

    /// <summary>The units of the trade-weighted index column, which is not a currency.</summary>
    public const string IndexUnits = "Index";

    /// <summary>The placeholder a single cell holds for a missing rate in the eras before 1999.</summary>
    public const string MissingValuePlaceholder = " --";

    /// <summary>The placeholder the US dollar cell holds once in the 2014-2017 era.</summary>
    public const string UnavailablePlaceholder = "N/A";

    /// <summary>The row interval at which the eras before 1999 place a dash placeholder.</summary>
    private const int DashInterval = 200;

    /// <summary>
    /// The row, within each <see cref="DashInterval" />, at which the eras before 1999 place a dash placeholder.
    /// </summary>
    private const int DashOffset = 120;

    /// <summary>
    /// The row at which every era's pound sterling cell is a lone blank, a rate missing without a placeholder.
    /// </summary>
    private const int MissingRow = 250;

    /// <summary>The row interval at which some 2010-2013 cells store a double a unit in the last place away.</summary>
    private const int NoisyInterval = 41;

    /// <summary>
    /// The row, within each <see cref="NoisyInterval" />, at which a 2010-2013 cell stores a noisy double.
    /// </summary>
    private const int NoisyOffset = 7;

    /// <summary>
    /// The value columns of the 1983-1998 workbooks, by their units; the last 11 are legacy currencies.
    /// </summary>
    private static readonly string[] s_columns1983 =
    [
        "USD", IndexUnits, "EUR", "JPY", "GBP", "CHF", "NZD", "CAD", "HKD", "SGD", "MYR", "TWD", "KRW", "IDR", "CNY", "SDR",
        "DEM", "FRF", "ESP", "ITL", "NLG", "BEF", "ATS", "FIM", "PTE", "IEP", "GRD",
    ];

    /// <summary>The value columns of the 1999-2009 workbooks: the first 16 of the 1983-1998 columns.</summary>
    private static readonly string[] s_columns1999 = s_columns1983[..16];

    /// <summary>The value columns of the 2010-2013 workbook, by their units.</summary>
    private static readonly string[] s_columns2010 =
    [
        "USD", IndexUnits, "CNY", "JPY", "EUR", "KRW", "GBP", "SGD", "INR", "THB", "NZD", "TWD", "MYR", "IDR", "VND", "AED",
        "PGK", "HKD", "CAD", "ZAR", "SAR", "CHF", "SEK", "PHP", "SDR",
    ];

    /// <summary>
    /// The value columns of the workbooks from 2014: the 2010-2013 columns less the riyal and the krona.
    /// </summary>
    private static readonly string[] s_columns2014 = [.. s_columns2010.Where(static units => units is not ("SAR" or "SEK"))];

    /// <summary>The columns that take turns to hold a dash placeholder in the eras before 1999.</summary>
    private static readonly string[] s_dashColumns = ["DEM", "TWD", "CHF", "CAD", "IDR", "SDR"];

    /// <summary>The four-decimal columns of the 2010-2013 workbook some of whose cells store a noisy double.</summary>
    private static readonly string[] s_noisyColumns = ["CNY", "SGD", "NZD", "MYR", "AED", "PGK", "HKD", "ZAR", "SAR", "SEK"];

    /// <summary>The scale of a value with each number of decimal places, from zero to four.</summary>
    private static readonly long[] s_scales = [1, 10, 100, 1_000, 10_000];

    /// <summary>The header cells of each column, keyed by units, and the level about which its rates move.</summary>
    private static readonly Dictionary<string, (RbaTestColumn Template, decimal Level)> s_catalogue = CreateCatalogue();

    /// <summary>
    /// Gets the date of the last data row of the open-ended era's workbook.
    /// </summary>
    /// <value>
    /// 12 June 2026, a date in the past, so the latest rate the tests read is the same whenever they run.
    /// </value>
    public static DateOnly LastDate { get; } = new(2026, 6, 12);

    /// <summary>
    /// Builds the synthetic workbook of an era.
    /// </summary>
    /// <param name="era">An era of <see cref="RbaEraWorkbook.Default" />, matched by its label.</param>
    /// <returns>The bytes of the <c>.xls</c> compound file.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="era" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no era of the default catalog has the label of <paramref name="era" />.
    /// </exception>
    public static byte[] Build(RbaEraWorkbook era) =>
        Build(CreateSheet(era));

    /// <summary>
    /// Creates the content of the synthetic workbook of an era.
    /// </summary>
    /// <param name="era">An era of <see cref="RbaEraWorkbook.Default" />, matched by its label.</param>
    /// <returns>The content of the workbook, from which the tests compute what the parser should read.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="era" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no era of the default catalog has the label of <paramref name="era" />.
    /// </exception>
    public static RbaTestSheet CreateSheet(RbaEraWorkbook era)
    {
        ArgumentNullException.ThrowIfNull(era);

        string[] layout = GetColumns(era.Label);
        List<DateOnly> dates = GetDates(era, HasClosedRows(era.Label));
        RbaTestColumn[] columns = [.. layout.Select(units => CreateColumn(era.Label, units, dates))];

        return new RbaTestSheet
        {
            Banner = era.Start.Year < 2014 ? HistoricalBanner : TableBanner,
            NotesName = era.Label == "2007-2009" ? "Notes " : "Notes",
            HeaderFormulas = era.Label == "2010-2013",
            Dates = dates,
            Columns = columns,
        };
    }

    /// <summary>
    /// Returns the value columns of an era's workbook.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <returns>The units of each value column, in column order.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no era of the default catalog has <paramref name="label" />.
    /// </exception>
    private static string[] GetColumns(string label) => label switch
    {
        _ when IsBeforeEuro(label) => s_columns1983,
        "1999-2002" or "2003-2006" or "2007-2009" => s_columns1999,
        "2010-2013" => s_columns2010,
        "2014-2017" or "2018-2022" or "2023-current" => s_columns2014,
        _ => throw new ArgumentException($"No synthetic workbook is defined for the era '{label}'.", nameof(label)),
    };

    /// <summary>
    /// Returns the observation dates of an era: its weekdays, without New Year's Day and Christmas and the weekdays
    /// that stand for them, and without the closed days unless the era carries a row for them.
    /// </summary>
    /// <param name="era">The era.</param>
    /// <param name="closedRows">Whether the era carries a row for each closed day.</param>
    /// <returns>The dates, in order, to the era's end or to <see cref="LastDate" /> for the open-ended era.</returns>
    private static List<DateOnly> GetDates(RbaEraWorkbook era, bool closedRows)
    {
        DateOnly last = era.End ?? LastDate;
        List<DateOnly> dates = [];
        for (DateOnly date = era.Start; date <= last; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || IsHoliday(date) || (IsClosedDay(date) && !closedRows))
                continue;

            dates.Add(date);
        }

        return dates;
    }

    /// <summary>
    /// Reports whether a weekday is New Year's Day or Christmas, or a weekday that stands for one falling on a weekend.
    /// </summary>
    /// <param name="date">A weekday.</param>
    /// <returns><see langword="true" /> when no era has a row for <paramref name="date" />.</returns>
    private static bool IsHoliday(DateOnly date) => (date.Month, date.Day, date.DayOfWeek) switch
    {
        (1, 1, _) => true,
        (1, 2 or 3, DayOfWeek.Monday) => true,
        (12, 25 or 26, _) => true,
        (12, 27, DayOfWeek.Monday or DayOfWeek.Tuesday) => true,
        (12, 28, DayOfWeek.Tuesday) => true,
        _ => false,
    };

    /// <summary>
    /// Reports whether a weekday is a closed day, Australia Day or Anzac Day.
    /// </summary>
    /// <param name="date">A weekday.</param>
    /// <returns>
    /// <see langword="true" /> when the eras with closed rows carry a row of placeholders for <paramref name="date" />.
    /// </returns>
    private static bool IsClosedDay(DateOnly date) =>
        date is { Month: 1, Day: 26 } or { Month: 4, Day: 25 };

    /// <summary>
    /// Reports whether an era is one of the four before 1999, whose workbooks have 27 value columns.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <returns><see langword="true" /> for 1983-1986, 1987-1990, 1991-1994 and 1995-1998.</returns>
    private static bool IsBeforeEuro(string label) =>
        label is "1983-1986" or "1987-1990" or "1991-1994" or "1995-1998";

    /// <summary>
    /// Reports whether an era carries a row for each closed day.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <returns><see langword="true" /> for the eras to 2006 other than 1995-1998.</returns>
    private static bool HasClosedRows(string label) =>
        label is "1983-1986" or "1987-1990" or "1991-1994" or "1999-2002" or "2003-2006";

    /// <summary>
    /// Returns the text the US dollar cell of a closed day's row holds.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="date">The closed day.</param>
    /// <returns><c>CLOSED</c> in 1999-2002 and the even years of 2003-2006; otherwise <c>Closed</c>.</returns>
    private static string GetClosedText(string label, DateOnly date) =>
        label == "1999-2002" || (label == "2003-2006" && date.Year % 2 == 0) ? "CLOSED" : "Closed";

    /// <summary>
    /// Returns the dates within which a column of an era holds rates.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="units">The column's units.</param>
    /// <param name="first">The era's first observation date.</param>
    /// <param name="last">The era's last observation date.</param>
    /// <returns>
    /// The first and last dates of the column's rates, or <see langword="null" /> when it holds none.
    /// </returns>
    private static (DateOnly From, DateOnly To)? GetWindow(string label, string units, DateOnly first, DateOnly last) => (label, units) switch
    {
        ("1983-1986" or "1987-1990" or "1991-1994" or "1995-1998", "EUR") => null,
        ("1983-1986" or "1987-1990", "FIM") => null,
        ("1983-1986" or "1987-1990" or "1991-1994", "ESP" or "ATS" or "PTE" or "GRD" or "IEP") => null,
        ("1991-1994", "FIM") => (new DateOnly(1993, 4, 1), last),
        ("1995-1998", "ESP" or "ATS" or "PTE" or "GRD") => (new DateOnly(1998, 8, 3), last),
        ("2010-2013", "SEK") => (first, new DateOnly(2013, 10, 31)),
        ("2010-2013", "PHP") => (new DateOnly(2013, 11, 1), last),
        ("2014-2017", "ZAR") => (first, new DateOnly(2014, 10, 31)),
        ("2014-2017", "PHP") => (first, new DateOnly(2015, 10, 30)),
        ("2018-2022" or "2023-current", "ZAR") => null,
        ("2018-2022", "AED") => (first, new DateOnly(2020, 10, 30)),
        ("2018-2022", "PGK") => (first, new DateOnly(2022, 6, 30)),
        ("2018-2022", "CAD") => (first, new DateOnly(2022, 11, 30)),
        ("2018-2022", "PHP") => (new DateOnly(2022, 12, 1), last),
        ("2023-current", "AED") => null,
        ("2023-current", "PGK") => (new DateOnly(2026, 1, 5), last),
        ("2023-current", "CAD") => (new DateOnly(2023, 12, 1), last),
        _ => (first, last),
    };

    /// <summary>
    /// Reports whether a date falls in a pause of a column that stops and resumes within its era.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="units">The column's units.</param>
    /// <param name="date">The date.</param>
    /// <returns>
    /// <see langword="true" /> for the Swiss franc from March to August 2024; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsPaused(string label, string units, DateOnly date) =>
        label == "2023-current" && units == "CHF" && date >= new DateOnly(2024, 3, 1) && date <= new DateOnly(2024, 8, 30);

    /// <summary>
    /// Returns the text of a column's source cell in an era.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="units">The column's units.</param>
    /// <returns>
    /// <c>IMF</c> for the SDR, the US dollar's source of the era, and <c>RBA</c> for every other column.
    /// </returns>
    private static string GetSource(string label, string units) => units switch
    {
        "SDR" => "IMF",
        "USD" when label is "2007-2009" => "RBA and WM/Reuters",
        "USD" when label is "2010-2013" or "2014-2017" or "2018-2022" or "2023-current" => "WM/Reuters",
        _ => "RBA",
    };

    /// <summary>
    /// Creates a value column of an era's workbook.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="units">The column's units.</param>
    /// <param name="dates">The era's observation dates.</param>
    /// <returns>The column, with its header cells and one cell per date.</returns>
    private static RbaTestColumn CreateColumn(string label, string units, List<DateOnly> dates)
    {
        (RbaTestColumn template, decimal level) = s_catalogue[units];
        (DateOnly From, DateOnly To)? window = GetWindow(label, units, dates[0], dates[^1]);

        // A legacy column with no rate in the era has no records at all; any other column is formatted, so its empty
        // cells are blank records, as Excel writes the cells of a range that was formatted and left empty.
        RbaTestCell empty = window is null && template.SeriesId is null ? RbaTestCell.Absent : RbaTestCell.Blank;
        RbaTestCell[] cells = new RbaTestCell[dates.Count];
        for (int row = 0; row < dates.Count; row++)
        {
            DateOnly date = dates[row];
            if (HasClosedRows(label) && IsClosedDay(date))
                cells[row] = units == "USD" ? RbaTestCell.FromText(GetClosedText(label, date)) : RbaTestCell.Blank;
            else if (window is not { } span || date < span.From || date > span.To || IsPaused(label, units, date))
                cells[row] = empty;
            else
                cells[row] = CreateCell(label, units, template.Decimals, level, row, date);
        }

        return template with
        {
            Source = GetSource(label, units),
            PublicationDate = window is { } bounds && bounds.To < dates[^1] ? bounds.To : dates[^1],
            Cells = cells,
        };
    }

    /// <summary>
    /// Creates the cell of a column on a date within the column's rates, planting the values and placeholders the
    /// workbooks are built to carry.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <param name="units">The column's units.</param>
    /// <param name="decimals">The number of decimal places the column shows.</param>
    /// <param name="level">The level about which the column's rates move.</param>
    /// <param name="row">The zero-based index of the date among the era's observation dates.</param>
    /// <param name="date">The date.</param>
    /// <returns>The cell.</returns>
    private static RbaTestCell CreateCell(string label, string units, int decimals, decimal level, int row, DateOnly date)
    {
        switch ((label, units))
        {
            case ("2023-current", "USD") when date == new DateOnly(2023, 1, 3):
                return RbaTestCell.FromNumber(0.6828m);

            case ("2023-current", "USD") when date == LastDate:
                return RbaTestCell.FromNumber(0.7029m);

            case ("2023-current", "JPY") when date == LastDate:
                return RbaTestCell.FromNumber(112.71m);

            case ("2010-2013", "ZAR") when date == new DateOnly(2010, 1, 4):
                return RbaTestCell.FromNumber(6.6755m, 6.6754999999999995);

            case ("2010-2013", "SAR") when date == new DateOnly(2011, 2, 10):
                return RbaTestCell.FromNumber(3.7781m, 3.7781000000000002);

            case ("1991-1994", "TWD") when row == 333:
            case ("1991-1994", "SDR") when row == 666:
            case ("1995-1998", "SDR") when row is 444 or 445:
                return RbaTestCell.FromNumber(0m);

            case ("2014-2017", "USD") when row == 500:
                return RbaTestCell.FromText(UnavailablePlaceholder);

            case (_, "GBP") when row == MissingRow:
                return RbaTestCell.Blank;
        }

        if (IsBeforeEuro(label) && row % DashInterval == DashOffset && units == s_dashColumns[row / DashInterval % s_dashColumns.Length])
            return RbaTestCell.FromText(MissingValuePlaceholder);

        decimal value = CreateRate(units, level, decimals, date);
        if (label == "2010-2013" && row % NoisyInterval == NoisyOffset && s_noisyColumns.Contains(units))
            return RbaTestCell.FromNumber(value, NextDouble(value, up: row % 2 == 0));

        return RbaTestCell.FromNumber(value);
    }

    /// <summary>
    /// Returns the synthetic rate of a currency on a date.
    /// </summary>
    /// <param name="units">The currency's units, which seed its movements.</param>
    /// <param name="level">The level about which its rates move.</param>
    /// <param name="decimals">The number of decimal places its rates carry.</param>
    /// <param name="date">The date.</param>
    /// <returns>
    /// A rate within about 12 percent of <paramref name="level" />: two triangle waves of different periods and phases
    /// and a hashed jitter, computed in whole units of the last decimal place so that no rounding can differ.
    /// </returns>
    private static decimal CreateRate(string units, decimal level, int decimals, DateOnly date)
    {
        long scale = s_scales[decimals];
        long centre = (long)(level * scale);
        ulong seed = Seed(units);
        long day = date.DayNumber;

        long slow = Triangle(day + (long)(seed % 1_461), 1_461) * centre / 10_000;
        long fast = Triangle(day + (long)((seed >> 20) % 113), 113) * centre / 50_000;
        long jitter = ((long)(Mix(seed ^ (ulong)day) % 2_001) - 1_000) * centre / 400_000;

        return (centre + slow + fast + jitter) / (decimal)scale;
    }

    /// <summary>
    /// Returns the double one unit in the last place from the double nearest a rate, which still converts back to it.
    /// </summary>
    /// <param name="value">The published rate.</param>
    /// <param name="up">Whether to step towards positive infinity rather than towards zero.</param>
    /// <returns>The neighboring double.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the neighbor does not convert back to <paramref name="value" />.
    /// </exception>
    private static double NextDouble(decimal value, bool up)
    {
        double nearest = (double)value;
        double next = up ? Math.BitIncrement(nearest) : Math.BitDecrement(nearest);
        if ((decimal)next != value) throw new InvalidOperationException($"{next:R} does not convert back to {value}.");

        return next;
    }

    /// <summary>
    /// Returns a triangle wave that rises from -1000 to 1000 over the first half of each period and falls back over the
    /// second.
    /// </summary>
    /// <param name="x">The position.</param>
    /// <param name="period">The period.</param>
    /// <returns>The wave's value at <paramref name="x" />, from -1000 to 1000.</returns>
    private static long Triangle(long x, long period)
    {
        long phase = ((x % period) + period) % period;
        long half = period / 2;

        return phase < half
            ? -1_000 + (2_000 * phase / half)
            : 1_000 - (2_000 * (phase - half) / (period - half));
    }

    /// <summary>
    /// Returns the seed of a currency's movements: the 64-bit FNV-1a hash of its units.
    /// </summary>
    /// <param name="units">The units.</param>
    /// <returns>The seed, the same on every run.</returns>
    private static ulong Seed(string units)
    {
        ulong hash = 14_695_981_039_346_656_037;
        foreach (char ch in units)
            hash = unchecked((hash ^ ch) * 1_099_511_628_211);

        return hash;
    }

    /// <summary>
    /// Mixes a 64-bit value with the SplitMix64 finalizer, so neighboring inputs give unrelated outputs.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The mixed value.</returns>
    private static ulong Mix(ulong value)
    {
        unchecked
        {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EB;
            return value ^ (value >> 31);
        }
    }

    /// <summary>
    /// Creates the catalog of value columns: each column's header cells, keyed by its units, and the level about
    /// which its rates move.
    /// </summary>
    /// <returns>The catalog.</returns>
    private static Dictionary<string, (RbaTestColumn Template, decimal Level)> CreateCatalogue()
    {
        Dictionary<string, (RbaTestColumn Template, decimal Level)> catalogue = new(StringComparer.Ordinal)
        {
            [IndexUnits] = (new RbaTestColumn
            {
                Title = "Trade-weighted Index May 1970 = 100",
                Description = "Australian Dollar Trade-weighted Index",
                Units = IndexUnits,
                SeriesId = "FXRTWI",
                Decimals = 1,
            }, 60.0m),
        };

        AddCurrency(catalogue, "USD", "FXRUSD", 4, 0.7000m);
        AddCurrency(catalogue, "EUR", "FXREUR", 4, 0.6200m);
        AddCurrency(catalogue, "JPY", "FXRJY", 2, 95.00m);
        AddCurrency(catalogue, "GBP", "FXRUKPS", 4, 0.5000m);
        AddCurrency(catalogue, "CHF", "FXRSF", 4, 0.7500m);
        AddCurrency(catalogue, "NZD", "FXRNZD", 4, 1.1000m);
        AddCurrency(catalogue, "CAD", "FXRCD", 4, 0.9000m);
        AddCurrency(catalogue, "HKD", "FXRHKD", 4, 5.4000m);
        AddCurrency(catalogue, "SGD", "FXRSD", 4, 1.0000m);
        AddCurrency(catalogue, "MYR", "FXRMR", 4, 2.8000m);
        AddCurrency(catalogue, "TWD", "FXRNTD", 2, 22.00m);
        AddCurrency(catalogue, "KRW", "FXRSKW", 2, 820.00m);
        AddCurrency(catalogue, "IDR", "FXRIR", 0, 7600m);
        AddCurrency(catalogue, "CNY", "FXRCR", 4, 4.6000m);
        AddCurrency(catalogue, "SDR", "FXRSDR", 4, 0.4800m, "XDR");
        AddCurrency(catalogue, "INR", "FXRIRE", 2, 50.00m);
        AddCurrency(catalogue, "THB", "FXRTB", 2, 24.00m);
        AddCurrency(catalogue, "VND", "FXRVD", 0, 16000m);
        AddCurrency(catalogue, "AED", "FXRUAED", 4, 2.6000m);
        AddCurrency(catalogue, "PGK", "FXRPNGK", 4, 2.2000m);
        AddCurrency(catalogue, "ZAR", "FXRSARD", 4, 6.8000m);
        AddCurrency(catalogue, "SAR", "FXRSARY", 4, 3.5000m);
        AddCurrency(catalogue, "SEK", "FXRSK", 4, 6.2000m);
        AddCurrency(catalogue, "PHP", "FXRPHP", 2, 38.00m);

        // The legacy currencies of the 1983-1998 workbooks carry no series identifier.
        AddCurrency(catalogue, "DEM", null, 4, 1.2000m);
        AddCurrency(catalogue, "FRF", null, 4, 4.1000m);
        AddCurrency(catalogue, "ESP", null, 2, 95.00m);
        AddCurrency(catalogue, "ITL", null, 2, 1150.00m);
        AddCurrency(catalogue, "NLG", null, 4, 1.3500m);
        AddCurrency(catalogue, "BEF", null, 2, 25.00m);
        AddCurrency(catalogue, "ATS", null, 4, 8.4000m);
        AddCurrency(catalogue, "FIM", null, 4, 3.6000m);
        AddCurrency(catalogue, "PTE", null, 2, 105.00m);
        AddCurrency(catalogue, "IEP", null, 4, 0.4600m);
        AddCurrency(catalogue, "GRD", null, 2, 190.00m);

        return catalogue;
    }

    /// <summary>
    /// Adds a currency column to the catalog.
    /// </summary>
    /// <param name="catalogue">The catalog.</param>
    /// <param name="units">The currency's units, the RBA's label for it.</param>
    /// <param name="seriesId">The column's series identifier, or <see langword="null" /> for a legacy currency.</param>
    /// <param name="decimals">The number of decimal places the column shows.</param>
    /// <param name="level">The level about which the column's rates move.</param>
    /// <param name="currencyCode">
    /// The ISO 4217 code the parser resolves the units to, when it differs from them.
    /// </param>
    private static void AddCurrency(
        Dictionary<string, (RbaTestColumn Template, decimal Level)> catalogue,
        string units,
        string? seriesId,
        int decimals,
        decimal level,
        string? currencyCode = null)
    {
        RbaTestColumn template = new()
        {
            Title = "A$1=" + units,
            Description = units == "USD" ? "AUD/USD Exchange Rate; see notes for further detail." : $"AUD/{units} Exchange Rate",
            Units = units,
            SeriesId = seriesId,
            CurrencyCode = currencyCode ?? units,
            Decimals = decimals,
        };

        catalogue.Add(units, (template, level));
    }
}
