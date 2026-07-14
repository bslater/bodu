// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheFileConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Converts between a territory's in-memory entry list and the persisted <see cref="NotableDateCacheFile" /> shape, so
/// the TOML and JSON file caches share one serialization mapping and differ only in the serializer they call. It also
/// exposes the per-entry occurrence blob mapping the SQLite backend stores, and the shared
/// <see cref="JsonSerializerOptions" /> every JSON surface uses.
/// </summary>
/// <remarks>
/// Conversion back to entries is deliberately lenient: an occurrence row whose category cannot be parsed, or a year row
/// with no version, is skipped rather than throwing, so a partially corrupt file degrades to the entries it can still
/// read instead of failing the whole read.
/// </remarks>
internal static class NotableDateCacheFileConverter
{
    /// <summary>The serializer options shared by every JSON surface: the occurrence blob, the distributed per-territory blob, and the JSON file cache's read path.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes an entry's occurrences to the JSON blob the SQLite backend stores per row.
    /// </summary>
    /// <param name="occurrences">The occurrences to serialize.</param>
    /// <returns>The JSON blob.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="occurrences" /> is <see langword="null" />.
    /// </exception>
    public static string SerializeOccurrences(IReadOnlyList<NotableDate> occurrences)
    {
        ThrowHelper.ThrowIfNull(occurrences);

        List<NotableDateCacheOccurrenceRow> rows = new(occurrences.Count);
        foreach (NotableDate occurrence in occurrences)
            rows.Add(ToRow(0, string.Empty, occurrence));

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    /// <summary>
    /// Deserializes an entry's occurrences from the JSON blob the SQLite backend stores per row, skipping rows that
    /// cannot be read.
    /// </summary>
    /// <param name="json">The JSON blob.</param>
    /// <returns>The reconstructed occurrences, in stored order.</returns>
    public static IReadOnlyList<NotableDate> DeserializeOccurrences(string json)
    {
        List<NotableDateCacheOccurrenceRow>? rows = JsonSerializer.Deserialize<List<NotableDateCacheOccurrenceRow>>(json, JsonOptions);
        if (rows is null)
            return Array.Empty<NotableDate>();

        List<NotableDate> occurrences = new(rows.Count);
        foreach (NotableDateCacheOccurrenceRow row in rows)
        {
            if (TryToOccurrence(row, out NotableDate? occurrence))
                occurrences.Add(occurrence);
        }

        return occurrences;
    }

    /// <summary>
    /// Projects a territory's entries into the persisted file shape.
    /// </summary>
    /// <param name="territory">The normalized territory the entries belong to.</param>
    /// <param name="entries">The entries to project.</param>
    /// <returns>The file representation of <paramref name="entries" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="territory" /> or <paramref name="entries" /> is <see langword="null" />.
    /// </exception>
    public static NotableDateCacheFile ToFile(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(entries);

        var file = new NotableDateCacheFile { Territory = territory };

        foreach (NotableDateCacheEntry entry in entries)
        {
            file.Entries.Add(new NotableDateCacheYearRow
            {
                Year = entry.Year,
                Version = entry.ResourceVersion,
                ComputedAtUtc = entry.ComputedAtUtc,
            });

            foreach (NotableDate occurrence in entry.Occurrences)
                file.Occurrences.Add(ToRow(entry.Year, entry.ResourceVersion, occurrence));
        }

        return file;
    }

    /// <summary>
    /// Reconstructs a territory's entries from the persisted file shape, skipping rows that cannot be read.
    /// </summary>
    /// <param name="file">The file to reconstruct from.</param>
    /// <returns>The reconstructed entries, or an empty list when the file names no territory.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDateCacheEntry> ToEntries(NotableDateCacheFile file)
    {
        ThrowHelper.ThrowIfNull(file);

        if (string.IsNullOrEmpty(file.Territory) || file.Entries.Count == 0)
            return Array.Empty<NotableDateCacheEntry>();

        // Group the flat occurrence rows by their owning (year, version) first, so assembly is one linear pass rather
        // than rescanning every occurrence row per year entry. Stored order within each year is preserved.
        Dictionary<(int Year, string Version), List<NotableDate>> occurrencesByYear = new(file.Entries.Count);
        foreach (NotableDateCacheOccurrenceRow row in file.Occurrences)
        {
            if (row.Version is null || !TryToOccurrence(row, out NotableDate? occurrence))
                continue;

            (int Year, string Version) key = (row.Year, row.Version);
            if (!occurrencesByYear.TryGetValue(key, out List<NotableDate>? list))
            {
                list = new List<NotableDate>();
                occurrencesByYear[key] = list;
            }

            list.Add(occurrence);
        }

        List<NotableDateCacheEntry> entries = new(file.Entries.Count);
        foreach (NotableDateCacheYearRow row in file.Entries)
        {
            if (row.Version is null)
                continue;

            IReadOnlyList<NotableDate> occurrences = occurrencesByYear.TryGetValue((row.Year, row.Version), out List<NotableDate>? list)
                ? list
                : Array.Empty<NotableDate>();

            entries.Add(new NotableDateCacheEntry(file.Territory, row.Year, row.Version, occurrences, row.ComputedAtUtc));
        }

        return entries;
    }

    /// <summary>
    /// Flattens a single occurrence into its persisted row form.
    /// </summary>
    /// <param name="year">The civil year that associates the occurrence with its cached year entry.</param>
    /// <param name="version">The resource version that associates the occurrence with its cached year entry.</param>
    /// <param name="occurrence">The occurrence to flatten.</param>
    /// <returns>The flattened occurrence row.</returns>
    private static NotableDateCacheOccurrenceRow ToRow(int year, string version, NotableDate occurrence) =>
        new()
        {
            Year = year,
            Version = version,
            Date = occurrence.Date,
            ActualDate = occurrence.ActualDate,
            IsObserved = occurrence.IsObserved,
            ResourceId = occurrence.Identity.ResourceId,
            NotableDateId = occurrence.Identity.NotableDateId,
            RuleId = occurrence.Identity.RuleId,
            DisplayName = occurrence.DisplayName,
            TerritoryCode = occurrence.TerritoryCode,
            Category = occurrence.Category.ToString(),
            Priority = occurrence.Priority,
            DurationDays = occurrence.DurationDays,
            IsNonWorkingDay = occurrence.IsNonWorkingDay,
            Tags = new List<string>(occurrence.Tags),
            AdjustmentPolicyId = occurrence.AdjustmentPolicyId,
            AdjustmentReason = occurrence.AdjustmentReason,
        };

    /// <summary>
    /// Attempts to reconstruct a single occurrence from its persisted row form.
    /// </summary>
    /// <param name="row">The row to reconstruct from.</param>
    /// <param name="occurrence">
    /// When this method returns <see langword="true" />, the reconstructed occurrence; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the row reconstructs to a valid occurrence; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryToOccurrence(NotableDateCacheOccurrenceRow row, out NotableDate? occurrence)
    {
        occurrence = null;

        if (!Enum.TryParse(row.Category, out NotableDateCategory category))
            return false;

        var identity = new NotableDateRuleIdentity(
            row.ResourceId ?? string.Empty,
            row.NotableDateId ?? string.Empty,
            row.RuleId ?? string.Empty);

        occurrence = new NotableDate(
            row.Date,
            row.ActualDate,
            row.IsObserved,
            identity,
            row.DisplayName ?? string.Empty,
            row.TerritoryCode ?? string.Empty,
            category,
            row.Priority,
            row.DurationDays,
            row.IsNonWorkingDay,
            row.Tags ?? new List<string>(),
            row.AdjustmentPolicyId,
            row.AdjustmentReason);

        return true;
    }
}
