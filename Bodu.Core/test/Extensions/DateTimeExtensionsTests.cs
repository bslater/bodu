// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Globalization;

namespace Bodu.Extensions
{
    /// <summary>
    /// Contains unit tests for the <see cref="DateTimeExtensions" /> extension methods.
    /// </summary>
    [TestClass]
    public partial class DateTimeExtensionsTests
    {
        public static readonly FirstAndLastDayData[] FirstAndLastDayOfWeekTestData =
        {
            // === Saturday–Sunday weekend => week starts Monday (2024-01-01), ends Sunday (2024-01-07)
            new() { Input = new DateTime(2024, 1, 1), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 2), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 3), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 4), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 5), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 6), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 7), Weekend = CalendarWeekendDefinition.SaturdaySunday, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },

            // === Friday–Saturday weekend => week starts Sunday (2023-12-31), ends Saturday (2024-01-06)
            new() { Input = new DateTime(2023, 12, 31), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 1), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 2), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 3), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 4), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 5), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },
            new() { Input = new DateTime(2024, 1, 6), Weekend = CalendarWeekendDefinition.FridaySaturday, ExpectedFirst = new DateTime(2023, 12, 31), ExpectedLast = new DateTime(2024, 1, 6) },

            // === Thursday–Friday weekend => week starts Saturday (2024-01-06), ends Friday (2024-01-12)
            new() { Input = new DateTime(2024, 1, 6), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 7), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 8), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 9), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 10), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 11), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },
            new() { Input = new DateTime(2024, 1, 12), Weekend = CalendarWeekendDefinition.ThursdayFriday, ExpectedFirst = new DateTime(2024, 1, 6), ExpectedLast = new DateTime(2024, 1, 12) },

            // === Friday only weekend => week starts Saturday (2023-12-30), ends Friday (2024-01-05)
            new() { Input = new DateTime(2023, 12, 30), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2023, 12, 31), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2024, 1, 1), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2024, 1, 2), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2024, 1, 3), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2024, 1, 4), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },
            new() { Input = new DateTime(2024, 1, 5), Weekend = CalendarWeekendDefinition.FridayOnly, ExpectedFirst = new DateTime(2023, 12, 30), ExpectedLast = new DateTime(2024, 1, 5) },

            // === Sunday only weekend => week starts Monday (2024-01-01), ends Sunday (2024-01-07)
            new() { Input = new DateTime(2024, 1, 1), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 2), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 3), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 4), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 5), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 6), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 7), Weekend = CalendarWeekendDefinition.SundayOnly, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },

            // === None => defaults to Monday–Sunday
            new() { Input = new DateTime(2024, 1, 1), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 2), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 3), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 4), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 5), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 6), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },
            new() { Input = new DateTime(2024, 1, 7), Weekend = CalendarWeekendDefinition.None, ExpectedFirst = new DateTime(2024, 1, 1), ExpectedLast = new DateTime(2024, 1, 7) },

            new () { Input = new DateTime(2024, 04, 08), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Monday
            new () { Input = new DateTime(2024, 04, 09), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Tuesday
            new () { Input = new DateTime(2024, 04, 10), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Wednesday
            new () { Input = new DateTime(2024, 04, 11), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Thursday
            new () { Input = new DateTime(2024, 04, 12), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Friday
            new () { Input = new DateTime(2024, 04, 13), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Saturday
            new () { Input = new DateTime(2024, 04, 14), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Sunday
            new () { Input = new DateTime(2024, 04, 15), CultureInfo = CultureInfo.GetCultureInfo("en-GB"), ExpectedFirst = new DateTime(2024, 04, 15), ExpectedLast = new DateTime(2024, 04, 21) }, // Monday (next week)

            new() { Input = new DateTime(2024, 04, 08), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Monday
            new() { Input = new DateTime(2024, 04, 09), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Tuesday
            new() { Input = new DateTime(2024, 04, 10), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Wednesday
            new() { Input = new DateTime(2024, 04, 11), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Thursday
            new() { Input = new DateTime(2024, 04, 12), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Friday
            new() { Input = new DateTime(2024, 04, 13), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Saturday
            new() { Input = new DateTime(2024, 04, 14), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 08), ExpectedLast = new DateTime(2024, 04, 14) }, // Sunday
            new() { Input = new DateTime(2024, 04, 15), CultureInfo = CultureInfo.GetCultureInfo("fr-FR"), ExpectedFirst = new DateTime(2024, 04, 15), ExpectedLast = new DateTime(2024, 04, 21) }, // Monday (next week)
        };

        public static readonly FirstAndLastDayData[] FirstAndLastDayOMonthDataTestData =
        {
            // Regular months in 2023
            new() { Input = new DateTime(2023, 01, 15, 8, 0, 0), ExpectedFirst = new DateTime(2023, 01, 01, 0, 0, 0), ExpectedLast =    new DateTime(2023, 01, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 02, 28, 23, 59, 59), ExpectedFirst = new DateTime(2023, 02, 01, 0, 0, 0), ExpectedLast = new DateTime(2023, 02, 28, 0, 0, 0) },
            new() { Input = new DateTime(2023, 03, 31, 12, 30, 0), ExpectedFirst = new DateTime(2023, 03, 01, 0, 0, 0), ExpectedLast =  new DateTime(2023, 03, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 04, 10, 0, 0, 1), ExpectedFirst = new DateTime(2023, 04, 01, 0, 0, 0), ExpectedLast =    new DateTime(2023, 04, 30, 0, 0, 0) },
            new() { Input = new DateTime(2023, 05, 01, 18, 0, 0), ExpectedFirst = new DateTime(2023, 05, 01, 0, 0, 0), ExpectedLast =   new DateTime(2023, 05, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 06, 15, 6, 30, 0), ExpectedFirst = new DateTime(2023, 06, 01, 0, 0, 0), ExpectedLast =   new DateTime(2023, 06, 30, 0, 0, 0) },
            new() { Input = new DateTime(2023, 07, 25, 5, 0, 0), ExpectedFirst = new DateTime(2023, 07, 01, 0, 0, 0), ExpectedLast =    new DateTime(2023, 07, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 08, 31, 23, 59, 59), ExpectedFirst = new DateTime(2023, 08, 01, 0, 0, 0), ExpectedLast = new DateTime(2023, 08, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 09, 05, 12, 0, 0), ExpectedFirst = new DateTime(2023, 09, 01, 0, 0, 0), ExpectedLast =   new DateTime(2023, 09, 30, 0, 0, 0) },
            new() { Input = new DateTime(2023, 10, 10, 10, 10, 10), ExpectedFirst = new DateTime(2023, 10, 01, 0, 0, 0), ExpectedLast = new DateTime(2023, 10, 31, 0, 0, 0) },
            new() { Input = new DateTime(2023, 11, 30, 8, 0, 0), ExpectedFirst = new DateTime(2023, 11, 01, 0, 0, 0), ExpectedLast =    new DateTime(2023, 11, 30, 0, 0, 0) },
            new() { Input = new DateTime(2023, 12, 31, 15, 0, 0), ExpectedFirst = new DateTime(2023, 12, 01, 0, 0, 0), ExpectedLast =   new DateTime(2023, 12, 31, 0, 0, 0) },

            // Leap year (2024)
            new() { Input = new DateTime(2024, 01, 01, 0, 0, 0), ExpectedFirst = new DateTime(2024, 01, 01, 0, 0, 0), ExpectedLast =    new DateTime(2024, 01, 31, 0, 0, 0) },
            new() { Input = new DateTime(2024, 02, 29, 23, 59, 59), ExpectedFirst = new DateTime(2024, 02, 01, 0, 0, 0), ExpectedLast = new DateTime(2024, 02, 29, 0, 0, 0) },
            new() { Input = new DateTime(2024, 03, 01, 0, 0, 0), ExpectedFirst = new DateTime(2024, 03, 01, 0, 0, 0), ExpectedLast =    new DateTime(2024, 03, 31, 0, 0, 0) },
            new() { Input = new DateTime(2024, 04, 18, 13, 45, 0), ExpectedFirst = new DateTime(2024, 04, 01, 0, 0, 0), ExpectedLast =  new DateTime(2024, 04, 30, 0, 0, 0) },

            // Boundary checks
            new() { Input = DateTime.MinValue, ExpectedFirst = new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, 1, 0, 0, 0), ExpectedLast = new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.DaysInMonth(DateTime.MinValue.Year, DateTime.MinValue.Month), 0, 0, 0) },
            new() { Input = DateTime.MaxValue, ExpectedFirst = new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, 1, 0, 0, 0), ExpectedLast = new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, DateTime.DaysInMonth(DateTime.MaxValue.Year, DateTime.MaxValue.Month), 0, 0, 0) }
        };

        public static readonly FirstAndLastDayData[] FirstAndLastDayOYearDataTestData =
        {
        };

        private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static CultureInfo TestCulture
        {
            get
            {
                var customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                customCulture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Wednesday;

                return customCulture;
            }
        }

        public struct FirstAndLastDayData
        {
            public CultureInfo? CultureInfo { get; set; }

            public DateTime ExpectedFirst { get; set; }

            public DateTime ExpectedLast { get; set; }

            public DateTime Input { get; set; }

            public CalendarWeekendDefinition? Weekend { get; set; }
        }

        public sealed class FridayOnlyWeekendProvider : IWeekendDefinitionProvider
        {
            public bool IsWeekend(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Friday;
        }

        public sealed class InValidQuarterProvider : IQuarterDefinitionProvider
        {
            /// <summary>
            /// Always returns an invalid quarter number (outside the expected range of 1–4).
            /// </summary>
            /// <param name="dateTime">The input <see cref="DateTime" />.</param>
            /// <returns>An invalid quarter number.</returns>
            public int GetQuarter(DateTime dateTime)
            {
                return dateTime.Month switch
                {
                    1 => 0,  // Invalid (below range)
                    2 => -5, // Invalid (negative)
                    3 => 5,  // Invalid (above range)
                    4 => 999, // Invalid (far above range)
                    _ => 10  // Also invalid
                };
            }

            public int GetQuarter(DateOnly dateOnly)
            {
                throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");
            }

            /// <summary>
            /// Always throws <see cref="ArgumentOutOfRangeException" /> to simulate an invalid quarter mapping.
            /// </summary>
            /// <param name="dateTime">The input <see cref="DateTime" />.</param>
            public DateTime GetQuarterEnd(DateTime dateTime)
            {
                throw new ArgumentOutOfRangeException(nameof(dateTime), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateTime GetQuarterEnd(int quarter)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterEndDate(DateOnly dateOnly)
            {
                throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterEndDate(int quarter)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            /// <summary>
            /// Always throws <see cref="ArgumentOutOfRangeException" /> to simulate an invalid quarter mapping.
            /// </summary>
            /// <param name="dateTime">The input <see cref="DateTime" />.</param>
            public DateTime GetQuarterStart(DateTime dateTime)
            {
                throw new ArgumentOutOfRangeException(nameof(dateTime), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateTime GetQuarterStart(int quarter)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterStartDate(DateOnly dateOnly)
            {
                throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterStartDate(int quarter)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateTime GetQuarterStart(int quarter, int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateTime GetQuarterEnd(int quarter, int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterStartDate(int quarter, int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public DateOnly GetQuarterEndDate(int quarter, int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");
            }

            public bool Is53WeekFiscalYear(int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(fiscalYear), "This provider intentionally returns invalid quarter mappings.");
            }

            public int GetWeeksInFiscalYear(int fiscalYear)
            {
                throw new ArgumentOutOfRangeException(nameof(fiscalYear), "This provider intentionally returns invalid quarter mappings.");
            }
        }

        public sealed class ValidQuarterProvider : IQuarterDefinitionProvider
        {
            public static IEnumerable<object[]> FirstDayOfQuarterTestData()
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

            public static IEnumerable<object[]> FirstDayOfWeekInQuarterTestData()
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

            public static IEnumerable<object[]> IsFirstDayOfQuarterTestData() =>
                FirstDayOfQuarterTestData()
                    .Select(o => new object[] { o[0], (DateTime)o[0] == (DateTime)o[1] });

            public static IEnumerable<object[]> IsLastDayOfQuarterTestData() =>
                LastDayOfQuarterTestData()
                    .Select(o => new object[] { o[0], (DateTime)o[0] == (DateTime)o[1] });

            public static IEnumerable<object[]> LastDayOfQuarterTestData()
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
                    _ => throw new ArgumentOutOfRangeException(nameof(dateTime.Month))
                };
            }

            public int GetQuarter(DateOnly dateOnly) => GetQuarter(dateOnly.ToDateTime(TimeOnly.MinValue));

            public DateTime GetQuarterEnd(DateTime dateTime)
            {
                var quarter = GetQuarter(dateTime);
                var start = GetQuarterStart(dateTime);

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

#pragma warning disable CS0618 // intentional: delegate the fiscal-year overloads to the existing fixed-year implementations
            public DateTime GetQuarterStart(int quarter, int fiscalYear) => GetQuarterStart(quarter);

            public DateTime GetQuarterEnd(int quarter, int fiscalYear) => GetQuarterEnd(quarter);

            public DateOnly GetQuarterStartDate(int quarter, int fiscalYear) => GetQuarterStartDate(quarter);

            public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) => GetQuarterEndDate(quarter);
#pragma warning restore CS0618

            public bool Is53WeekFiscalYear(int fiscalYear) => false;

            public int GetWeeksInFiscalYear(int fiscalYear) => 52;
        }
    }
}