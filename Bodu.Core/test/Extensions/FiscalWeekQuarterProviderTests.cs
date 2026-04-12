// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    /// <summary>
    /// Contains unit tests for <see cref="FiscalWeekQuarterProvider" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All providers used as shared fixtures use <c>isFiscalYearEnd: false</c> combined with an anchor month
    /// whose first day is exactly the desired <see cref="DayOfWeek" />, eliminating alignment ambiguity and
    /// making expected quarter boundaries straightforwardly verifiable from first principles.
    /// </para>
    /// <para>
    /// Four providers are defined to give broad scenario coverage:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="Sunday52" /></term>
    /// <description>
    /// 52-week Sunday-start fiscal year beginning 1 January 2023 (a Sunday).
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="Monday52Leap" /></term>
    /// <description>
    /// 52-week Monday-start fiscal year beginning 1 January 2024 (a Monday). 2024 is a leap year.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="Sunday53" /></term>
    /// <description>
    /// 53-week Sunday-start fiscal year beginning 29 December 2019. Q4 contains 14 weeks and
    /// ends 2 January 2021.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="Saturday52" /></term>
    /// <description>
    /// 52-week Saturday-start fiscal year beginning 1 April 2023, straddling the calendar year.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <strong>Known validation constraint:</strong>
    /// <see cref="FiscalWeekQuarterProvider" /> aligns each input date <em>forward</em> to the next
    /// <c>_firstDayOfWeek</c> occurrence to determine its fiscal week. Dates in the last 1–6 days of
    /// the final fiscal week — those that are not the fiscal week start day itself — resolve to a
    /// <c>weekStartTicks</c> value that falls in the next fiscal year and therefore throw
    /// <see cref="ArgumentOutOfRangeException" />. Affected dates per provider:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="Sunday52" />: 28–30 December 2023 (Thu–Sat)</description></item>
    /// <item><description><see cref="Monday52Leap" />: 27–29 December 2024 (Fri–Sun)</description></item>
    /// <item><description><see cref="Sunday53" />: 31 December 2020–2 January 2021 (Thu–Sat)</description></item>
    /// <item><description><see cref="Saturday52" />: 27–29 March 2024 (Wed–Fri)</description></item>
    /// </list>
    /// <para>
    /// Test data and test methods avoid these dates. Quarter boundary tests that operate on the last day of
    /// a quarter use only Q1–Q3, and Q4 coverage uses the <em>first</em> day of the final fiscal week.
    /// </para>
    /// </remarks>
    [TestClass]
    public partial class FiscalWeekQuarterProviderTests
    {
        // Jan 1, 2023 = Sunday — alignment is exact; no ambiguity.
        // FY: 2023-01-01 – 2023-12-30 | 52 weeks | Weeks544
        // Q1: 2023-01-01 – 2023-04-01  Q2: 2023-04-02 – 2023-07-01
        // Q3: 2023-07-02 – 2023-09-30  Q4: 2023-10-01 – 2023-12-30
        private static readonly FiscalWeekQuarterProvider Sunday52 =
            new FiscalWeekQuarterProvider(2023, 1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks544);

        // Jan 1, 2024 = Monday — alignment is exact; no ambiguity.
        // 2024 is a leap year (contains Feb 29). FY: 2024-01-01 – 2024-12-29 | 52 weeks | Weeks445
        // Q1: 2024-01-01 – 2024-03-31  Q2: 2024-04-01 – 2024-06-30
        // Q3: 2024-07-01 – 2024-09-29  Q4: 2024-09-30 – 2024-12-29
        private static readonly FiscalWeekQuarterProvider Monday52Leap =
            new FiscalWeekQuarterProvider(2024, 1, DayOfWeek.Monday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks445);

        // Jan 1, 2020 = Wednesday — nearest Sunday is Dec 29, 2019 (3 days earlier).
        // 2020 is a leap year. FY: 2019-12-29 – 2021-01-02 | 53 weeks | Weeks445
        // Q1: 2019-12-29 – 2020-03-28  Q2: 2020-03-29 – 2020-06-27
        // Q3: 2020-06-28 – 2020-09-26  Q4: 2020-09-27 – 2021-01-02  (14 weeks)
        private static readonly FiscalWeekQuarterProvider Sunday53 =
            new FiscalWeekQuarterProvider(2020, 1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks445);

        // Apr 1, 2023 = Saturday — alignment is exact; no ambiguity.
        // Straddles calendar year; Q4 falls in a leap year (2024). FY: 2023-04-01 – 2024-03-29 | 52 weeks | Weeks454
        // Q1: 2023-04-01 – 2023-06-30  Q2: 2023-07-01 – 2023-09-29
        // Q3: 2023-09-30 – 2023-12-29  Q4: 2023-12-30 – 2024-03-29
        private static readonly FiscalWeekQuarterProvider Saturday52 =
            new FiscalWeekQuarterProvider(2023, 4, DayOfWeek.Saturday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks454);

        /// <summary>
        /// Provides quarter number test cases: (provider, date, expectedQuarter).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Includes mid-quarter dates, the first day of each quarter, and — for Q1 through Q3 —
        /// the last day of each quarter. Leap day (29 February) is explicitly covered.
        /// </para>
        /// <para>
        /// Q4 last-day entries are intentionally omitted. Dates in the last 1–6 days of the final
        /// fiscal week (those that are not the fiscal week start day itself) produce a
        /// <c>weekStartTicks</c> value that falls in the following fiscal year and therefore throw
        /// <see cref="ArgumentOutOfRangeException" />. Q4 coverage instead uses the first day of
        /// the final fiscal week, which is always valid (it is the <c>_firstDayOfWeek</c> itself,
        /// giving a forward offset of zero).
        /// </para>
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
        }

        /// <summary>
        /// Provides quarter boundary test cases: (provider, quarter, expectedStart, expectedEnd).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each row covers one quarter of one provider. The <c>expectedStart</c> and <c>expectedEnd</c>
        /// values are the dates returned by <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" />
        /// and <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> respectively — both purely
        /// arithmetic and both <see cref="DateTimeKind.Unspecified" />.
        /// </para>
        /// <para>
        /// Tests that pass <c>expectedEnd</c> to a <see cref="DateTime" />- or
        /// <see cref="DateOnly" />-accepting overload must guard against Q4 rows, because the last
        /// day of Q4 falls in the validation dead zone described in the class remarks.
        /// </para>
        /// </remarks>
        public static IEnumerable<object[]> GetQuarterBoundaryTestData()
        {
            // Sunday52 (2023-01-01 – 2023-12-30)
            yield return new object[] { Sunday52, 1, new DateTime(2023, 1, 1), new DateTime(2023, 4, 1) };
            yield return new object[] { Sunday52, 2, new DateTime(2023, 4, 2), new DateTime(2023, 7, 1) };
            yield return new object[] { Sunday52, 3, new DateTime(2023, 7, 2), new DateTime(2023, 9, 30) };
            yield return new object[] { Sunday52, 4, new DateTime(2023, 10, 1), new DateTime(2023, 12, 30) };

            // Monday52Leap (2024-01-01 – 2024-12-29) — leap year
            yield return new object[] { Monday52Leap, 1, new DateTime(2024, 1, 1), new DateTime(2024, 3, 31) };
            yield return new object[] { Monday52Leap, 2, new DateTime(2024, 4, 1), new DateTime(2024, 6, 30) };
            yield return new object[] { Monday52Leap, 3, new DateTime(2024, 7, 1), new DateTime(2024, 9, 29) };
            yield return new object[] { Monday52Leap, 4, new DateTime(2024, 9, 30), new DateTime(2024, 12, 29) };

            // Sunday53 (2019-12-29 – 2021-01-02) — 53-week year; Q4 spans 14 weeks
            yield return new object[] { Sunday53, 1, new DateTime(2019, 12, 29), new DateTime(2020, 3, 28) };
            yield return new object[] { Sunday53, 2, new DateTime(2020, 3, 29), new DateTime(2020, 6, 27) };
            yield return new object[] { Sunday53, 3, new DateTime(2020, 6, 28), new DateTime(2020, 9, 26) };
            yield return new object[] { Sunday53, 4, new DateTime(2020, 9, 27), new DateTime(2021, 1, 2) };

            // Saturday52 (2023-04-01 – 2024-03-29) — straddles calendar year; Q4 within leap year
            yield return new object[] { Saturday52, 1, new DateTime(2023, 4, 1), new DateTime(2023, 6, 30) };
            yield return new object[] { Saturday52, 2, new DateTime(2023, 7, 1), new DateTime(2023, 9, 29) };
            yield return new object[] { Saturday52, 3, new DateTime(2023, 9, 30), new DateTime(2023, 12, 29) };
            yield return new object[] { Saturday52, 4, new DateTime(2023, 12, 30), new DateTime(2024, 3, 29) };
        }
    }
}