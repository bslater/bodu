// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UsFederalHolidayVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads the embedded United States federal holiday vector table — twenty schedule years of observed dates as published
/// by the U.S. Office of Personnel Management — as KAT rows for <c>[DynamicData]</c> binding.
/// </summary>
/// <remarks>
/// <para>
/// The observed dates follow the statutory substitution rule (a Saturday holiday is observed the preceding Friday, a
/// Sunday holiday the following Monday), so an observed New Year's Day can fall on 31 December of the previous
/// Gregorian year; the file's provenance header records the details and the derivation of the observed flag.
/// </para>
/// </remarks>
internal static class UsFederalHolidayVectors
{
    /// <summary>The logical name of the embedded vector file.</summary>
    private const string ResourceName = "Bodu.Globalization.Calendar.Americas.Fixtures.Vectors.UsFederalHolidays-2011-2030.csv";

    /// <summary>
    /// Gets the vector rows, one per (schedule year, holiday) pair, each carrying the observed date and flag.
    /// </summary>
    /// <value>The KAT rows for <c>[DynamicData]</c> binding.</value>
    public static IEnumerable<object[]> Rows
    {
        get
        {
            using Stream stream = typeof(UsFederalHolidayVectors).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded vector file '{ResourceName}' not found.");
            using StreamReader reader = new(stream);

            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                string[] fields = line.Split(',');
                var scheduleYear = int.Parse(fields[0], CultureInfo.InvariantCulture);
                string observanceId = fields[1];
                var observed = DateOnly.Parse(fields[2], CultureInfo.InvariantCulture);
                bool isObserved = bool.Parse(fields[3]);

                yield return
                [
                    new ValidKat<(int ScheduleYear, string ObservanceId), (DateOnly Observed, bool IsObserved)>(
                        $"{observanceId} {scheduleYear}",
                        (scheduleYear, observanceId),
                        (observed, isObserved)),
                ];
            }
        }
    }
}
