// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BahaiHolyDayVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads the embedded Baha'i holy-day vector table — the fifty years 172-221 B.E. (2015-2064) fixed by the Universal
/// House of Justice in its 50-year Badi table — as KAT rows for <c>[DynamicData]</c> binding.
/// </summary>
/// <remarks>
/// <para>
/// The table carries the nine holy days the <c>global-bahai</c> catalogue models; the full eleven-id transcription
/// including the Twin Birthdays lives at <c>corpus/bahai/uhj-holy-days-172-221-be.csv</c>, and the file's provenance
/// header records the source chain, the pre-commit verification, and the measured engine relationship.
/// </para>
/// </remarks>
internal static class BahaiHolyDayVectors
{
    /// <summary>The logical name of the embedded vector file.</summary>
    private const string ResourceName = "Bodu.Globalization.Calendar.Fixtures.Vectors.BahaiHolyDays-172-221BE.csv";

    /// <summary>
    /// Gets the vector rows, one per (Gregorian year, holy day) pair, each carrying the official date.
    /// </summary>
    /// <value>The KAT rows for <c>[DynamicData]</c> binding.</value>
    public static IEnumerable<object[]> Rows
    {
        get
        {
            using Stream stream = typeof(BahaiHolyDayVectors).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded vector file '{ResourceName}' not found.");
            using StreamReader reader = new(stream);

            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                string[] fields = line.Split(',');
                var year = int.Parse(fields[0], CultureInfo.InvariantCulture);
                string observanceId = fields[1];
                var official = DateOnly.Parse(fields[2], CultureInfo.InvariantCulture);

                yield return
                [
                    new ValidKat<(int Year, string ObservanceId), DateOnly>(
                        $"{observanceId} {year}",
                        (year, observanceId),
                        official),
                ];
            }
        }
    }
}
