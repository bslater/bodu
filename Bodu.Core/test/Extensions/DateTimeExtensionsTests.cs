// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
    }
}
