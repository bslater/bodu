// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.ValidQuarterProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Test-only <see cref="IQuarterDefinitionProvider" /> implementing a shifted 3-month quarter grid
    /// (Q1 = Dec–Feb, Q2 = Mar–May, Q3 = Jun–Aug, Q4 = Sep–Nov). Exposes public <c>DynamicData</c> sources
    /// used by provider-overload tests in both <see cref="DateTimeExtensionsTests" /> and
    /// <see cref="DateOnlyExtensionsTests" />.
    /// </summary>
    public sealed class ValidQuarterProvider
        : IQuarterDefinitionProvider
    {

        public static IEnumerable<object[]> FirstDateOfQuarterTestData()
        {
            yield return new object[] { new DateTime(2024, 01, 01), new DateTime(2023, 12, 01) };
            yield return new object[] { new DateTime(2024, 02, 01), new DateTime(2023, 12, 01) };
            yield return new object[] { new DateTime(2024, 03, 01), new DateTime(2024, 03, 01) };
            yield return new object[] { new DateTime(2024, 04, 01), new DateTime(2024, 03, 01) };
            yield return new object[] { new DateTime(2024, 05, 01), new DateTime(2024, 03, 01) };
            yield return new object[] { new DateTime(2024, 06, 01), new DateTime(2024, 06, 01) };
            yield return new object[] { new DateTime(2024, 07, 01), new DateTime(2024, 06, 01) };
            yield return new object[] { new DateTime(2024, 08, 01), new DateTime(2024, 06, 01) };
            yield return new object[] { new DateTime(2024, 09, 01), new DateTime(2024, 09, 01) };
            yield return new object[] { new DateTime(2024, 10, 01), new DateTime(2024, 09, 01) };
            yield return new object[] { new DateTime(2024, 11, 01), new DateTime(2024, 09, 01) };
            yield return new object[] { new DateTime(2023, 12, 01), new DateTime(2023, 12, 01) };
        }

        public static IEnumerable<object[]> FirstDateOfWeekInQuarterTestData()
        {
            yield return new object[] { new DateTime(2024, 01, 01), DayOfWeek.Monday, new DateTime(2023, 12, 04) };
            yield return new object[] { new DateTime(2024, 02, 01), DayOfWeek.Tuesday, new DateTime(2023, 12, 05) };
            yield return new object[] { new DateTime(2024, 03, 01), DayOfWeek.Wednesday, new DateTime(2024, 03, 06) };
            yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, new DateTime(2024, 03, 07) };
            yield return new object[] { new DateTime(2024, 05, 01), DayOfWeek.Friday, new DateTime(2024, 03, 01) };
            yield return new object[] { new DateTime(2024, 06, 01), DayOfWeek.Saturday, new DateTime(2024, 06, 01) };
            yield return new object[] { new DateTime(2024, 07, 01), DayOfWeek.Sunday, new DateTime(2024, 06, 02) };
            yield return new object[] { new DateTime(2024, 08, 01), DayOfWeek.Monday, new DateTime(2024, 06, 03) };
            yield return new object[] { new DateTime(2024, 09, 01), DayOfWeek.Tuesday, new DateTime(2024, 09, 03) };
            yield return new object[] { new DateTime(2024, 10, 01), DayOfWeek.Wednesday, new DateTime(2024, 09, 04) };
            yield return new object[] { new DateTime(2024, 11, 01), DayOfWeek.Thursday, new DateTime(2024, 09, 05) };
            yield return new object[] { new DateTime(2023, 12, 01), DayOfWeek.Friday, new DateTime(2023, 12, 01) };
        }

        public static IEnumerable<object[]> IsFirstDateOfQuarterTestData() =>
            FirstDateOfQuarterTestData()
                .Select(o => new object[] { o[0], (DateTime)o[0] == (DateTime)o[1] });

        public static IEnumerable<object[]> IsLastDateOfQuarterTestData() =>
            LastDateOfQuarterTestData()
                .Select(o => new object[] { o[0], (DateTime)o[0] == (DateTime)o[1] });

        public static IEnumerable<object[]> LastDateOfQuarterTestData()
        {
            yield return new object[] { new DateTime(2024, 01, 31), new DateTime(2024, 02, 29) };
            yield return new object[] { new DateTime(2024, 02, 29), new DateTime(2024, 02, 29) };
            yield return new object[] { new DateTime(2024, 03, 31), new DateTime(2024, 05, 31) };
            yield return new object[] { new DateTime(2024, 04, 30), new DateTime(2024, 05, 31) };
            yield return new object[] { new DateTime(2024, 05, 31), new DateTime(2024, 05, 31) };
            yield return new object[] { new DateTime(2024, 06, 30), new DateTime(2024, 08, 31) };
            yield return new object[] { new DateTime(2024, 07, 31), new DateTime(2024, 08, 31) };
            yield return new object[] { new DateTime(2024, 08, 31), new DateTime(2024, 08, 31) };
            yield return new object[] { new DateTime(2024, 09, 30), new DateTime(2024, 11, 30) };
            yield return new object[] { new DateTime(2024, 10, 31), new DateTime(2024, 11, 30) };
            yield return new object[] { new DateTime(2024, 11, 30), new DateTime(2024, 11, 30) };
            yield return new object[] { new DateTime(2023, 12, 31), new DateTime(2024, 02, 29) };
        }

        public static IEnumerable<object[]> QuarterTestData()
        {
            yield return new object[] { new DateTime(2024, 01, 01), 1 };
            yield return new object[] { new DateTime(2024, 02, 01), 1 };
            yield return new object[] { new DateTime(2024, 03, 01), 2 };
            yield return new object[] { new DateTime(2024, 04, 01), 2 };
            yield return new object[] { new DateTime(2024, 05, 01), 2 };
            yield return new object[] { new DateTime(2024, 06, 01), 3 };
            yield return new object[] { new DateTime(2024, 07, 01), 3 };
            yield return new object[] { new DateTime(2024, 08, 01), 3 };
            yield return new object[] { new DateTime(2024, 09, 01), 4 };
            yield return new object[] { new DateTime(2024, 10, 01), 4 };
            yield return new object[] { new DateTime(2024, 11, 01), 4 };
            yield return new object[] { new DateTime(2023, 12, 01), 1 };
        }

        public int GetQuarter(DateTime dateTime)
        {
            return dateTime.Month switch
            {
                12 => 1,
                1 or 2 => 1,
                3 or 4 or 5 => 2,
                6 or 7 or 8 => 3,
                9 or 10 or 11 => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(dateTime))
            };
        }

        public int GetQuarter(DateOnly dateOnly) => GetQuarter(dateOnly.ToDateTime(TimeOnly.MinValue));

        public DateTime GetQuarterEnd(DateTime dateTime)
        {
            var quarter = GetQuarter(dateTime);
            DateTime start = GetQuarterStart(dateTime);

            return quarter switch
            {
                1 => new DateTime(start.Year + 1, 2, DateTime.DaysInMonth(start.Year + 1, 2)),
                2 => new DateTime(start.Year, 5, 31),
                3 => new DateTime(start.Year, 8, 31),
                4 => new DateTime(start.Year, 11, 30),
                _ => throw new ArgumentOutOfRangeException(nameof(dateTime))
            };
        }

        public DateTime GetQuarterEnd(int quarter)
        {
            return quarter switch
            {
                1 => new DateTime(2024, 2, DateTime.DaysInMonth(2024, 2)),
                2 => new DateTime(2024, 5, 31),
                3 => new DateTime(2024, 8, 31),
                4 => new DateTime(2024, 11, 30),
                _ => throw new ArgumentOutOfRangeException(nameof(quarter))
            };
        }

        public DateOnly GetQuarterEndDate(DateOnly dateOnly) => GetQuarterEnd(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

        public DateOnly GetQuarterEndDate(int quarter) => GetQuarterEndDate(GetQuarterEnd(quarter).ToDateOnly());

        public DateTime GetQuarterStart(DateTime dateTime)
        {
            return GetQuarter(dateTime) switch
            {
                1 => new DateTime(dateTime.Month == 12 ? dateTime.Year : dateTime.Year - 1, 12, 1),
                2 => new DateTime(dateTime.Year, 3, 1),
                3 => new DateTime(dateTime.Year, 6, 1),
                4 => new DateTime(dateTime.Year, 9, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(dateTime))
            };
        }

        public DateTime GetQuarterStart(int quarter)
        {
            return quarter switch
            {
                1 => new DateTime(2023, 12, 1), // assumes test year context
                2 => new DateTime(2024, 3, 1),
                3 => new DateTime(2024, 6, 1),
                4 => new DateTime(2024, 9, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(quarter))
            };
        }

        public DateOnly GetQuarterStartDate(DateOnly dateOnly) => GetQuarterStart(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

        public DateOnly GetQuarterStartDate(int quarter) => GetQuarterStartDate(GetQuarterStart(quarter).ToDateOnly());

        public int GetWeeksInFiscalYear(int fiscalYear) => 52;
        public bool Is53WeekFiscalYear(int fiscalYear) => false;

#pragma warning disable CS0618 // intentional: delegate the fiscal-year overloads to the existing fixed-year implementations
        public DateTime GetQuarterStart(int quarter, int fiscalYear) => GetQuarterStart(quarter);

        public DateTime GetQuarterEnd(int quarter, int fiscalYear) => GetQuarterEnd(quarter);

        public DateOnly GetQuarterStartDate(int quarter, int fiscalYear) => GetQuarterStartDate(quarter);

        public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) => GetQuarterEndDate(quarter);
#pragma warning restore CS0618


    }

}
