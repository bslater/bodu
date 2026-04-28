// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

/// <summary>
/// Contains unit tests for <see cref="FiscalWeekQuarterProvider" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FiscalWeekQuarterProvider" /> describes a recurring fiscal calendar rule; year-specific
/// values (fiscal year start, 53-week detection, quarter boundaries) are computed on demand from
/// either an explicit <c>fiscalYear</c> argument or from the input date via an internal cross-year
/// search. Dates near a fiscal year boundary therefore resolve to the correct neighbouring fiscal
/// year rather than throwing.
/// </para>
/// <para>
/// Four providers are defined to give broad scenario coverage:
/// </para>
/// <list type="bullet">
/// <item>
/// <term><see cref="Sunday52" /></term>
/// <description>
/// 52-week Sunday-start rule; when applied to fiscal year 2023 the year begins 1 January 2023 (a Sunday).
/// </description>
/// </item>
/// <item>
/// <term><see cref="Monday52Leap" /></term>
/// <description>
/// 52-week Monday-start rule; fiscal year 2024 begins 1 January 2024 (a Monday). 2024 is a leap year.
/// </description>
/// </item>
/// <item>
/// <term><see cref="Sunday53" /></term>
/// <description>
/// 53-week Sunday-start rule; fiscal year 2020 begins 29 December 2019 and ends 2 January 2021
/// (Q4 spans 14 weeks).
/// </description>
/// </item>
/// <item>
/// <term><see cref="Saturday52" /></term>
/// <description>
/// 52-week Saturday-start rule; fiscal year 2023 begins 1 April 2023 and ends 29 March 2024, straddling
/// the calendar year.
/// </description>
/// </item>
/// </list>
/// </remarks>
[TestClass]
public partial class FiscalWeekQuarterProviderTests
{
    // FY 2023 — Jan 1, 2023 = Sunday — alignment is exact; no ambiguity.
    // FY 2023: 2023-01-01 – 2023-12-30 | 52 weeks | Weeks544
    // Q1: 2023-01-01 – 2023-04-01  Q2: 2023-04-02 – 2023-07-01
    // Q3: 2023-07-02 – 2023-09-30  Q4: 2023-10-01 – 2023-12-30
    private const int Sunday52FiscalYear = 2023;

