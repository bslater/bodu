// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar;

// Generates the Hindu observance regression vectors for the global-hindu catalogue.
//
// Unlike the Hebrew / Persian / Islamic vector tables, these rows are engine-pinned: the in-repo
// HinduLunarCalculator is itself the model (there is no offline independent full-range panchanga), so the table
// freezes the engine's current output as a regression baseline. Two independent braces guard it: every lunar row is
// structurally cross-checked by tools/verify-hindu-observance-vectors.py (tithi proximity to the Meeus conjunction
// series plus a seasonal window), and the published-panchanga rows for 2023-2027 in
// GlobalHinduCatalogueKnownAnswerTests anchor the modern end within the documented one-to-two-day panchanga
// tolerance.
//
// Usage:
//     dotnet run --project tools/HinduObservanceVectorGenerator > \
//         Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/HinduObservances-1990-2039.csv

const int FirstGregorianYear = 1990;
const int LastGregorianYear = 2039;

string[] observanceIds =
[
    "makar-sankranti", "pongal", "vasant-panchami", "saraswati-puja", "maha-shivaratri", "holi", "ram-navami",
    "raksha-bandhan", "janmashtami", "ganesh-chaturthi", "navaratri", "dussehra", "karva-chauth", "diwali",
];

string content = CommonNotableDateResources.Resolve("global-hindu")
    ?? throw new InvalidOperationException("The bundled global-hindu catalogue was not found.");
var service = new NotableDateService(NotableDateResourceLoader.Load(content, CommonNotableDateResources.Resolver));

Console.WriteLine("# Hindu observance vectors for the bundled global-hindu catalogue.");
Console.WriteLine($"# Range: Gregorian years {FirstGregorianYear}-{LastGregorianYear}, one occurrence per observance per year.");
Console.WriteLine("# Source: ENGINE-PINNED - the rows freeze the in-repo HinduLunarCalculator / catalogue output as");
Console.WriteLine("#   a regression baseline (no offline independent full-range panchanga exists; regional panchanga");
Console.WriteLine("#   reckonings themselves differ by a day). Two independent checks brace the table: every lunar");
Console.WriteLine("#   row's tithi proximity to the geocentric conjunction computed by the standalone Meeus ch. 49");
Console.WriteLine("#   series in tools/verify-hindu-observance-vectors.py, each within its expected seasonal window,");
Console.WriteLine("#   and the published-panchanga 2023-2027 rows in GlobalHinduCatalogueKnownAnswerTests as modern");
Console.WriteLine("#   anchors; see NotableDateCatalogueVerification.md for the measured distributions.");
Console.WriteLine("# Exactness relative to a specific published panchanga is NOT claimed beyond the documented");
Console.WriteLine("#   one-to-two-day tolerance; the sweep guards against regression, not against panchanga variance.");
Console.WriteLine("# Regenerate: dotnet run --project tools/HinduObservanceVectorGenerator > <this file>");
Console.WriteLine("# Verify:     python3 tools/verify-hindu-observance-vectors.py <this file>");
Console.WriteLine("# Columns: gregorianYear,observanceId,date");

for (int year = FirstGregorianYear; year <= LastGregorianYear; year++)
{
    IReadOnlyList<NotableDate> resolved = service.Resolve(
        new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)),
        "XX");

    foreach (string id in observanceIds)
    {
        var dates = resolved
            .Where(r => r.NotableDateId == id && r.Date.Year == year)
            .Select(r => r.Date)
            .OrderBy(d => d)
            .ToList();

        if (dates.Count != 1)
            throw new InvalidOperationException($"{id} {year}: expected one occurrence, resolved {dates.Count}.");

        Console.WriteLine($"{year},{id},{dates[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }
}
