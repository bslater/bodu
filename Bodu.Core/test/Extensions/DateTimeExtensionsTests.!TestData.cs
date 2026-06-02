// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.!TestData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    public static IEnumerable<object[]> AgeTestData()
    {
        yield return new object[] { new DateTime(2000, 01, 01), new DateTime(2024, 04, 18), 24 };                           // Birthday passed this year
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(2024, 04, 18), 24 };                           // Birthday today
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(2024, 04, 17), 23 };                           // Day before birthday
        yield return new object[] { new DateTime(2000, 12, 31), new DateTime(2024, 04, 18), 23 };                           // Birthday not yet reached
        yield return new object[] { new DateTime(2000, 12, 31), new DateTime(2021, 01, 01), 20 };                           // Not yet birthday next year
        yield return new object[] { new DateTime(2000, 12, 31), new DateTime(2021, 12, 31), 21 };                           // Birthday on Dec 31
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(2000, 04, 18), 0 };                            // Same day
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(1999, 12, 31), 0 };                            // Clamped to 0
        yield return new object[] { new DateTime(2000, 02, 29), new DateTime(2023, 02, 28), 23 };                           // Leap day → non-leap year
        yield return new object[] { new DateTime(2000, 02, 29), new DateTime(2024, 02, 29), 24 };                           // Leap day → leap year
        yield return new object[] { new DateTime(1900, 01, 01), new DateTime(2000, 01, 01), 100 };                          // Century span
        yield return new object[] { new DateTime(2000, 01, 01), new DateTime(2024, 12, 31), 24 };                           // Birthday passed early in year
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(1999, 04, 18), 0 };                            // Negative range
        yield return new object[] { new DateTime(0001, 01, 01), new DateTime(0001, 12, 31), 0 };                            // First year
        yield return new object[] { new DateTime(0001, 01, 01), new DateTime(9999, 12, 31), 9998 };                         // Max valid range
        yield return new object[] { new DateTime(2001, 02, 28), new DateTime(2024, 02, 28), 23 };                           // Born non-leap, evaluated in leap year
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(2024, 04, 18, 23, 59, 59), 24 };               // Time late in day
        yield return new object[] { new DateTime(2000, 04, 18), new DateTime(2024, 04, 17, 00, 00, 00), 23 };               // Midnight day before
        yield return new object[] { new DateTime(2000, 04, 18, 2, 59, 59), new DateTime(2024, 04, 18, 00, 00, 00), 24 };    // Time ignored
    }

    public static IEnumerable<object[]> CalendarQuarterDefinitionDateTimeKindTestData() =>
        Enum.GetValues<CalendarQuarterDefinition>()
            .Where(def => def != CalendarQuarterDefinition.Custom)
            .SelectMany(def =>
                Enum.GetValues<DateTimeKind>()
                    .Select(kind => new object[] { def, kind }));

    public static IEnumerable<object[]> WorkingDaysOfWeekDateTimeKindTestData() =>
        Enum.GetValues<WorkingDaysOfWeek>()
            .Where(def => def != WorkingDaysOfWeek.Custom)
            .SelectMany(def =>
                Enum.GetValues<DateTimeKind>()
                    .Select(kind => new object[] { def, kind }));

    public static IEnumerable<object[]> DayNameFrenchTestData() =>
        DayNameTestData()
            .Where(o => ((CultureInfo)o[1]).Name == "fr-FR")
            .Select(o => new object[] { o[0], o[2] });

    public static IEnumerable<object[]> DayNameTestData()
    {
        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("en-US"), "Sunday" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("en-US"), "Monday" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("en-US"), "Tuesday" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("en-US"), "Wednesday" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("en-US"), "Thursday" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("en-US"), "Friday" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("en-US"), "Saturday" };

        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("de-DE"), "Sonntag" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("de-DE"), "Montag" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("de-DE"), "Dienstag" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("de-DE"), "Mittwoch" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("de-DE"), "Donnerstag" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("de-DE"), "Freitag" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("de-DE"), "Samstag" };

        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("fr-FR"), "dimanche" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("fr-FR"), "lundi" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("fr-FR"), "mardi" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("fr-FR"), "mercredi" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("fr-FR"), "jeudi" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("fr-FR"), "vendredi" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("fr-FR"), "samedi" };

        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("it-IT"), "domenica" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("it-IT"), "lunedì" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("it-IT"), "martedì" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("it-IT"), "mercoledì" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("it-IT"), "giovedì" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("it-IT"), "venerdì" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("it-IT"), "sabato" };

        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("ru-RU"), "воскресенье" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("ru-RU"), "понедельник" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("ru-RU"), "вторник" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("ru-RU"), "среда" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("ru-RU"), "четверг" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("ru-RU"), "пятница" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("ru-RU"), "суббота" };

        yield return new object[] { new DateTime(2024, 4, 7), new CultureInfo("nb-NO"), "søndag" };
        yield return new object[] { new DateTime(2024, 4, 1), new CultureInfo("nb-NO"), "mandag" };
        yield return new object[] { new DateTime(2024, 4, 9), new CultureInfo("nb-NO"), "tirsdag" };
        yield return new object[] { new DateTime(2024, 4, 10), new CultureInfo("nb-NO"), "onsdag" };
        yield return new object[] { new DateTime(2024, 4, 11), new CultureInfo("nb-NO"), "torsdag" };
        yield return new object[] { new DateTime(2024, 4, 12), new CultureInfo("nb-NO"), "fredag" };
        yield return new object[] { new DateTime(2024, 4, 13), new CultureInfo("nb-NO"), "lørdag" };
    }

    public static IEnumerable<object[]> DayOfWeekOccurrenceInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 07), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 01), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 02), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 03), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 04), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 05), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 06), WeekOfMonthOrdinal.First };
        yield return new object[] { new DateTime(2024, 04, 14), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 08), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 09), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 10), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 11), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 12), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 13), WeekOfMonthOrdinal.Second };
        yield return new object[] { new DateTime(2024, 04, 21), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 15), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 16), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 17), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 18), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 19), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 20), WeekOfMonthOrdinal.Third };
        yield return new object[] { new DateTime(2024, 04, 28), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 22), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 23), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 24), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 25), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 26), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 27), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 28), WeekOfMonthOrdinal.Fourth };
        yield return new object[] { new DateTime(2024, 04, 29), WeekOfMonthOrdinal.Fifth };
        yield return new object[] { new DateTime(2024, 04, 30), WeekOfMonthOrdinal.Fifth };
    }

    public static IEnumerable<object[]> DaysInMonthTestData()
    {
        // Non-leap year: 2023
        yield return new object[] { new DateTime(2023, 01, 15), 31 }; // January
        yield return new object[] { new DateTime(2023, 02, 15), 28 }; // February (non-leap)
        yield return new object[] { new DateTime(2023, 03, 15), 31 }; // March
        yield return new object[] { new DateTime(2023, 04, 15), 30 }; // April
        yield return new object[] { new DateTime(2023, 05, 15), 31 }; // May
        yield return new object[] { new DateTime(2023, 06, 15), 30 }; // June
        yield return new object[] { new DateTime(2023, 07, 15), 31 }; // July
        yield return new object[] { new DateTime(2023, 08, 15), 31 }; // August
        yield return new object[] { new DateTime(2023, 09, 15), 30 }; // September
        yield return new object[] { new DateTime(2023, 10, 15), 31 }; // October
        yield return new object[] { new DateTime(2023, 11, 15), 30 }; // November
        yield return new object[] { new DateTime(2023, 12, 15), 31 }; // December

        // Leap year: 2024
        yield return new object[] { new DateTime(2024, 01, 15), 31 };
        yield return new object[] { new DateTime(2024, 02, 15), 29 }; // February (leap)
        yield return new object[] { new DateTime(2024, 03, 15), 31 };
        yield return new object[] { new DateTime(2024, 04, 15), 30 };
        yield return new object[] { new DateTime(2024, 05, 15), 31 };
        yield return new object[] { new DateTime(2024, 06, 15), 30 };
        yield return new object[] { new DateTime(2024, 07, 15), 31 };
        yield return new object[] { new DateTime(2024, 08, 15), 31 };
        yield return new object[] { new DateTime(2024, 09, 15), 30 };
        yield return new object[] { new DateTime(2024, 10, 15), 31 };
        yield return new object[] { new DateTime(2024, 11, 15), 30 };
        yield return new object[] { new DateTime(2024, 12, 15), 31 };
    }

    public static IEnumerable<object[]> DaysInYearGregorianCalendarTestData() =>
        DateTimeExtensionsTests.DaysInYearTestData()
            .Where(o => o[1] is GregorianCalendar)
            .Select(o => new object[] { new DateTime((int)o[0], 1, 1), o[2] });

    public static IEnumerable<object[]> DaysInYearTestData()
    {
        yield return new object[] { 1900, new GregorianCalendar(), 365 };
        yield return new object[] { 2000, new GregorianCalendar(), 366 };
        yield return new object[] { 2100, new GregorianCalendar(), 365 };
        yield return new object[] { 2400, new GregorianCalendar(), 366 };
        yield return new object[] { 2024, new GregorianCalendar(), 366 };
        yield return new object[] { 2023, new GregorianCalendar(), 365 };

        yield return new object[] { 2000, new JulianCalendar(), 366 };
        yield return new object[] { 2100, new JulianCalendar(), 366 };
        yield return new object[] { 2024, new JulianCalendar(), 366 };

        yield return new object[] { 5758, new HebrewCalendar(), 354 };
        yield return new object[] { 5776, new HebrewCalendar(), 385 };
        yield return new object[] { 5784, new HebrewCalendar(), 383 };
        yield return new object[] { 5786, new HebrewCalendar(), 354 };
        yield return new object[] { 5793, new HebrewCalendar(), 383 };
        yield return new object[] { 5802, new HebrewCalendar(), 354 };

        yield return new object[] { 1445, new HijriCalendar(), 355 };
        yield return new object[] { 1444, new HijriCalendar(), 354 };
        yield return new object[] { 1443, new HijriCalendar(), 354 };

        yield return new object[] { 1445, new UmAlQuraCalendar(), 354 };
        yield return new object[] { 1444, new UmAlQuraCalendar(), 354 };
        yield return new object[] { 1443, new UmAlQuraCalendar(), 355 };
    }

    public static IEnumerable<object[]> EndOfDayTestData() =>
        MidnightTestData()
            .Select(o => new object[] { o[0], ((DateTime)o[0]).Date.AddDays(1).AddTicks(-1) });

    public static IEnumerable<object[]> FirstDateOfMonthDataTestData()
    {
        // Regular months in 2023
        yield return new object[] { new DateTime(2023, 01, 15), new DateTime(2023, 01, 01) };
        yield return new object[] { new DateTime(2023, 02, 28), new DateTime(2023, 02, 01) };
        yield return new object[] { new DateTime(2023, 03, 31), new DateTime(2023, 03, 01) };
        yield return new object[] { new DateTime(2023, 04, 10), new DateTime(2023, 04, 01) };
        yield return new object[] { new DateTime(2023, 05, 01), new DateTime(2023, 05, 01) };
        yield return new object[] { new DateTime(2023, 06, 15), new DateTime(2023, 06, 01) };
        yield return new object[] { new DateTime(2023, 07, 25), new DateTime(2023, 07, 01) };
        yield return new object[] { new DateTime(2023, 08, 31), new DateTime(2023, 08, 01) };
        yield return new object[] { new DateTime(2023, 09, 05), new DateTime(2023, 09, 01) };
        yield return new object[] { new DateTime(2023, 10, 10), new DateTime(2023, 10, 01) };
        yield return new object[] { new DateTime(2023, 11, 30), new DateTime(2023, 11, 01) };
        yield return new object[] { new DateTime(2023, 12, 31), new DateTime(2023, 12, 01) };

        // Leap year (2024)
        yield return new object[] { new DateTime(2024, 01, 01), new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 02, 29), new DateTime(2024, 02, 01) };
        yield return new object[] { new DateTime(2024, 03, 01), new DateTime(2024, 03, 01) };
        yield return new object[] { new DateTime(2024, 04, 18), new DateTime(2024, 04, 01) };

        // Boundary checks
        yield return new object[] { DateTime.MinValue, new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, 1) };
        yield return new object[] { DateTime.MaxValue, new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, 1) };
    }

    public static IEnumerable<object[]> FirstDateOfQuarterDateTimeJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.FirstDateOfQuarterDateTimeTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[2] });

    public static IEnumerable<object[]> FirstDateOfQuarterDateTimeTestData()
    {
        // January–December
        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 10, 01) };

        // July–June
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 04, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 04, 01) };
        yield return new object[] { new DateTime(2025, 05, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 04, 01) };
        yield return new object[] { new DateTime(2025, 06, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 04, 01) };

        // April–March
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 07, 01) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 01, 01) };

        // October–September
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 10, 01) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 01, 01) };
        yield return new object[] { new DateTime(2025, 04, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 04, 01) };
        yield return new object[] { new DateTime(2025, 05, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 04, 01) };
        yield return new object[] { new DateTime(2025, 06, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 04, 01) };
        yield return new object[] { new DateTime(2025, 07, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 07, 01) };
        yield return new object[] { new DateTime(2025, 08, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 07, 01) };
        yield return new object[] { new DateTime(2025, 09, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 07, 01) };

        // February–January
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 02, 01) };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 02, 01) };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 02, 01) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 05, 01) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 05, 01) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 05, 01) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 08, 01) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 08, 01) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 08, 01) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 11, 01) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 11, 01) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 11, 01) };

        // date boundary tests
        yield return new object[] { new DateTime(2024, 04, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 07, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 07, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 06) };
        yield return new object[] { new DateTime(2024, 10, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 06) };
        yield return new object[] { new DateTime(2024, 10, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 06) };
        yield return new object[] { new DateTime(2025, 01, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 06) };
        yield return new object[] { new DateTime(2025, 01, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 06) };
        yield return new object[] { new DateTime(2025, 04, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 06) };

        yield return new object[] { new DateTime(2024, 03, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 03, 25) };
        yield return new object[] { new DateTime(2024, 06, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 03, 25) };
        yield return new object[] { new DateTime(2024, 06, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 25) };
        yield return new object[] { new DateTime(2024, 09, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 25) };
        yield return new object[] { new DateTime(2024, 09, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 25) };
        yield return new object[] { new DateTime(2024, 12, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 25) };
        yield return new object[] { new DateTime(2024, 12, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 25) };
        yield return new object[] { new DateTime(2025, 03, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 25) };
    }

    public static IEnumerable<object[]> FirstDateOfQuarterTestData()
    {
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 01, 01) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 04, 01) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 07, 01) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 10, 01) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 02, 01) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 05, 01) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 08, 01) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 11, 01) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 04, 01) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 07, 01) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 10, 01) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 01, 01) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 07, 01) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 10, 01) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 01, 01) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 04, 01) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 10, 01) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 01, 01) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 04, 01) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 07, 01) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 04, 06) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 06) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 06) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 06) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 03, 25) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 25) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 25) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 25) };
    }

    public static IEnumerable<object[]> FirstDateOfQuarterYearQuarterJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.FirstDateOfQuarterTestData()
            .Where(o => (CalendarQuarterDefinition)o[2] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[1], o[3] });

    public static IEnumerable<object[]> FirstDateOfWeekCultureInfoTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 14) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 15) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 14) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 14) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 13) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 15) };
    }

    public static IEnumerable<object[]> FirstDateOfWeekDefinitionTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 15) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 14) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 13) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 13) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 15) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 15) };
    }

    public static IEnumerable<object[]> FirstDateOfWeekInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, new DateTime(2024, 04, 02) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, new DateTime(2024, 04, 03) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, new DateTime(2024, 04, 04) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, new DateTime(2024, 04, 05) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, new DateTime(2024, 04, 06) };

        yield return new object[] { new DateTime(2024, 04, 07), DayOfWeek.Sunday, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 04, 02), DayOfWeek.Tuesday, new DateTime(2024, 04, 02) };
        yield return new object[] { new DateTime(2024, 04, 03), DayOfWeek.Wednesday, new DateTime(2024, 04, 03) };
        yield return new object[] { new DateTime(2024, 04, 04), DayOfWeek.Thursday, new DateTime(2024, 04, 04) };
        yield return new object[] { new DateTime(2024, 04, 05), DayOfWeek.Friday, new DateTime(2024, 04, 05) };
        yield return new object[] { new DateTime(2024, 04, 06), DayOfWeek.Saturday, new DateTime(2024, 04, 06) };

        yield return new object[] { DateTime.MinValue, DateTime.MinValue.DayOfWeek, DateTime.MinValue };
        yield return new object[] { DateTime.MaxValue.AddDays(-1), DayOfWeek.Monday, new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, 06) };
    }

    public static IEnumerable<object[]> FirstDateOfWeekInQuarterDateTimeJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.FirstDateOfWeekInQuarterDateTimeTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[2], o[3] });

    public static IEnumerable<object[]> FirstDateOfWeekInQuarterDateTimeTestData()
    {
        // JanuaryToDecember
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };

        // FebruaryToJanuary
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2023, 11, 5) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2023, 11, 6) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2023, 11, 7) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2023, 11, 1) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2023, 11, 2) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2023, 11, 3) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2023, 11, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 2, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 2, 5) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 2, 6) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 2, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 2, 1) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 2, 2) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 2, 3) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 2, 4) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 2, 5) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 2, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 2, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 2, 1) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 2, 2) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 2, 3) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 2, 4) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 2, 5) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 2, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 2, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 2, 1) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 2, 2) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 2, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 5, 5) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 5, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 5, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 5, 1) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 5, 2) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 5, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 5, 4) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 5, 5) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 5, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 5, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 5, 1) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 5, 2) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 5, 3) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 5, 4) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 5, 5) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 5, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 5, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 5, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 5, 2) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 5, 3) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 5, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 8, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 8, 5) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 8, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 8, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 8, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 8, 2) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 8, 3) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 8, 4) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 8, 5) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 8, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 8, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 8, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 8, 2) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 8, 3) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 8, 4) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 8, 5) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 8, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 8, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 8, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 8, 2) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 8, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 11, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 11, 4) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 11, 5) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 11, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 11, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 11, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 11, 2) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 11, 3) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 11, 4) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 11, 5) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 11, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 11, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 11, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 11, 2) };

        // March25ToMarch24
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2023, 12, 31) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2023, 12, 25) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2023, 12, 26) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2023, 12, 27) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2023, 12, 28) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2023, 12, 29) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2023, 12, 30) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2023, 12, 31) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2023, 12, 25) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2023, 12, 26) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2023, 12, 27) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2023, 12, 28) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2023, 12, 29) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2023, 12, 30) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2023, 12, 31) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2023, 12, 25) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2023, 12, 26) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2023, 12, 27) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2023, 12, 28) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2023, 12, 29) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2023, 12, 30) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 3, 31) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 3, 25) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 3, 26) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 3, 27) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 3, 28) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 3, 29) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 3, 30) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 3, 31) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 3, 25) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 3, 26) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 3, 27) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 3, 28) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 3, 29) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 3, 30) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 3, 31) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 3, 25) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 3, 26) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 3, 27) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 3, 28) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 3, 29) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 3, 30) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 6, 30) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 6, 25) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 6, 26) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 6, 27) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 6, 28) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 6, 29) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 6, 30) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 6, 25) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 6, 26) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 6, 27) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 6, 28) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 6, 29) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 6, 30) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 6, 25) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 6, 26) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 6, 27) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 6, 28) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 6, 29) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 9, 29) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 9, 30) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 9, 25) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 9, 26) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 9, 27) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 9, 28) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 9, 29) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 9, 30) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 9, 25) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 9, 26) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 9, 27) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 9, 28) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 9, 29) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 9, 30) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 9, 25) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 9, 26) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 9, 27) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 9, 28) };

        // AprilToMarch
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };

        // April6ToApril5
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2023, 10, 8) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2023, 10, 9) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2023, 10, 10) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2023, 10, 11) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2023, 10, 12) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2023, 10, 6) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2023, 10, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 1, 8) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 1, 9) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 1, 10) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 1, 11) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 1, 12) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 1, 8) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 1, 9) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 1, 10) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 1, 11) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 1, 12) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 1, 8) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 1, 9) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 1, 10) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 1, 11) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 1, 12) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 4, 8) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 4, 9) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 4, 10) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 4, 11) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 4, 12) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 4, 8) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 4, 9) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 4, 10) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 4, 11) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 4, 12) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 4, 8) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 4, 9) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 4, 10) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 4, 11) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 4, 12) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 7, 8) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 7, 9) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 7, 10) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 7, 11) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 7, 12) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 7, 8) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 7, 9) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 7, 10) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 7, 11) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 7, 12) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 7, 8) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 7, 9) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 7, 10) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 7, 11) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 7, 12) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 10, 8) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 10, 9) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 10, 10) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 10, 11) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 10, 12) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 10, 8) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 10, 9) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 10, 10) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 10, 11) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 10, 12) };

        // JulyToJune
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };

        // OctoberToSeptember
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 1, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 2, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { new DateTime(2024, 3, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 4, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 5, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { new DateTime(2024, 6, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 7, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 8, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { new DateTime(2024, 9, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 10, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 11, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { new DateTime(2024, 12, 1), CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
    }

    public static IEnumerable<object[]> FirstDateOfWeekInQuarterTestData()
    {
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 1, 1) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 1, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 1, 3) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 1, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 1, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 1, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 2, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 2, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 2, 6) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 2, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 2, 1) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 2, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 2, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 5, 5) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 5, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 5, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 5, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 5, 2) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 5, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 5, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 8, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 8, 5) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 8, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 8, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 8, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 8, 2) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 8, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Sunday, new DateTime(2024, 11, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Monday, new DateTime(2024, 11, 4) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Tuesday, new DateTime(2024, 11, 5) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Wednesday, new DateTime(2024, 11, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Thursday, new DateTime(2024, 11, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Friday, new DateTime(2024, 11, 1) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, DayOfWeek.Saturday, new DateTime(2024, 11, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 3, 31) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 3, 25) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 3, 26) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 3, 27) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 3, 28) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 3, 29) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 3, 30) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 6, 30) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 6, 25) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 6, 26) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 6, 27) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 6, 28) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 6, 29) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 9, 29) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 9, 30) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 9, 25) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 9, 26) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 9, 27) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 9, 28) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Sunday, new DateTime(2024, 12, 29) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Monday, new DateTime(2024, 12, 30) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Tuesday, new DateTime(2024, 12, 31) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Wednesday, new DateTime(2024, 12, 25) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Thursday, new DateTime(2024, 12, 26) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Friday, new DateTime(2024, 12, 27) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, DayOfWeek.Saturday, new DateTime(2024, 12, 28) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 4, 1) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 4, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 4, 3) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 4, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 4, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Sunday, new DateTime(2025, 1, 5) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Monday, new DateTime(2025, 1, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Tuesday, new DateTime(2025, 1, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Wednesday, new DateTime(2025, 1, 1) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Thursday, new DateTime(2025, 1, 2) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Friday, new DateTime(2025, 1, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, DayOfWeek.Saturday, new DateTime(2025, 1, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 4, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 4, 8) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 4, 9) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 4, 10) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 4, 11) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 4, 12) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 4, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 7, 8) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 7, 9) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 7, 10) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 7, 11) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 7, 12) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2024, 10, 8) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2024, 10, 9) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2024, 10, 10) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2024, 10, 11) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2024, 10, 12) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Sunday, new DateTime(2025, 1, 12) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Monday, new DateTime(2025, 1, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Tuesday, new DateTime(2025, 1, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Wednesday, new DateTime(2025, 1, 8) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Thursday, new DateTime(2025, 1, 9) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Friday, new DateTime(2025, 1, 10) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, DayOfWeek.Saturday, new DateTime(2025, 1, 11) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 7, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 7, 1) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 7, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 7, 3) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 7, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 7, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 7, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2025, 1, 5) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2025, 1, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2025, 1, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2025, 1, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2025, 1, 2) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2025, 1, 3) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2025, 1, 4) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Sunday, new DateTime(2025, 4, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Monday, new DateTime(2025, 4, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Tuesday, new DateTime(2025, 4, 1) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Wednesday, new DateTime(2025, 4, 2) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Thursday, new DateTime(2025, 4, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Friday, new DateTime(2025, 4, 4) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, DayOfWeek.Saturday, new DateTime(2025, 4, 5) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2024, 10, 6) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2024, 10, 7) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2024, 10, 2) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2024, 10, 3) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2024, 10, 4) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2024, 10, 5) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2025, 1, 5) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2025, 1, 6) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2025, 1, 7) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2025, 1, 1) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2025, 1, 2) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2025, 1, 3) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2025, 1, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2025, 4, 6) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2025, 4, 7) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2025, 4, 1) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2025, 4, 2) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2025, 4, 3) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2025, 4, 4) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2025, 4, 5) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Sunday, new DateTime(2025, 7, 6) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Monday, new DateTime(2025, 7, 7) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Tuesday, new DateTime(2025, 7, 1) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Wednesday, new DateTime(2025, 7, 2) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Thursday, new DateTime(2025, 7, 3) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Friday, new DateTime(2025, 7, 4) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, DayOfWeek.Saturday, new DateTime(2025, 7, 5) };
    }

    public static IEnumerable<object[]> FirstDateOfWeekInQuarterYearQuarterJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.FirstDateOfWeekInQuarterTestData()
            .Where(o => (CalendarQuarterDefinition)o[2] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[1], o[3], o[4] });

    public static IEnumerable<object[]> FirstDateOfWeekInYearTestData()
    {
        yield return new object[] { new DateTime(2024, 10, 15), DayOfWeek.Sunday, new DateTime(2024, 01, 07) };
        yield return new object[] { new DateTime(2024, 06, 15), DayOfWeek.Monday, new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 11, 19), DayOfWeek.Tuesday, new DateTime(2024, 01, 02) };
        yield return new object[] { new DateTime(2024, 05, 27), DayOfWeek.Wednesday, new DateTime(2024, 01, 03) };
        yield return new object[] { new DateTime(2024, 08, 19), DayOfWeek.Thursday, new DateTime(2024, 01, 04) };
        yield return new object[] { new DateTime(2024, 03, 27), DayOfWeek.Friday, new DateTime(2024, 01, 05) };
        yield return new object[] { new DateTime(2024, 04, 19), DayOfWeek.Saturday, new DateTime(2024, 01, 06) };

        yield return new object[] { new DateTime(2024, 01, 07), DayOfWeek.Sunday, new DateTime(2024, 01, 07) };
        yield return new object[] { new DateTime(2024, 01, 01), DayOfWeek.Monday, new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 01, 02), DayOfWeek.Tuesday, new DateTime(2024, 01, 02) };
        yield return new object[] { new DateTime(2024, 01, 03), DayOfWeek.Wednesday, new DateTime(2024, 01, 03) };
        yield return new object[] { new DateTime(2024, 01, 04), DayOfWeek.Thursday, new DateTime(2024, 01, 04) };
        yield return new object[] { new DateTime(2024, 01, 05), DayOfWeek.Friday, new DateTime(2024, 01, 05) };
        yield return new object[] { new DateTime(2024, 01, 06), DayOfWeek.Saturday, new DateTime(2024, 01, 06) };

        yield return new object[] { DateTime.MinValue, DateTime.MinValue.DayOfWeek, DateTime.MinValue };
        yield return new object[] { DateTime.MaxValue, DayOfWeek.Monday, new DateTime(DateTime.MaxValue.Year, 01, 04) };
    }

    public static IEnumerable<object[]> FirstDateOfYearTestData()
    {
        yield return new object[] { new DateTime(2023, 02, 28), new DateTime(2023, 01, 01) };
        yield return new object[] { new DateTime(2024, 10, 15), new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 06, 15), new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 02, 29), new DateTime(2024, 01, 01) };
        yield return new object[] { new DateTime(2024, 08, 19), new DateTime(2024, 01, 01) };

        yield return new object[] { new DateTime(2023, 01, 01), new DateTime(2023, 01, 01) };
        yield return new object[] { new DateTime(2024, 01, 01), new DateTime(2024, 01, 01) };

        yield return new object[] { DateTime.MinValue, DateTime.MinValue.Date };
        yield return new object[] { DateTime.MaxValue, new DateTime(DateTime.MaxValue.Year, 01, 01) };
    }

    public static IEnumerable<object[]> IsFirstDateOfMonthTestData() =>
        FirstDateOfMonthDataTestData()
            .Select(o => new object[] { o[1] });

    public static IEnumerable<object[]> IsFirstDateOfQuarterJanuaryDecemberTestData() =>
        IsFirstDateOfQuarterTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0] });

    public static IEnumerable<object[]> IsFirstDateOfQuarterTestData() =>
        FirstDateOfQuarterTestData()
            .Select(o => new object[] { o[3], o[2] });

    public static IEnumerable<object[]> IsLastDateOfMonthDataTestData() =>
        LastDateOfMonthDataTestData()
            .Select(o => new object[] { o[1] });

    public static IEnumerable<object[]> IsLastDateOfQuarterJanuaryDecemberTestData() =>
        LastDateOfQuarterDateTimeTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], (DateTime)o[0] == (DateTime)o[2] });

    public static IEnumerable<object[]> IsLastDateOfQuarterTestData() =>
        LastDateOfQuarterTestData()
            .Select(o => new object[] { o[3], o[2] });

    public static IEnumerable<object[]> IsNotFirstDateOfMonthTestData() =>
        Enumerable.Range(1, 12)
            .SelectMany(month =>
            {
                var daysInMonth = DateTime.DaysInMonth(2024, month);
                return Enumerable.Range(2, daysInMonth - 1) //
                                 .Select(day => new object[] { new DateTime(2024, month, day) });
            });

    public static IEnumerable<object[]> IsNotFirstDateOfQuarterTestData() =>
        FirstDateOfQuarterDateTimeTestData()
            .Where(o => (DateTime)o[0] != (DateTime)o[2])
            .Select(o => new object[] { o[0], o[1] });

    public static IEnumerable<object[]> IsNotLastDateOfMonthTestData() =>
        Enumerable.Range(1, 12)
            .SelectMany(month =>
            {
                var daysInMonth = DateTime.DaysInMonth(2024, month);
                return Enumerable.Range(1, daysInMonth - 2)
                                 .Select(day => new object[] { new DateTime(2024, month, day) });
            });

    public static IEnumerable<object[]> IsNotLastDateOfQuarterTestData() =>
                LastDateOfQuarterDateTimeTestData()
            .Where(o => (DateTime)o[0] != (DateTime)o[2])
            .Select(o => new object[] { o[0], o[1] });

    public static IEnumerable<object[]> LastDateOfMonthDataTestData()
    {
        // Regular months in 2023
        yield return new object[] { new DateTime(2023, 01, 15), new DateTime(2023, 01, 31) };
        yield return new object[] { new DateTime(2023, 02, 28), new DateTime(2023, 02, 28) };
        yield return new object[] { new DateTime(2023, 03, 31), new DateTime(2023, 03, 31) };
        yield return new object[] { new DateTime(2023, 04, 10), new DateTime(2023, 04, 30) };
        yield return new object[] { new DateTime(2023, 05, 01), new DateTime(2023, 05, 31) };
        yield return new object[] { new DateTime(2023, 06, 15), new DateTime(2023, 06, 30) };
        yield return new object[] { new DateTime(2023, 07, 25), new DateTime(2023, 07, 31) };
        yield return new object[] { new DateTime(2023, 08, 31), new DateTime(2023, 08, 31) };
        yield return new object[] { new DateTime(2023, 09, 05), new DateTime(2023, 09, 30) };
        yield return new object[] { new DateTime(2023, 10, 10), new DateTime(2023, 10, 31) };
        yield return new object[] { new DateTime(2023, 11, 30), new DateTime(2023, 11, 30) };
        yield return new object[] { new DateTime(2023, 12, 31), new DateTime(2023, 12, 31) };

        // Leap year (2024)
        yield return new object[] { new DateTime(2024, 01, 01), new DateTime(2024, 01, 31) };
        yield return new object[] { new DateTime(2024, 02, 29), new DateTime(2024, 02, 29) };
        yield return new object[] { new DateTime(2024, 03, 01), new DateTime(2024, 03, 31) };
        yield return new object[] { new DateTime(2024, 04, 18), new DateTime(2024, 04, 30) };

        // Boundary checks
        yield return new object[] { DateTime.MinValue, new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.DaysInMonth(DateTime.MinValue.Year, DateTime.MinValue.Month)) };
        yield return new object[] { DateTime.MaxValue, new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, DateTime.DaysInMonth(DateTime.MaxValue.Year, DateTime.MaxValue.Month)) };
    }

    public static IEnumerable<object[]> LastDateOfQuarterDateTimeJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.LastDateOfQuarterDateTimeTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[2] });

    public static IEnumerable<object[]> LastDateOfQuarterDateTimeTestData()
    {
        // January–December
        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 03, 31) };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 03, 31) };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 03, 31) };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 12, 31) };

        // July–June
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 04, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 06, 30) };
        yield return new object[] { new DateTime(2025, 05, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 06, 30) };
        yield return new object[] { new DateTime(2025, 06, 01), CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 06, 30) };

        // April–March
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 06, 30) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 09, 30) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 03, 31) };

        // October–September
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 02, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 03, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 03, 31) };
        yield return new object[] { new DateTime(2025, 04, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 06, 30) };
        yield return new object[] { new DateTime(2025, 05, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 06, 30) };
        yield return new object[] { new DateTime(2025, 06, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 06, 30) };
        yield return new object[] { new DateTime(2025, 07, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 09, 30) };
        yield return new object[] { new DateTime(2025, 08, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 09, 30) };
        yield return new object[] { new DateTime(2025, 09, 01), CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 09, 30) };

        // February–January
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 07, 31) };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 07, 31) };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 07, 31) };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 10, 31) };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 10, 31) };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 10, 31) };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2025, 01, 31) };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2025, 01, 31) };
        yield return new object[] { new DateTime(2025, 01, 01), CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2025, 01, 31) };

        // date boundary tests
        yield return new object[] { new DateTime(2024, 04, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 05) };
        yield return new object[] { new DateTime(2024, 07, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 05) };
        yield return new object[] { new DateTime(2024, 07, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 05) };
        yield return new object[] { new DateTime(2024, 10, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 05) };
        yield return new object[] { new DateTime(2024, 10, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 05) };
        yield return new object[] { new DateTime(2025, 01, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 05) };
        yield return new object[] { new DateTime(2025, 01, 06), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 04, 05) };
        yield return new object[] { new DateTime(2025, 04, 05), CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 04, 05) };

        yield return new object[] { new DateTime(2024, 03, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 24) };
        yield return new object[] { new DateTime(2024, 06, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 24) };
        yield return new object[] { new DateTime(2024, 06, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 24) };
        yield return new object[] { new DateTime(2024, 09, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 24) };
        yield return new object[] { new DateTime(2024, 09, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 24) };
        yield return new object[] { new DateTime(2024, 12, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 24) };
        yield return new object[] { new DateTime(2024, 12, 25), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2025, 03, 24) };
        yield return new object[] { new DateTime(2025, 03, 24), CalendarQuarterDefinition.March25ToMarch24, new DateTime(2025, 03, 24) };
    }

    public static IEnumerable<object[]> LastDateOfQuarterJanuaryDecemberTestData() =>
        DateTimeExtensionsTests.LastDateOfQuarterTestData()
            .Where(o => (CalendarQuarterDefinition)o[2] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[1], o[3] });

    public static IEnumerable<object[]> LastDateOfQuarterTestData()
    {
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 03, 31) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 06, 30) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 09, 30) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JanuaryToDecember, new DateTime(2024, 12, 31) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 04, 30) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 07, 31) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2024, 10, 31) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.FebruaryToJanuary, new DateTime(2025, 01, 31) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 06, 30) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 09, 30) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.AprilToMarch, new DateTime(2024, 12, 31) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.AprilToMarch, new DateTime(2025, 03, 31) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 09, 30) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.JulyToJune, new DateTime(2024, 12, 31) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 03, 31) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.JulyToJune, new DateTime(2025, 06, 30) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2024, 12, 31) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 03, 31) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 06, 30) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.OctoberToSeptember, new DateTime(2025, 09, 30) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 07, 05) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.April6ToApril5, new DateTime(2024, 10, 05) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 01, 05) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.April6ToApril5, new DateTime(2025, 04, 05) };
        yield return new object[] { 2024, 1, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 06, 24) };
        yield return new object[] { 2024, 2, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 09, 24) };
        yield return new object[] { 2024, 3, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2024, 12, 24) };
        yield return new object[] { 2024, 4, CalendarQuarterDefinition.March25ToMarch24, new DateTime(2025, 03, 24) };
    }

    public static IEnumerable<object[]> LastDateOfWeekCultureInfoTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 20) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("en-US"), new DateTime(2024, 04, 20) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("en-GB"), new DateTime(2024, 04, 21) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 20) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("ar-SA"), new DateTime(2024, 04, 20) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 20) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("he-IL"), new DateTime(2024, 04, 20) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("fa-IR"), new DateTime(2024, 04, 19) };

        yield return new object[] { new DateTime(2024, 04, 08), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 09), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 10), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 11), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 12), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 13), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 14), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), CultureInfo.GetCultureInfo("ru-RU"), new DateTime(2024, 04, 21) };
    }

    public static IEnumerable<object[]> LastDateOfWeekDefinitionTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.MondayToFriday, new DateTime(2024, 04, 21) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 20) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SundayToThursday, new DateTime(2024, 04, 20) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SaturdayToWednesday, new DateTime(2024, 04, 19) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.SaturdayToThursday, new DateTime(2024, 04, 19) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.MondayToSaturday, new DateTime(2024, 04, 21) };

        yield return new object[] { new DateTime(2024, 04, 08), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 09), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 10), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 11), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 12), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 13), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 14), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 15), WorkingDaysOfWeek.AllDays, new DateTime(2024, 04, 21) };
    }

    public static IEnumerable<object[]> LastDateOfWeekInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Sunday, new DateTime(2024, 04, 28) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Monday, new DateTime(2024, 04, 29) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Tuesday, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Wednesday, new DateTime(2024, 04, 24) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Thursday, new DateTime(2024, 04, 25) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Friday, new DateTime(2024, 04, 26) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Saturday, new DateTime(2024, 04, 27) };

        yield return new object[] { new DateTime(2024, 04, 28), DayOfWeek.Sunday, new DateTime(2024, 04, 28) };
        yield return new object[] { new DateTime(2024, 04, 29), DayOfWeek.Monday, new DateTime(2024, 04, 29) };
        yield return new object[] { new DateTime(2024, 04, 30), DayOfWeek.Tuesday, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 04, 24), DayOfWeek.Wednesday, new DateTime(2024, 04, 24) };
        yield return new object[] { new DateTime(2024, 04, 25), DayOfWeek.Thursday, new DateTime(2024, 04, 25) };
        yield return new object[] { new DateTime(2024, 04, 26), DayOfWeek.Friday, new DateTime(2024, 04, 26) };
        yield return new object[] { new DateTime(2024, 04, 27), DayOfWeek.Saturday, new DateTime(2024, 04, 27) };

        yield return new object[] { DateTime.MinValue, DateTime.MinValue.DayOfWeek, new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, 29) };
        yield return new object[] { DateTime.MaxValue, DateTime.MaxValue.DayOfWeek, DateTime.MaxValue.Date };
    }

    public static IEnumerable<object[]> LastDateOfWeekInYearTestData()
    {
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Sunday, new DateTime(2024, 12, 29) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Monday, new DateTime(2024, 12, 30) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Tuesday, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Wednesday, new DateTime(2024, 12, 25) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Thursday, new DateTime(2024, 12, 26) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Friday, new DateTime(2024, 12, 27) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Saturday, new DateTime(2024, 12, 28) };

        yield return new object[] { new DateTime(2024, 12, 29), DayOfWeek.Sunday, new DateTime(2024, 12, 29) };
        yield return new object[] { new DateTime(2024, 12, 30), DayOfWeek.Monday, new DateTime(2024, 12, 30) };
        yield return new object[] { new DateTime(2024, 12, 31), DayOfWeek.Tuesday, new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 12, 25), DayOfWeek.Wednesday, new DateTime(2024, 12, 25) };
        yield return new object[] { new DateTime(2024, 12, 26), DayOfWeek.Thursday, new DateTime(2024, 12, 26) };
        yield return new object[] { new DateTime(2024, 12, 27), DayOfWeek.Friday, new DateTime(2024, 12, 27) };
        yield return new object[] { new DateTime(2024, 12, 28), DayOfWeek.Saturday, new DateTime(2024, 12, 28) };

        yield return new object[] { DateTime.MinValue, DayOfWeek.Monday, new DateTime(DateTime.MinValue.Year, 12, 31) };
        yield return new object[] { DateTime.MaxValue, DateTime.MaxValue.DayOfWeek, DateTime.MaxValue.Date };
    }

    public static IEnumerable<object[]> LastDateOfYearTestData()
    {
        yield return new object[] { new DateTime(2023, 02, 28), new DateTime(2023, 12, 31) };
        yield return new object[] { new DateTime(2024, 10, 15), new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 06, 15), new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 02, 29), new DateTime(2024, 12, 31) };
        yield return new object[] { new DateTime(2024, 08, 19), new DateTime(2024, 12, 31) };

        yield return new object[] { new DateTime(2023, 01, 01), new DateTime(2023, 12, 31) };
        yield return new object[] { new DateTime(2024, 01, 01), new DateTime(2024, 12, 31) };

        yield return new object[] { DateTime.MinValue, new DateTime(DateTime.MinValue.Year, 12, 31) };
        yield return new object[] { DateTime.MaxValue, DateTime.MaxValue.Date };
    }

    public static IEnumerable<object[]> LeapYearTestData()
    {
        yield return new object[] { 1600, true };   // Leap year (divisible by 400)
        yield return new object[] { 1700, false };  // Not a leap year (divisible by 100 but not 400)
        yield return new object[] { 1800, false };
        yield return new object[] { 1900, false };
        yield return new object[] { 2000, true };   // Leap year (divisible by 400)
        yield return new object[] { 2004, true };   // Typical leap year (divisible by 4)
        yield return new object[] { 2015, false };  // Normal common year
        yield return new object[] { 2016, true };
        yield return new object[] { 2023, false };
        yield return new object[] { 2024, true };
        yield return new object[] { 2100, false };  // Century non-leap year
        yield return new object[] { 2400, true };   // Century leap year
        yield return new object[] { 1, false };     // Minimum supported year
        yield return new object[] { 9999, false };  // Maximum supported DateTime year
    }

    public static IEnumerable<object[]> MiddayTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 01, 00, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 01, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 02, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 03, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 04, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 05, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 06, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 07, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 08, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 09, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 10, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 11, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 12, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 13, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 14, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 15, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 16, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 17, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 18, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 19, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 20, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 21, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 22, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 23, 15, 15), new DateTime(2024, 04, 01, 12, 00, 00) };

        yield return new object[] { new DateTime(2024, 04, 01, 23, 59, 59, 999, 999), new DateTime(2024, 04, 01, 12, 00, 00, 00, 00) };

        yield return new object[] { new DateTime(2024, 02, 29, 23, 15, 15), new DateTime(2024, 02, 29, 12, 00, 00) };
    }

    public static IEnumerable<object[]> MidnightTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 01, 00, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 01, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 02, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 03, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 04, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 05, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 06, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 07, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 08, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 09, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 10, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 11, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 12, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 13, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 14, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 15, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 16, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 17, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 18, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 19, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 20, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 21, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 22, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 01, 23, 15, 15), new DateTime(2024, 04, 01, 00, 00, 00) };

        yield return new object[] { new DateTime(2024, 04, 01, 23, 59, 59, 999, 999), new DateTime(2024, 04, 01, 00, 00, 00, 00, 00) };

        yield return new object[] { new DateTime(2024, 02, 29, 23, 15, 15), new DateTime(2024, 02, 29, 00, 00, 00) };
    }

    public static IEnumerable<object[]> MonthNameFrenchTestData() =>
        MonthNameTestData()
            .Where(o => ((CultureInfo)o[2]).Name == "fr-FR")
            .Select(o => new object[] { o[0], o[1], o[3] });

    public static IEnumerable<object[]> MonthNameTestData()
    {
        yield return new object[] { 2024, 1, new CultureInfo("en-US"), "January" };
        yield return new object[] { 2024, 2, new CultureInfo("en-US"), "February" };
        yield return new object[] { 2024, 3, new CultureInfo("en-US"), "March" };
        yield return new object[] { 2024, 4, new CultureInfo("en-US"), "April" };
        yield return new object[] { 2024, 5, new CultureInfo("en-US"), "May" };
        yield return new object[] { 2024, 6, new CultureInfo("en-US"), "June" };
        yield return new object[] { 2024, 7, new CultureInfo("en-US"), "July" };
        yield return new object[] { 2024, 8, new CultureInfo("en-US"), "August" };
        yield return new object[] { 2024, 9, new CultureInfo("en-US"), "September" };
        yield return new object[] { 2024, 10, new CultureInfo("en-US"), "October" };
        yield return new object[] { 2024, 11, new CultureInfo("en-US"), "November" };
        yield return new object[] { 2024, 12, new CultureInfo("en-US"), "December" };

        yield return new object[] { 2024, 1, new CultureInfo("de-DE"), "Januar" };
        yield return new object[] { 2024, 2, new CultureInfo("de-DE"), "Februar" };
        yield return new object[] { 2024, 3, new CultureInfo("de-DE"), "März" };
        yield return new object[] { 2024, 4, new CultureInfo("de-DE"), "April" };
        yield return new object[] { 2024, 5, new CultureInfo("de-DE"), "Mai" };
        yield return new object[] { 2024, 6, new CultureInfo("de-DE"), "Juni" };
        yield return new object[] { 2024, 7, new CultureInfo("de-DE"), "Juli" };
        yield return new object[] { 2024, 8, new CultureInfo("de-DE"), "August" };
        yield return new object[] { 2024, 9, new CultureInfo("de-DE"), "September" };
        yield return new object[] { 2024, 10, new CultureInfo("de-DE"), "Oktober" };
        yield return new object[] { 2024, 11, new CultureInfo("de-DE"), "November" };
        yield return new object[] { 2024, 12, new CultureInfo("de-DE"), "Dezember" };

        yield return new object[] { 2024, 1, new CultureInfo("fr-FR"), "janvier" };
        yield return new object[] { 2024, 2, new CultureInfo("fr-FR"), "février" };
        yield return new object[] { 2024, 3, new CultureInfo("fr-FR"), "mars" };
        yield return new object[] { 2024, 4, new CultureInfo("fr-FR"), "avril" };
        yield return new object[] { 2024, 5, new CultureInfo("fr-FR"), "mai" };
        yield return new object[] { 2024, 6, new CultureInfo("fr-FR"), "juin" };
        yield return new object[] { 2024, 7, new CultureInfo("fr-FR"), "juillet" };
        yield return new object[] { 2024, 8, new CultureInfo("fr-FR"), "août" };
        yield return new object[] { 2024, 9, new CultureInfo("fr-FR"), "septembre" };
        yield return new object[] { 2024, 10, new CultureInfo("fr-FR"), "octobre" };
        yield return new object[] { 2024, 11, new CultureInfo("fr-FR"), "novembre" };
        yield return new object[] { 2024, 12, new CultureInfo("fr-FR"), "décembre" };

        yield return new object[] { 2024, 1, new CultureInfo("it-IT"), "gennaio" };
        yield return new object[] { 2024, 2, new CultureInfo("it-IT"), "febbraio" };
        yield return new object[] { 2024, 3, new CultureInfo("it-IT"), "marzo" };
        yield return new object[] { 2024, 4, new CultureInfo("it-IT"), "aprile" };
        yield return new object[] { 2024, 5, new CultureInfo("it-IT"), "maggio" };
        yield return new object[] { 2024, 6, new CultureInfo("it-IT"), "giugno" };
        yield return new object[] { 2024, 7, new CultureInfo("it-IT"), "luglio" };
        yield return new object[] { 2024, 8, new CultureInfo("it-IT"), "agosto" };
        yield return new object[] { 2024, 9, new CultureInfo("it-IT"), "settembre" };
        yield return new object[] { 2024, 10, new CultureInfo("it-IT"), "ottobre" };
        yield return new object[] { 2024, 11, new CultureInfo("it-IT"), "novembre" };
        yield return new object[] { 2024, 12, new CultureInfo("it-IT"), "dicembre" };

        yield return new object[] { 2024, 1, new CultureInfo("ru-RU"), "январь" };
        yield return new object[] { 2024, 2, new CultureInfo("ru-RU"), "февраль" };
        yield return new object[] { 2024, 3, new CultureInfo("ru-RU"), "март" };
        yield return new object[] { 2024, 4, new CultureInfo("ru-RU"), "апрель" };
        yield return new object[] { 2024, 5, new CultureInfo("ru-RU"), "май" };
        yield return new object[] { 2024, 6, new CultureInfo("ru-RU"), "июнь" };
        yield return new object[] { 2024, 7, new CultureInfo("ru-RU"), "июль" };
        yield return new object[] { 2024, 8, new CultureInfo("ru-RU"), "август" };
        yield return new object[] { 2024, 9, new CultureInfo("ru-RU"), "сентябрь" };
        yield return new object[] { 2024, 10, new CultureInfo("ru-RU"), "октябрь" };
        yield return new object[] { 2024, 11, new CultureInfo("ru-RU"), "ноябрь" };
        yield return new object[] { 2024, 12, new CultureInfo("ru-RU"), "декабрь" };

        yield return new object[] { 2024, 1, new CultureInfo("nb-NO"), "januar" };
        yield return new object[] { 2024, 2, new CultureInfo("nb-NO"), "februar" };
        yield return new object[] { 2024, 3, new CultureInfo("nb-NO"), "mars" };
        yield return new object[] { 2024, 4, new CultureInfo("nb-NO"), "april" };
        yield return new object[] { 2024, 5, new CultureInfo("nb-NO"), "mai" };
        yield return new object[] { 2024, 6, new CultureInfo("nb-NO"), "juni" };
        yield return new object[] { 2024, 7, new CultureInfo("nb-NO"), "juli" };
        yield return new object[] { 2024, 8, new CultureInfo("nb-NO"), "august" };
        yield return new object[] { 2024, 9, new CultureInfo("nb-NO"), "september" };
        yield return new object[] { 2024, 10, new CultureInfo("nb-NO"), "oktober" };
        yield return new object[] { 2024, 11, new CultureInfo("nb-NO"), "november" };
        yield return new object[] { 2024, 12, new CultureInfo("nb-NO"), "desember" };
    }

    public static IEnumerable<object[]> NextDateOfWeekTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 19, 02, 30, 10), DayOfWeek.Monday, new DateTime(2024, 04, 22, 02, 30, 10) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Tuesday, new DateTime(2024, 04, 16, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Wednesday, new DateTime(2024, 04, 17, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 18, 17, 45, 00), DayOfWeek.Thursday, new DateTime(2024, 04, 25, 17, 45, 00) };
        yield return new object[] { new DateTime(2024, 04, 18, 08, 15, 30), DayOfWeek.Friday, new DateTime(2024, 04, 19, 08, 15, 30) };
        yield return new object[] { new DateTime(2024, 04, 21, 23, 59, 59), DayOfWeek.Saturday, new DateTime(2024, 04, 27, 23, 59, 59) };
        yield return new object[] { new DateTime(2024, 04, 15, 06, 33, 44), DayOfWeek.Sunday, new DateTime(2024, 04, 21, 06, 33, 44) };
    }

    public static IEnumerable<object[]> NthDateOfWeekInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 02) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 09) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 16) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 23) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Fifth, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 30) };

        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 07) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 01) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 02) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 03) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 04) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 05) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, WeekOfMonthOrdinal.First, new DateTime(2024, 04, 06) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 14) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 08) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 09) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 10) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 11) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 12) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, WeekOfMonthOrdinal.Second, new DateTime(2024, 04, 13) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 21) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 15) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 16) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 17) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 18) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 19) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, WeekOfMonthOrdinal.Third, new DateTime(2024, 04, 20) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 28) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 22) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 23) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 24) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 25) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 26) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, WeekOfMonthOrdinal.Fourth, new DateTime(2024, 04, 27) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Sunday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 28) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Monday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 29) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 30) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Wednesday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 24) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 25) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Friday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 26) };
        yield return new object[] { new DateTime(2024, 04, 01), DayOfWeek.Saturday, WeekOfMonthOrdinal.Last, new DateTime(2024, 04, 27) };

        yield return new object[] { new DateTime(2024, 02, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.First, new DateTime(2024, 02, 01) }; // Feb 1st Thursday
        yield return new object[] { new DateTime(2024, 02, 01), DayOfWeek.Thursday, WeekOfMonthOrdinal.Last, new DateTime(2024, 02, 29) };  // Leap year - last Thursday
        yield return new object[] { new DateTime(2023, 02, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.First, new DateTime(2023, 02, 07) };  // Non-leap Feb - last Sunday
        yield return new object[] { new DateTime(2023, 02, 01), DayOfWeek.Tuesday, WeekOfMonthOrdinal.Last, new DateTime(2023, 02, 28) };   // Non-Leap year - last Thursday
    }

    public static IEnumerable<object[]> PreviousDateOfWeekTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 18, 14, 30, 00), DayOfWeek.Monday, new DateTime(2024, 04, 15, 14, 30, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 05, 00, 00), DayOfWeek.Tuesday, new DateTime(2024, 04, 16, 05, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 05, 00, 00), DayOfWeek.Wednesday, new DateTime(2024, 04, 17, 05, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 18, 14, 30, 00), DayOfWeek.Thursday, new DateTime(2024, 04, 11, 14, 30, 00) };
        yield return new object[] { new DateTime(2024, 04, 19, 03, 00, 00), DayOfWeek.Friday, new DateTime(2024, 04, 12, 03, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 19, 03, 00, 00), DayOfWeek.Saturday, new DateTime(2024, 04, 13, 03, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 23, 59, 59), DayOfWeek.Sunday, new DateTime(2024, 04, 14, 23, 59, 59) };
    }

    public static IEnumerable<object[]> NextOrSameDateOfWeekTestData()
    {
        // Same-day rows — the critical difference from the strict NextDateOfWeek variant: the input is returned unchanged.
        yield return new object[] { new DateTime(2024, 04, 15, 10, 00, 00), DayOfWeek.Monday, new DateTime(2024, 04, 15, 10, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 19, 23, 59, 59), DayOfWeek.Friday, new DateTime(2024, 04, 19, 23, 59, 59) };
        yield return new object[] { new DateTime(2024, 04, 21, 00, 00, 00), DayOfWeek.Sunday, new DateTime(2024, 04, 21, 00, 00, 00) };

        // Forward-by-N rows — the input is on Monday 2024-04-15; each target day advances by its weekday delta.
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Tuesday, new DateTime(2024, 04, 16, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Wednesday, new DateTime(2024, 04, 17, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Thursday, new DateTime(2024, 04, 18, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Friday, new DateTime(2024, 04, 19, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Saturday, new DateTime(2024, 04, 20, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 15, 12, 00, 00), DayOfWeek.Sunday, new DateTime(2024, 04, 21, 12, 00, 00) };
    }

    public static IEnumerable<object[]> PreviousOrSameDateOfWeekTestData()
    {
        // Same-day rows — the critical difference from the strict PreviousDateOfWeek variant: the input is returned unchanged.
        yield return new object[] { new DateTime(2024, 04, 15, 10, 00, 00), DayOfWeek.Monday, new DateTime(2024, 04, 15, 10, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 19, 23, 59, 59), DayOfWeek.Friday, new DateTime(2024, 04, 19, 23, 59, 59) };
        yield return new object[] { new DateTime(2024, 04, 21, 00, 00, 00), DayOfWeek.Sunday, new DateTime(2024, 04, 21, 00, 00, 00) };

        // Backward-by-N rows — the input is on Sunday 2024-04-21; each target day retreats by its weekday delta.
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Saturday, new DateTime(2024, 04, 20, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Friday, new DateTime(2024, 04, 19, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Thursday, new DateTime(2024, 04, 18, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Wednesday, new DateTime(2024, 04, 17, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Tuesday, new DateTime(2024, 04, 16, 12, 00, 00) };
        yield return new object[] { new DateTime(2024, 04, 21, 12, 00, 00), DayOfWeek.Monday, new DateTime(2024, 04, 15, 12, 00, 00) };
    }

    public static IEnumerable<object[]> QuarterJanuaryDecemberTestData() =>
                                                                        DateTimeExtensionsTests.QuarterTestData()
            .Where(o => (CalendarQuarterDefinition)o[1] == CalendarQuarterDefinition.JanuaryToDecember)
            .Select(o => new object[] { o[0], o[2] });

    public static IEnumerable<object[]> QuarterTestData()
    {
        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 01, 31), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarQuarterDefinition.JanuaryToDecember, 1 };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 04, 30), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 05, 31), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 06, 30), CalendarQuarterDefinition.JanuaryToDecember, 2 };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 07, 31), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 08, 31), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 09, 30), CalendarQuarterDefinition.JanuaryToDecember, 3 };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JanuaryToDecember, 4 };
        yield return new object[] { new DateTime(2024, 10, 31), CalendarQuarterDefinition.JanuaryToDecember, 4 };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JanuaryToDecember, 4 };
        yield return new object[] { new DateTime(2024, 11, 30), CalendarQuarterDefinition.JanuaryToDecember, 4 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JanuaryToDecember, 4 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarQuarterDefinition.JanuaryToDecember, 4 };

        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.FebruaryToJanuary, 4 };
        yield return new object[] { new DateTime(2024, 01, 31), CalendarQuarterDefinition.FebruaryToJanuary, 4 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 04, 30), CalendarQuarterDefinition.FebruaryToJanuary, 1 };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 05, 31), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 06, 30), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 07, 31), CalendarQuarterDefinition.FebruaryToJanuary, 2 };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 08, 31), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 09, 30), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 10, 31), CalendarQuarterDefinition.FebruaryToJanuary, 3 };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.FebruaryToJanuary, 4 };
        yield return new object[] { new DateTime(2024, 11, 30), CalendarQuarterDefinition.FebruaryToJanuary, 4 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.FebruaryToJanuary, 4 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarQuarterDefinition.FebruaryToJanuary, 4 };

        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 01, 31), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarQuarterDefinition.AprilToMarch, 4 };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 04, 30), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 05, 31), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 06, 30), CalendarQuarterDefinition.AprilToMarch, 1 };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 07, 31), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 08, 31), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 09, 30), CalendarQuarterDefinition.AprilToMarch, 2 };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.AprilToMarch, 3 };
        yield return new object[] { new DateTime(2024, 10, 31), CalendarQuarterDefinition.AprilToMarch, 3 };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.AprilToMarch, 3 };
        yield return new object[] { new DateTime(2024, 11, 30), CalendarQuarterDefinition.AprilToMarch, 3 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.AprilToMarch, 3 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarQuarterDefinition.AprilToMarch, 3 };

        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 01, 31), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarQuarterDefinition.JulyToJune, 3 };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 04, 30), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 05, 31), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 06, 30), CalendarQuarterDefinition.JulyToJune, 4 };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 07, 31), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 08, 31), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 09, 30), CalendarQuarterDefinition.JulyToJune, 1 };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.JulyToJune, 2 };
        yield return new object[] { new DateTime(2024, 10, 31), CalendarQuarterDefinition.JulyToJune, 2 };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.JulyToJune, 2 };
        yield return new object[] { new DateTime(2024, 11, 30), CalendarQuarterDefinition.JulyToJune, 2 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.JulyToJune, 2 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarQuarterDefinition.JulyToJune, 2 };

        yield return new object[] { new DateTime(2024, 01, 01), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 01, 31), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarQuarterDefinition.OctoberToSeptember, 2 };
        yield return new object[] { new DateTime(2024, 04, 01), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 04, 30), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 05, 01), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 05, 31), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 06, 01), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 06, 30), CalendarQuarterDefinition.OctoberToSeptember, 3 };
        yield return new object[] { new DateTime(2024, 07, 01), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 07, 31), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 08, 01), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 08, 31), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 09, 01), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 09, 30), CalendarQuarterDefinition.OctoberToSeptember, 4 };
        yield return new object[] { new DateTime(2024, 10, 01), CalendarQuarterDefinition.OctoberToSeptember, 1 };
        yield return new object[] { new DateTime(2024, 10, 31), CalendarQuarterDefinition.OctoberToSeptember, 1 };
        yield return new object[] { new DateTime(2024, 11, 01), CalendarQuarterDefinition.OctoberToSeptember, 1 };
        yield return new object[] { new DateTime(2024, 11, 30), CalendarQuarterDefinition.OctoberToSeptember, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarQuarterDefinition.OctoberToSeptember, 1 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarQuarterDefinition.OctoberToSeptember, 1 };

        yield return new object[] { new DateTime(2024, 01, 05), CalendarQuarterDefinition.April6ToApril5, 3 };
        yield return new object[] { new DateTime(2024, 01, 06), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 02, 05), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 02, 06), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 03, 05), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 03, 06), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 04, 05), CalendarQuarterDefinition.April6ToApril5, 4 };
        yield return new object[] { new DateTime(2024, 04, 06), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 05, 05), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 05, 06), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 06, 05), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 06, 06), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 07, 05), CalendarQuarterDefinition.April6ToApril5, 1 };
        yield return new object[] { new DateTime(2024, 07, 06), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 08, 05), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 08, 06), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 09, 05), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 09, 06), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 10, 05), CalendarQuarterDefinition.April6ToApril5, 2 };
        yield return new object[] { new DateTime(2024, 10, 06), CalendarQuarterDefinition.April6ToApril5, 3 };
        yield return new object[] { new DateTime(2024, 11, 05), CalendarQuarterDefinition.April6ToApril5, 3 };
        yield return new object[] { new DateTime(2024, 11, 06), CalendarQuarterDefinition.April6ToApril5, 3 };
        yield return new object[] { new DateTime(2024, 12, 05), CalendarQuarterDefinition.April6ToApril5, 3 };
        yield return new object[] { new DateTime(2024, 12, 06), CalendarQuarterDefinition.April6ToApril5, 3 };

        yield return new object[] { new DateTime(2024, 01, 24), CalendarQuarterDefinition.March25ToMarch24, 4 };
        yield return new object[] { new DateTime(2024, 01, 25), CalendarQuarterDefinition.March25ToMarch24, 4 };
        yield return new object[] { new DateTime(2024, 02, 24), CalendarQuarterDefinition.March25ToMarch24, 4 };
        yield return new object[] { new DateTime(2024, 02, 25), CalendarQuarterDefinition.March25ToMarch24, 4 };
        yield return new object[] { new DateTime(2024, 03, 24), CalendarQuarterDefinition.March25ToMarch24, 4 };
        yield return new object[] { new DateTime(2024, 03, 25), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 04, 24), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 04, 25), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 05, 24), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 05, 25), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 06, 24), CalendarQuarterDefinition.March25ToMarch24, 1 };
        yield return new object[] { new DateTime(2024, 06, 25), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 07, 24), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 07, 25), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 08, 24), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 08, 25), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 09, 24), CalendarQuarterDefinition.March25ToMarch24, 2 };
        yield return new object[] { new DateTime(2024, 09, 25), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 10, 24), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 10, 25), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 11, 24), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 11, 25), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 12, 24), CalendarQuarterDefinition.March25ToMarch24, 3 };
        yield return new object[] { new DateTime(2024, 12, 25), CalendarQuarterDefinition.March25ToMarch24, 4 };
    }

    public static IEnumerable<object[]> WeekendTestData()
    {
        yield return new object[] { new DateTime(2024, 04, 20), WorkingDaysOfWeek.MondayToFriday, null, true }; // Saturday
        yield return new object[] { new DateTime(2024, 04, 21), WorkingDaysOfWeek.MondayToFriday, null, true }; // Sunday
        yield return new object[] { new DateTime(2024, 04, 22), WorkingDaysOfWeek.MondayToFriday, null, false }; // Monday

        yield return new object[] { new DateTime(2024, 04, 19), WorkingDaysOfWeek.SundayToThursday, null, true }; // Friday
        yield return new object[] { new DateTime(2024, 04, 20), WorkingDaysOfWeek.SundayToThursday, null, true }; // Saturday
        yield return new object[] { new DateTime(2024, 04, 21), WorkingDaysOfWeek.SundayToThursday, null, false }; // Sunday

        yield return new object[] { new DateTime(2024, 04, 18), WorkingDaysOfWeek.SaturdayToWednesday, null, true }; // Thursday
        yield return new object[] { new DateTime(2024, 04, 19), WorkingDaysOfWeek.SaturdayToWednesday, null, true }; // Friday
        yield return new object[] { new DateTime(2024, 04, 20), WorkingDaysOfWeek.SaturdayToWednesday, null, false }; // Saturday

        yield return new object[] { new DateTime(2024, 04, 21), WorkingDaysOfWeek.MondayToSaturday, null, true }; // Sunday
        yield return new object[] { new DateTime(2024, 04, 22), WorkingDaysOfWeek.MondayToSaturday, null, false }; // Monday

        yield return new object[] { new DateTime(2024, 04, 19), WorkingDaysOfWeek.SaturdayToThursday, null, true }; // Friday
        yield return new object[] { new DateTime(2024, 04, 20), WorkingDaysOfWeek.SaturdayToThursday, null, false }; // Saturday

        // WorkingDaysOfWeek.AllDays means no day of the week is a weekend, so every day must evaluate to false.
        yield return new object[] { new DateTime(2024, 04, 21), WorkingDaysOfWeek.AllDays, null, false }; // Sunday
        yield return new object[] { new DateTime(2024, 04, 22), WorkingDaysOfWeek.AllDays, null, false }; // Monday
        yield return new object[] { new DateTime(2024, 04, 23), WorkingDaysOfWeek.AllDays, null, false }; // Tuesday
        yield return new object[] { new DateTime(2024, 04, 24), WorkingDaysOfWeek.AllDays, null, false }; // Wednesday
        yield return new object[] { new DateTime(2024, 04, 25), WorkingDaysOfWeek.AllDays, null, false }; // Thursday
        yield return new object[] { new DateTime(2024, 04, 26), WorkingDaysOfWeek.AllDays, null, false }; // Friday
        yield return new object[] { new DateTime(2024, 04, 27), WorkingDaysOfWeek.AllDays, null, false }; // Saturday

        yield return new object[] { new DateTime(2024, 04, 19), WorkingDaysOfWeek.Custom, typeof(FridayOnlyWeekendProvider), true };
        yield return new object[] { new DateTime(2024, 04, 20), WorkingDaysOfWeek.Custom, typeof(FridayOnlyWeekendProvider), false };
    }

    public static IEnumerable<object[]> WeekOfMonthCalendarWeekRuleTestData()
    {
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 6 };

        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 6 };

        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 6 };
    }

    public static IEnumerable<object[]> WeekOfMonthCultureTestData()
    {
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("en-US"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("en-US"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("en-US"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("en-US"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("en-US"), 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("en-US"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("en-US"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("en-US"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-US"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-US"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("en-US"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("en-US"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("en-US"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("en-US"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("en-US"), 5 };

        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("en-GB"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("en-GB"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("en-GB"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("en-GB"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("en-GB"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("en-GB"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("en-GB"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("en-GB"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("en-GB"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("en-GB"), 6 };

        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("de-DE"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("de-DE"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("de-DE"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("de-DE"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("de-DE"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("de-DE"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("de-DE"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("de-DE"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("de-DE"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("de-DE"), 6 };

        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("fr-FR"), 6 };

        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("ar-SA"), 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };

        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("he-IL"), 2 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("he-IL"), 3 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("he-IL"), 4 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("he-IL"), 6 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("he-IL"), 2 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("he-IL"), 3 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("he-IL"), 4 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("he-IL"), 2 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("he-IL"), 3 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("he-IL"), 4 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
    }

    public static IEnumerable<object[]> WeekOfYearCalendarWeekTestData()
    {
        // FirstDay + Sunday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 14 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 49 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 50 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 51 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 52 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 53 }; // FirstDay never crosses year boundary
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 53 };

        // FirstDay + Monday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 52 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstDay, DayOfWeek.Monday, 53 };

        // FirstFullWeek + Sunday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 4 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 7 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 8 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 8 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 9 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 10 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 11 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 12 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 52 }; // Dec 29-Jan 4 not fully in 2025 → last week of 2024
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday, 52 };

        // FirstFullWeek + Monday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 52 };
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday, 53 }; // Dec 30-31 partial, stays in last week of 2024

        // FirstFourDayWeek + Sunday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 14 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 49 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 50 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 51 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 52 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };  // week Dec 29-Jan 4 has 4 days in 2025 → week 1 of 2025
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday, 1 };  // same week → week 1 of 2025

        // FirstFourDayWeek + Monday
        yield return new object[] { new DateTime(2024, 02, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 52 }; // week Dec 23-29, all in 2024 → week 52
        yield return new object[] { new DateTime(2024, 12, 31), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1 };  // week Dec 30-Jan 5 has 5 days in 2025 → week 1 of 2025
    }

    public static IEnumerable<object[]> WeekOfYearCultureTestData()
    {
        // en-US: FirstDay + Sunday
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("en-US"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-US"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("en-US"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("en-US"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("en-US"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-US"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-US"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("en-US"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("en-US"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("en-US"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("en-US"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("en-US"), 14 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-US"), 49 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("en-US"), 50 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("en-US"), 51 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("en-US"), 52 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("en-US"), 53 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("en-US"), 53 }; // Tuesday, FirstDay+Sunday stays in week 53

        // en-GB: FirstFourDayWeek + Monday (ISO-like)
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("en-GB"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("en-GB"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("en-GB"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("en-GB"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("en-GB"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("en-GB"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("en-GB"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("en-GB"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("en-GB"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("en-GB"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("en-GB"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("en-GB"), 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("en-GB"), 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("en-GB"), 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("en-GB"), 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("en-GB"), 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("en-GB"), 52 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("en-GB"), 1 };  // Tuesday → ISO week 1 of 2025

        // de-DE: FirstFourDayWeek + Monday (ISO)
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("de-DE"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("de-DE"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("de-DE"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("de-DE"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("de-DE"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("de-DE"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("de-DE"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("de-DE"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("de-DE"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("de-DE"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("de-DE"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("de-DE"), 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("de-DE"), 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("de-DE"), 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("de-DE"), 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("de-DE"), 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("de-DE"), 52 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("de-DE"), 1 };  // Tuesday → ISO week 1 of 2025

        // fr-FR: FirstFourDayWeek + Monday (ISO)
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("fr-FR"), 13 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("fr-FR"), 48 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("fr-FR"), 49 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("fr-FR"), 50 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("fr-FR"), 51 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("fr-FR"), 52 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("fr-FR"), 1 };  // Tuesday → ISO week 1 of 2025

        // ar-SA: FirstDay + Saturday
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("ar-SA"), 14 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("ar-SA"), 49 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("ar-SA"), 50 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("ar-SA"), 51 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("ar-SA"), 52 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("ar-SA"), 53 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("ar-SA"), 53 }; // FirstDay+Saturday stays in week 53

        // he-IL: FirstDay + Sunday
        yield return new object[] { new DateTime(2024, 01, 01), CultureInfo.CreateSpecificCulture("he-IL"), 1 };
        yield return new object[] { new DateTime(2024, 02, 01), CultureInfo.CreateSpecificCulture("he-IL"), 5 };
        yield return new object[] { new DateTime(2024, 02, 08), CultureInfo.CreateSpecificCulture("he-IL"), 6 };
        yield return new object[] { new DateTime(2024, 02, 15), CultureInfo.CreateSpecificCulture("he-IL"), 7 };
        yield return new object[] { new DateTime(2024, 02, 22), CultureInfo.CreateSpecificCulture("he-IL"), 8 };
        yield return new object[] { new DateTime(2024, 02, 29), CultureInfo.CreateSpecificCulture("he-IL"), 9 };
        yield return new object[] { new DateTime(2024, 03, 01), CultureInfo.CreateSpecificCulture("he-IL"), 9 };
        yield return new object[] { new DateTime(2024, 03, 08), CultureInfo.CreateSpecificCulture("he-IL"), 10 };
        yield return new object[] { new DateTime(2024, 03, 15), CultureInfo.CreateSpecificCulture("he-IL"), 11 };
        yield return new object[] { new DateTime(2024, 03, 22), CultureInfo.CreateSpecificCulture("he-IL"), 12 };
        yield return new object[] { new DateTime(2024, 03, 29), CultureInfo.CreateSpecificCulture("he-IL"), 13 };
        yield return new object[] { new DateTime(2024, 03, 31), CultureInfo.CreateSpecificCulture("he-IL"), 14 };
        yield return new object[] { new DateTime(2024, 12, 01), CultureInfo.CreateSpecificCulture("he-IL"), 49 };
        yield return new object[] { new DateTime(2024, 12, 08), CultureInfo.CreateSpecificCulture("he-IL"), 50 };
        yield return new object[] { new DateTime(2024, 12, 15), CultureInfo.CreateSpecificCulture("he-IL"), 51 };
        yield return new object[] { new DateTime(2024, 12, 22), CultureInfo.CreateSpecificCulture("he-IL"), 52 };
        yield return new object[] { new DateTime(2024, 12, 29), CultureInfo.CreateSpecificCulture("he-IL"), 53 };
        yield return new object[] { new DateTime(2024, 12, 31), CultureInfo.CreateSpecificCulture("he-IL"), 53 }; // FirstDay+Sunday stays in week 53
    }

}