    private static readonly FiscalWeekQuarterProvider Sunday52 =
        new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks544);

    // FY 2024 — Jan 1, 2024 = Monday — alignment is exact; no ambiguity.
    // 2024 is a leap year (contains Feb 29). FY 2024: 2024-01-01 – 2024-12-29 | 52 weeks | Weeks445
    // Q1: 2024-01-01 – 2024-03-31  Q2: 2024-04-01 – 2024-06-30
    // Q3: 2024-07-01 – 2024-09-29  Q4: 2024-09-30 – 2024-12-29
    private const int Monday52LeapFiscalYear = 2024;

    private static readonly FiscalWeekQuarterProvider Monday52Leap =
        new FiscalWeekQuarterProvider(1, DayOfWeek.Monday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks445);

    // FY 2020 — Jan 1, 2020 = Wednesday — nearest Sunday is Dec 29, 2019 (3 days earlier).
    // 2020 is a leap year. FY 2020: 2019-12-29 – 2021-01-02 | 53 weeks | Weeks445
    // Q1: 2019-12-29 – 2020-03-28  Q2: 2020-03-29 – 2020-06-27
    // Q3: 2020-06-28 – 2020-09-26  Q4: 2020-09-27 – 2021-01-02  (14 weeks)
    private const int Sunday53FiscalYear = 2020;

    private static readonly FiscalWeekQuarterProvider Sunday53 =
        new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks445);

    // FY 2023 — Apr 1, 2023 = Saturday — alignment is exact; no ambiguity.
    // Straddles calendar year; Q4 falls in a leap year (2024). FY 2023: 2023-04-01 – 2024-03-29 | 52 weeks | Weeks454
    // Q1: 2023-04-01 – 2023-06-30  Q2: 2023-07-01 – 2023-09-29
    // Q3: 2023-09-30 – 2023-12-29  Q4: 2023-12-30 – 2024-03-29
    private const int Saturday52FiscalYear = 2023;

    private static readonly FiscalWeekQuarterProvider Saturday52 =
        new FiscalWeekQuarterProvider(4, DayOfWeek.Saturday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks454);

    /// <summary>
    /// Provides quarter number test cases: (provider, date, expectedQuarter).
    /// </summary>
    /// <remarks>
    /// Includes mid-quarter dates, the first day of each quarter, and the last day of each
    /// quarter (including Q4). Leap day (29 February) is explicitly covered.
    /// </remarks>
    public static IEnumerable<object[]> GetQuarterNumberTestData()
    {
        // Sunday52 — Q1 through Q3 use full boundaries; Q4 uses week-start day
        yield return new object[] { Sunday52, new DateTime(2023, 2, 15), 1 }; // mid-Q1
        yield return new object[] { Sunday52, new DateTime(2023, 1, 1), 1 }; // Q1 first day (Sunday)
        yield return new object[] { Sunday52, new DateTime(2023, 4, 1), 1 }; // Q1 last day (Saturday)
        yield return new object[] { Sunday52, new DateTime(2023, 5, 15), 2 }; // mid-Q2
        yield return new object[] { Sunday52, new DateTime(2023, 4, 2), 2 }; // Q2 first day
        yield return new object[] { Sunday52, new DateTime(2023, 7, 1), 2 }; // Q2 last day (Saturday)
        yield return new object[] { Sunday52, new DateTime(2023, 8, 20), 3 }; // mid-Q3
        yield return new object[] { Sunday52, new DateTime(2023, 7, 2), 3 }; // Q3 first day
        yield return new object[] { Sunday52, new DateTime(2023, 9, 30), 3 }; // Q3 last day (Saturday)
        yield return new object[] { Sunday52, new DateTime(2023, 11, 15), 4 }; // mid-Q4
        yield return new object[] { Sunday52, new DateTime(2023, 10, 1), 4 }; // Q4 first day (Sunday)
        yield return new object[] { Sunday52, new DateTime(2023, 12, 24), 4 }; // first day of final fiscal week (Sun)
        yield return new object[] { Sunday52, new DateTime(2023, 12, 30), 4 }; // Q4 last day (Saturday)

        // Monday52Leap — Q1 last day is Sunday (always valid); Q4 uses final week start (Mon 23 Dec)
        yield return new object[] { Monday52Leap, new DateTime(2024, 2, 29), 1 }; // leap day in Q1
        yield return new object[] { Monday52Leap, new DateTime(2024, 1, 1), 1 }; // Q1 first day (Monday)
        yield return new object[] { Monday52Leap, new DateTime(2024, 3, 31), 1 }; // Q1 last day (Sunday)
        yield return new object[] { Monday52Leap, new DateTime(2024, 5, 15), 2 }; // mid-Q2
        yield return new object[] { Monday52Leap, new DateTime(2024, 4, 1), 2 }; // Q2 first day
        yield return new object[] { Monday52Leap, new DateTime(2024, 6, 30), 2 }; // Q2 last day (Sunday)
        yield return new object[] { Monday52Leap, new DateTime(2024, 8, 10), 3 }; // mid-Q3
        yield return new object[] { Monday52Leap, new DateTime(2024, 7, 1), 3 }; // Q3 first day
        yield return new object[] { Monday52Leap, new DateTime(2024, 9, 29), 3 }; // Q3 last day (Sunday)
        yield return new object[] { Monday52Leap, new DateTime(2024, 11, 5), 4 }; // mid-Q4
        yield return new object[] { Monday52Leap, new DateTime(2024, 9, 30), 4 }; // Q4 first day (Monday)
        yield return new object[] { Monday52Leap, new DateTime(2024, 12, 23), 4 }; // first day of final fiscal week (Mon)
        yield return new object[] { Monday52Leap, new DateTime(2024, 12, 29), 4 }; // Q4 last day (Sunday)

        // Sunday53 — FY spans two calendar years; Dec 27, 2020 is the first day of week 53 (Sunday)
        yield return new object[] { Sunday53, new DateTime(2019, 12, 29), 1 }; // FY first day (prior calendar year)
        yield return new object[] { Sunday53, new DateTime(2020, 2, 29), 1 }; // leap day in Q1
        yield return new object[] { Sunday53, new DateTime(2020, 3, 28), 1 }; // Q1 last day (Saturday)
        yield return new object[] { Sunday53, new DateTime(2020, 3, 29), 2 }; // Q2 first day
        yield return new object[] { Sunday53, new DateTime(2020, 5, 10), 2 }; // mid-Q2
        yield return new object[] { Sunday53, new DateTime(2020, 6, 27), 2 }; // Q2 last day (Saturday)
        yield return new object[] { Sunday53, new DateTime(2020, 6, 28), 3 }; // Q3 first day
        yield return new object[] { Sunday53, new DateTime(2020, 8, 15), 3 }; // mid-Q3
        yield return new object[] { Sunday53, new DateTime(2020, 9, 26), 3 }; // Q3 last day (Saturday)
        yield return new object[] { Sunday53, new DateTime(2020, 9, 27), 4 }; // Q4 first day (Sunday)
        yield return new object[] { Sunday53, new DateTime(2020, 12, 25), 4 }; // mid-Q4
        yield return new object[] { Sunday53, new DateTime(2020, 12, 27), 4 }; // first day of fiscal week 53 (Sunday)
        yield return new object[] { Sunday53, new DateTime(2021, 1, 2), 4 }; // Q4 last day (Saturday) — 53rd week end

        // Saturday52 — Q4 first day is Dec 30 (Saturday); final week start is Mar 23, 2024 (Saturday)
        yield return new object[] { Saturday52, new DateTime(2023, 5, 15), 1 }; // mid-Q1
        yield return new object[] { Saturday52, new DateTime(2023, 4, 1), 1 }; // Q1 first day (Saturday)
        yield return new object[] { Saturday52, new DateTime(2023, 6, 30), 1 }; // Q1 last day (Friday)
        yield return new object[] { Saturday52, new DateTime(2023, 7, 1), 2 }; // Q2 first day
        yield return new object[] { Saturday52, new DateTime(2023, 8, 15), 2 }; // mid-Q2
        yield return new object[] { Saturday52, new DateTime(2023, 9, 29), 2 }; // Q2 last day (Friday)
        yield return new object[] { Saturday52, new DateTime(2023, 9, 30), 3 }; // Q3 first day
        yield return new object[] { Saturday52, new DateTime(2023, 11, 15), 3 }; // mid-Q3
        yield return new object[] { Saturday52, new DateTime(2023, 12, 29), 3 }; // Q3 last day (Friday)
        yield return new object[] { Saturday52, new DateTime(2023, 12, 30), 4 }; // Q4 first day (Saturday)
        yield return new object[] { Saturday52, new DateTime(2024, 1, 15), 4 }; // mid-Q4
        yield return new object[] { Saturday52, new DateTime(2024, 3, 23), 4 }; // first day of final fiscal week (Sat)
        yield return new object[] { Saturday52, new DateTime(2024, 3, 29), 4 }; // Q4 last day (Friday)
    }

    /// <summary>
    /// Provides quarter boundary test cases: (provider, quarter, fiscalYear, expectedStart, expectedEnd).
    /// </summary>
    /// <remarks>
    /// Each row covers one quarter of one provider within a specific fiscal year. The
    /// <c>expectedStart</c> and <c>expectedEnd</c> values are the dates returned by
    /// <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> and
    /// <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> respectively — both purely
    /// arithmetic and both <see cref="DateTimeKind.Unspecified" />.
    /// </remarks>
    public static IEnumerable<object[]> GetQuarterBoundaryTestData()
    {
        // Sunday52, FY 2023 (2023-01-01 – 2023-12-30)
        yield return new object[] { Sunday52, 1, Sunday52FiscalYear, new DateTime(2023, 1, 1), new DateTime(2023, 4, 1) };
        yield return new object[] { Sunday52, 2, Sunday52FiscalYear, new DateTime(2023, 4, 2), new DateTime(2023, 7, 1) };
        yield return new object[] { Sunday52, 3, Sunday52FiscalYear, new DateTime(2023, 7, 2), new DateTime(2023, 9, 30) };
        yield return new object[] { Sunday52, 4, Sunday52FiscalYear, new DateTime(2023, 10, 1), new DateTime(2023, 12, 30) };

        // Monday52Leap, FY 2024 (2024-01-01 – 2024-12-29) — leap year
        yield return new object[] { Monday52Leap, 1, Monday52LeapFiscalYear, new DateTime(2024, 1, 1), new DateTime(2024, 3, 31) };
        yield return new object[] { Monday52Leap, 2, Monday52LeapFiscalYear, new DateTime(2024, 4, 1), new DateTime(2024, 6, 30) };
        yield return new object[] { Monday52Leap, 3, Monday52LeapFiscalYear, new DateTime(2024, 7, 1), new DateTime(2024, 9, 29) };
        yield return new object[] { Monday52Leap, 4, Monday52LeapFiscalYear, new DateTime(2024, 9, 30), new DateTime(2024, 12, 29) };

        // Sunday53, FY 2020 (2019-12-29 – 2021-01-02) — 53-week year; Q4 spans 14 weeks
        yield return new object[] { Sunday53, 1, Sunday53FiscalYear, new DateTime(2019, 12, 29), new DateTime(2020, 3, 28) };
        yield return new object[] { Sunday53, 2, Sunday53FiscalYear, new DateTime(2020, 3, 29), new DateTime(2020, 6, 27) };
        yield return new object[] { Sunday53, 3, Sunday53FiscalYear, new DateTime(2020, 6, 28), new DateTime(2020, 9, 26) };
        yield return new object[] { Sunday53, 4, Sunday53FiscalYear, new DateTime(2020, 9, 27), new DateTime(2021, 1, 2) };

        // Saturday52, FY 2023 (2023-04-01 – 2024-03-29) — straddles calendar year; Q4 within leap year
        yield return new object[] { Saturday52, 1, Saturday52FiscalYear, new DateTime(2023, 4, 1), new DateTime(2023, 6, 30) };
        yield return new object[] { Saturday52, 2, Saturday52FiscalYear, new DateTime(2023, 7, 1), new DateTime(2023, 9, 29) };
        yield return new object[] { Saturday52, 3, Saturday52FiscalYear, new DateTime(2023, 9, 30), new DateTime(2023, 12, 29) };
        yield return new object[] { Saturday52, 4, Saturday52FiscalYear, new DateTime(2023, 12, 30), new DateTime(2024, 3, 29) };
    }
}
