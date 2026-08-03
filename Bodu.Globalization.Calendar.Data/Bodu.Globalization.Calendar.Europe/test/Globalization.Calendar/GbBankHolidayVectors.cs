// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GbBankHolidayVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads the embedded United Kingdom bank-holiday vector table — the official GOV.UK dataset for 2019-2028 — as KAT
/// rows for <c>[DynamicData]</c> binding.
/// </summary>
/// <remarks>
/// <para>
/// The table carries the regular rule-derived bank holidays per home nation (GB-ENG, GB-SCT, GB-NIR); the raw feed is
/// archived verbatim at <c>corpus/uk/bank-holidays-2019-2028.json</c>, and the vector file's provenance header records
/// the source chain, the sixteen excluded royal/proclamation one-offs, and the generation-time re-verification.
/// </para>
/// </remarks>
internal static class GbBankHolidayVectors
{
    /// <summary>The logical name of the embedded vector file.</summary>
    private const string ResourceName = "Bodu.Globalization.Calendar.Europe.Fixtures.Vectors.GbBankHolidays-2019-2028.csv";

    /// <summary>
    /// Gets the vector rows, one per (territory, year, bank holiday) triple, each carrying the official emitted date
    /// and whether the feed marks it as a substitute day.
    /// </summary>
    /// <value>The KAT rows for <c>[DynamicData]</c> binding.</value>
    public static IEnumerable<object[]> Rows
    {
        get
        {
            using Stream stream = typeof(GbBankHolidayVectors).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded vector file '{ResourceName}' not found.");
            using StreamReader reader = new(stream);

            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                string[] fields = line.Split(',');
                string territory = fields[0];
                var year = int.Parse(fields[1], CultureInfo.InvariantCulture);
                string observanceId = fields[2];
                var official = DateOnly.Parse(fields[3], CultureInfo.InvariantCulture);
                var isObserved = bool.Parse(fields[4]);

                yield return
                [
                    new ValidKat<(string Territory, int Year, string ObservanceId), (DateOnly Date, bool IsObserved)>(
                        $"{territory} {observanceId} {year}",
                        (territory, year, observanceId),
                        (official, isObserved)),
                ];
            }
        }
    }
}
