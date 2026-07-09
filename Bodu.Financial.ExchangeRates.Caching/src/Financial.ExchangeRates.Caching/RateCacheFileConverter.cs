// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheFileConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Maps between the internal <see cref="CachePairState" /> and the <see cref="RateCacheFile" /> serialization shape, so
/// every file-backed format — TOML, JSON, and any future peer — shares one identical projection of rows, coverage, and
/// the self-describing provider and currency pair.
/// </summary>
internal static class RateCacheFileConverter
{
    /// <summary>
    /// Projects a pair's state into the serialization shape, stamping the bound provider and the pair's currency codes
    /// so the file is self-describing.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="pair">The currency pair the state belongs to.</param>
    /// <param name="state">The per-pair state to project.</param>
    /// <returns>The serialization shape carrying the header, rows, and coverage windows.</returns>
    public static RateCacheFile ToFile(string provider, CurrencyPair pair, CachePairState state)
    {
        RateCacheFile file = new()
        {
            Provider = provider,
            From = pair.From.ToString(),
            To = pair.To.ToString(),
            Entries = new List<RateCacheEntry>(state.Entries.Count),
            Coverage = new List<RateCacheCoverageEntry>(state.Coverage.Count),
        };

        foreach (CachedRate entry in state.Entries)
            file.Entries.Add(new RateCacheEntry { Date = entry.Date, Rate = entry.Rate, CachedAtUtc = entry.CachedAtUtc, ObservedAtUtc = entry.ObservedAtUtc });

        foreach (CoverageWindow window in state.Coverage)
            file.Coverage.Add(new RateCacheCoverageEntry { Start = window.Start, End = window.End, FetchedAtUtc = window.FetchedAtUtc });

        return file;
    }

    /// <summary>
    /// Projects a deserialized file back into per-pair state, tolerating the absent arrays and keys of a legacy file by
    /// returning <see cref="CachePairState.Empty" /> when it carries neither rows nor coverage.
    /// </summary>
    /// <param name="file">
    /// The deserialized file, or <see langword="null" /> when the content deserialized to nothing.
    /// </param>
    /// <returns>
    /// The parsed state, or <see cref="CachePairState.Empty" /> when the file carries no rows or coverage.
    /// </returns>
    public static CachePairState ToState(RateCacheFile? file)
    {
        if (file is null)
            return CachePairState.Empty;

        // A pre-coverage file simply has no coverage array, and a pre-ObservedAtUtc entry has no such key, each
        // deserializing to an empty list or null with no error, so older caches keep their rows.
        List<CachedRate> entries = new(file.Entries?.Count ?? 0);
        if (file.Entries is { Count: > 0 })
        {
            foreach (RateCacheEntry entry in file.Entries)
                entries.Add(new CachedRate(entry.Date, entry.Rate, entry.CachedAtUtc, entry.ObservedAtUtc));
        }

        List<CoverageWindow> coverage = new(file.Coverage?.Count ?? 0);
        if (file.Coverage is { Count: > 0 })
        {
            foreach (RateCacheCoverageEntry window in file.Coverage)
                coverage.Add(new CoverageWindow(window.Start, window.End, window.FetchedAtUtc));
        }

        if (entries.Count == 0 && coverage.Count == 0)
            return CachePairState.Empty;

        return new CachePairState(entries, coverage);
    }
}
