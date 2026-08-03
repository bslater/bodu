#:project ../Bodu.Globalization.Calendar/src/Bodu.Globalization.Calendar.csproj

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="generate-hindu-observance-vectors.cs" company="Bodu Pty. Ltd.">
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
// series plus a seasonal window), and the published-panchanga rows in GlobalHinduCatalogueKnownAnswerTests anchor
// the modern end within the documented one-to-two-day panchanga tolerance.
//
// The script writes the CSV itself (rather than relying on stdout redirection) so stray MSBuild build diagnostics
// can never pollute the emitted table.
//
// Usage:
//     dotnet run tools/generate-hindu-observance-vectors.cs -- \
//         Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/HinduObservances-1990-2039.csv

const int FirstGregorianYear = 1990;
const int LastGregorianYear = 2039;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run tools/generate-hindu-observance-vectors.cs -- <output csv path>");
    return 2;
}

string[] observanceIds =
[
    "makar-sankranti", "pongal", "vasant-panchami", "saraswati-puja", "maha-shivaratri", "holi", "ram-navami",
    "raksha-bandhan", "janmashtami", "ganesh-chaturthi", "navaratri", "dussehra", "karva-chauth", "diwali",
];

string content = CommonNotableDateResources.Resolve("global-hindu")
    ?? throw new InvalidOperationException("The bundled global-hindu catalogue was not found.");
var service = new NotableDateService(NotableDateResourceLoader.Load(content, CommonNotableDateResources.Resolver));

List<string> lines =
[
    "# Hindu observance vectors for the bundled global-hindu catalogue.",
    $"# Range: Gregorian years {FirstGregorianYear}-{LastGregorianYear}, one occurrence per observance per year.",
    "# Source: ENGINE-PINNED - the rows freeze the in-repo HinduLunarCalculator / catalogue output as",
    "#   a regression baseline (no offline independent full-range panchanga exists; regional panchanga",
    "#   reckonings themselves differ by a day). Two independent checks brace the table: every lunar",
    "#   row's tithi proximity to the geocentric conjunction computed by the standalone Meeus ch. 49",
    "#   series in tools/verify-hindu-observance-vectors.py, each within its expected seasonal window,",
    "#   and the published-panchanga 2023-2029 rows in GlobalHinduCatalogueKnownAnswerTests as modern",
    "#   anchors; see NotableDateCatalogueVerification.md for the measured distributions.",
    "# Exactness relative to a specific published panchanga is NOT claimed beyond the documented",
    "#   one-to-two-day tolerance; the sweep guards against regression, not against panchanga variance.",
    "# Regenerate: dotnet run tools/generate-hindu-observance-vectors.cs -- <this file>",
    "# Verify:     python3 tools/verify-hindu-observance-vectors.py <this file>",
    "# Columns: gregorianYear,observanceId,date",
];

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

        lines.Add($"{year},{id},{dates[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }
}

File.WriteAllLines(args[0], lines);
Console.WriteLine($"Wrote {lines.Count - 14} vector rows to {args[0]}.");
return 0;
