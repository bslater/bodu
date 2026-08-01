// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaudiAnnouncedObservanceVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads the embedded Saudi announced (gazetted) observance table — the dates the High Judiciary Council of Saudi
/// Arabia actually announced for the sighting-sensitive month starts of Hijri 1422–1448 — as KAT rows for
/// <c>[DynamicData]</c> binding.
/// </summary>
/// <remarks>
/// <para>
/// The announcements can precede or follow the computed Umm al-Qura calendar by one day (never more across the
/// range), so consumers assert a one-day distance bound rather than equality; the file's provenance header records
/// the source and the divergence profile.
/// </para>
/// </remarks>
internal static class SaudiAnnouncedObservanceVectors
{
    /// <summary>The logical name of the embedded vector file.</summary>
    private const string ResourceName = "Bodu.Globalization.Calendar.Fixtures.Vectors.SaudiAnnouncedObservances-1422-1448.csv";

    /// <summary>
    /// Gets the vector rows, one per announced observance occurrence, each carrying the gazetted date.
    /// </summary>
    /// <value>The KAT rows for <c>[DynamicData]</c> binding.</value>
    public static IEnumerable<object[]> Rows
    {
        get
        {
            using Stream stream = typeof(SaudiAnnouncedObservanceVectors).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded vector file '{ResourceName}' not found.");
            using StreamReader reader = new(stream);

            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                string[] fields = line.Split(',');
                var year = int.Parse(fields[0], CultureInfo.InvariantCulture);
                string observanceId = fields[1];
                var announced = DateOnly.Parse(fields[2], CultureInfo.InvariantCulture);

                yield return
                [
                    new ValidKat<(int Year, string ObservanceId), DateOnly>(
                        $"{observanceId} {announced:yyyy-MM-dd} (announced)",
                        (year, observanceId),
                        announced),
                ];
            }
        }
    }
}
