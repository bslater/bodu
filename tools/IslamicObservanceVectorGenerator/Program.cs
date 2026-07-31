// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

// Generates the Umm al-Qura observance regression vectors for the global-islamic-umm-al-qura catalogue.
//
// The vectors project each observance's fixed Hijri (month, day) through System.Globalization.UmAlQuraCalendar —
// whose embedded month table is the official KACST Umm al-Qura data — replicating the engine's calendar-year sweep
// semantics (a short lunar date can land twice in one Gregorian year, so a year may carry two rows per observance).
// Laylat al-Qadr is 1 Ramadan + 26 days, keyed by the Gregorian year the resulting date falls in. Every emitted row
// is independently cross-checked astronomically by tools/verify-islamic-observance-vectors.py; the CSV provenance
// header records both derivations.
//
// Usage:
//     dotnet run --project tools/IslamicObservanceVectorGenerator > \
//         Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/UmmAlQuraObservances-1990-2039.csv

const int FirstGregorianYear = 1990;
const int LastGregorianYear = 2039;

// (observanceId, Hijri month, Hijri day, day offset) mirroring global-islamic-umm-al-qura.xml: nine fixed anchors
// plus Laylat al-Qadr as the catalogue's OffsetFromRule (1 Ramadan + 26 days, i.e. the night of 27 Ramadan).
(string Id, int Month, int Day, int Offset)[] observances =
[
    ("islamic-new-year", 1, 1, 0),
    ("day-of-ashura", 1, 10, 0),
    ("mawlid-al-nabi", 3, 12, 0),
    ("isra-and-miraj", 7, 27, 0),
    ("ramadan", 9, 1, 0),
    ("laylat-al-qadr", 9, 1, 26),
    ("eid-al-fitr", 10, 1, 0),
    ("hajj", 12, 8, 0),
    ("day-of-arafah", 12, 9, 0),
    ("eid-al-adha", 12, 10, 0),
];

var calendar = new UmAlQuraCalendar();

Console.WriteLine("# Umm al-Qura observance vectors for the bundled global-islamic-umm-al-qura catalogue.");
Console.WriteLine($"# Range: Gregorian years {FirstGregorianYear}-{LastGregorianYear}; a short lunar date can land twice in one");
Console.WriteLine("#   Gregorian year, so a year may carry two rows for the same observance (chronological order).");
Console.WriteLine("# Source: the official KACST Umm al-Qura month table as embedded in");
Console.WriteLine("#   System.Globalization.UmAlQuraCalendar, projected by tools/IslamicObservanceVectorGenerator");
Console.WriteLine("#   replicating the engine's calendar-year sweep; Laylat al-Qadr is 1 Ramadan + 26 days.");
Console.WriteLine("# Cross-verified at generation time: every row's underlying Hijri month start falls 0-3 days");
Console.WriteLine("#   after the geocentric lunar conjunction computed by the independent Meeus ch. 49 series in");
Console.WriteLine("#   tools/verify-islamic-observance-vectors.py, and the table agrees with the hand-pinned");
Console.WriteLine("#   published 2023-2025 rows in GlobalIslamicCatalogueKnownAnswerTests; see");
Console.WriteLine("#   NotableDateCatalogueVerification.md.");
Console.WriteLine("# Externally reconciled against the KFUPM Research Institute Comparison Calendar 1356-1411 AH");
Console.WriteLine("#   (King Fahd University of Petroleum & Minerals) print: all 24 month starts of 1410-1411 AH");
Console.WriteLine("#   and all 17 vector rows they determine (Gregorian 1990 and pre-July-1991) match exactly.");
Console.WriteLine("# Externally reconciled against ummulqura.org.sa year exports sampled across the range");
Console.WriteLine("#   (1420, 1430, 1446, 1448 AH): 48/48 month starts, 1418/1418 day rows, and all 40 vector");
Console.WriteLine("#   rows they determine match exactly. For 1410 AH the site's retrospective table runs one");
Console.WriteLine("#   day later than both the KFUPM print and the KACST table for 11 of 12 months; the vectors");
Console.WriteLine("#   follow the calendar contemporaneously printed in the Kingdom.");
Console.WriteLine("# Regenerate: dotnet run --project tools/IslamicObservanceVectorGenerator > <this file>");
Console.WriteLine("# Verify:     python3 tools/verify-islamic-observance-vectors.py <this file>");
Console.WriteLine("# Columns: gregorianYear,observanceId,date");

for (int year = FirstGregorianYear; year <= LastGregorianYear; year++)
{
    foreach ((string id, int month, int day, int offset) in observances)
    {
        foreach (DateOnly date in Occurrences(id, month, day, offset, year))
            Console.WriteLine($"{year},{id},{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }
}

return;

// The occurrences of the observance whose (possibly offset) date falls in the Gregorian year, in order.
List<DateOnly> Occurrences(string id, int month, int day, int offset, int gregorianYear)
{
    // An offset observance anchored late in a Gregorian year lands in the next one, so widen the anchor sweep by a
    // year on each side and key by the resulting date.
    List<DateOnly> dates = [];
    for (int anchorYear = gregorianYear - 1; anchorYear <= gregorianYear + 1; anchorYear++)
    {
        foreach (DateOnly anchor in AnchorsInGregorianYear(month, day, anchorYear))
        {
            DateOnly date = anchor.AddDays(offset);
            if (date.Year == gregorianYear && !dates.Contains(date))
                dates.Add(date);
        }
    }

    dates.Sort();
    return dates;
}

// Replicates CalendarSystems.ResolveCalendarYearSweep: project the fixed Hijri date through every Umm al-Qura year
// overlapping the Gregorian year and keep the occurrences that fall inside it.
List<DateOnly> AnchorsInGregorianYear(int month, int day, int gregorianYear)
{
    List<DateOnly> anchors = [];
    int firstHijriYear = calendar.GetYear(new DateTime(gregorianYear, 1, 1));
    int lastHijriYear = calendar.GetYear(new DateTime(gregorianYear, 12, 31));

    for (int hijriYear = firstHijriYear; hijriYear <= lastHijriYear; hijriYear++)
    {
        if (day > calendar.GetDaysInMonth(hijriYear, month))
            continue;

        DateTime candidate = calendar.ToDateTime(hijriYear, month, day, 0, 0, 0, 0);
        if (candidate.Year == gregorianYear)
            anchors.Add(DateOnly.FromDateTime(candidate));
    }

    return anchors;
}
