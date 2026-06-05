// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangeResolutionCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Maintains the unified entry set produced by the chronological range-resolution pipeline for a single resolution
/// request.
/// </summary>
/// <remarks>
/// <para>
/// The cache stores at most one entry per <see cref="NotableDateCacheKey" />. Each entry carries a state flag
/// distinguishing entries that are eligible for emission from those that are present only as adjustment context.
/// Callers can:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Look up an anchor date by rule name and year via <see cref="ResolveAnchor" /> — used by offset rules to read
/// previously-computed anchors without recalculating them.
/// </description>
/// </item>
/// <item>
/// <description>
/// Enumerate emission candidates via <see cref="EmissableEntries" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// Probe non-working day context via <see cref="IsNonWorkingDay" /> — used by the adjuster's blocker evaluation.
/// </description>
/// </item>
/// </list>
/// </remarks>
internal sealed class NotableDateRangeResolutionCache
{
    /// <summary>
    /// The backing dictionary keyed by <see cref="NotableDateCacheKey" />.
    /// </summary>
    private readonly Dictionary<NotableDateCacheKey, NotableDateCacheEntry> _entries = [];

    /// <summary>
    /// Gets the live entry sequence in arbitrary order.
    /// </summary>
    /// <returns>
    /// An enumeration of every <see cref="NotableDateCacheEntry" /> currently in the cache. Iteration order is not
    /// defined.
    /// </returns>
    public IEnumerable<NotableDateCacheEntry> Entries => _entries.Values;

    /// <summary>
    /// Gets the number of entries currently in the cache.
    /// </summary>
    /// <returns>A non-negative entry count.</returns>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds an entry to the cache, replacing any existing entry with the same key.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry" /> is <see langword="null" />.</exception>
    public void Add(NotableDateCacheEntry entry)
    {
        ThrowHelper.ThrowIfNull(entry);

        // Key on the rule's variant identity (Name + RuleName) combined with the materialized territory and calendar so
        // that distinct variants of the same canonical name do not collapse into a single cache slot.
        NotableDateRuleIdentity identity = new(
            entry.Rule.Name,
            entry.Rule.RuleName,
            entry.BaseNotable.TerritoryCode,
            entry.BaseNotable.CalendarType);

        _entries[new NotableDateCacheKey(identity, entry.AnchorYear)] = entry;
    }

    /// <summary>
    /// Determines whether an entry exists for the supplied key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns><see langword="true" /> when an entry exists; otherwise, <see langword="false" />.</returns>
    public bool Contains(NotableDateCacheKey key) =>
        _entries.ContainsKey(key);

    /// <summary>
    /// Attempts to retrieve an entry by its cache key.
    /// </summary>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="entry">The matching entry when the method returns <see langword="true" />.</param>
    /// <returns>
    /// <see langword="true" /> when an entry with the supplied key exists; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGet(NotableDateCacheKey key, out NotableDateCacheEntry entry)
    {
        if (_entries.TryGetValue(key, out NotableDateCacheEntry? found))
        {
            entry = found;
            return true;
        }

        entry = null!;
        return false;
    }

    /// <summary>
    /// Resolves the cached anchor date for the rule with the supplied composite identity and year, regardless of the
    /// entry's state.
    /// </summary>
    /// <param name="identity">
    /// The root anchor's identity. The cached entry is matched on canonical name, rule-level variant, and calendar type
    /// so that distinct same-name variants never resolve one another; the materialized territory is intentionally not
    /// part of the match, mirroring the territory-agnostic anchor reuse the pipeline relies on.
    /// </param>
    /// <param name="year">The civil year.</param>
    /// <returns>The cached anchor date, or <see langword="null" /> when no matching entry exists.</returns>
    public DateTime? ResolveAnchor(in NotableDateRuleIdentity identity, int year)
    {
        foreach (NotableDateCacheEntry entry in _entries.Values)
        {
            if (entry.AnchorYear != year) continue;
            if (!string.Equals(entry.Rule.Name, identity.Name, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(entry.Rule.RuleName ?? string.Empty, identity.RuleName ?? string.Empty, StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.Rule.CalendarType != identity.CalendarType) continue;

            return entry.BaseNotable.Date;
        }

        return null;
    }

    /// <summary>
    /// Resolves the cached anchor date for a rule by canonical name and year, returning the first matching entry. Kept
    /// for name-only convenience; execution paths prefer <see cref="ResolveAnchor(in NotableDateRuleIdentity, int)" /> so
    /// same-name variants do not collapse.
    /// </summary>
    /// <param name="ruleName">The canonical rule name.</param>
    /// <param name="year">The civil year.</param>
    /// <returns>The cached anchor date, or <see langword="null" /> when no matching entry exists.</returns>
    public DateTime? ResolveAnchor(string ruleName, int year)
    {
        foreach (NotableDateCacheEntry entry in _entries.Values)
        {
            if (entry.AnchorYear != year) continue;
            if (!string.Equals(entry.Rule.Name, ruleName, StringComparison.OrdinalIgnoreCase)) continue;

            return entry.BaseNotable.Date;
        }

        return null;
    }

    /// <summary>
    /// Returns the entries currently flagged as eligible for emission in the resolution output.
    /// </summary>
    /// <returns>The emission candidates.</returns>
    public IEnumerable<NotableDateCacheEntry> EmissableEntries()
    {
        foreach (NotableDateCacheEntry entry in _entries.Values)
        {
            if (entry.IsEmissable)
                yield return entry;
        }
    }

    /// <summary>
    /// Determines whether any cached entry flags the supplied date as a non-working day for the requested context.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="territoryCode">The territory context, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar context, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when at least one entry covers the date and is flagged as non-working; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool IsNonWorkingDay(DateTime date, string? territoryCode, Type? calendarType)
    {
        DateTime day = date.Date;

        foreach (NotableDateCacheEntry entry in _entries.Values)
        {
            if (EntryCoversDay(entry.BaseNotable, day, territoryCode, calendarType))
                return true;

            if (entry.Adjusted is { } adjusted && EntryCoversDay(adjusted, day, territoryCode, calendarType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the observed date for the named rule in the supplied year and context, preferring the adjusted date
    /// when present.
    /// </summary>
    /// <param name="ruleName">The canonical rule name.</param>
    /// <param name="ruleVariant">
    /// The optional rule-level variant to match against <see cref="NotableDateRule.RuleName" />, or
    /// <see langword="null" /> to resolve by name with context only.
    /// </param>
    /// <param name="year">The civil year.</param>
    /// <param name="territoryCode">The territory context, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar context, or <see langword="null" />.</param>
    /// <returns>The observed date, or <see langword="null" /> when no matching entry exists.</returns>
    public DateTime? ResolveObservedByName(string ruleName, string? ruleVariant, int year, string? territoryCode, Type? calendarType)
    {
        foreach (NotableDateCacheEntry entry in _entries.Values)
        {
            if (entry.AnchorYear != year) continue;
            if (!string.Equals(entry.Rule.Name, ruleName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(ruleVariant)
                && !string.Equals(entry.Rule.RuleName ?? string.Empty, ruleVariant, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ContextMatches(entry.BaseNotable.TerritoryCode, entry.BaseNotable.CalendarType, territoryCode, calendarType)) continue;

            return (entry.Adjusted ?? entry.BaseNotable).Date;
        }

        return null;
    }

    /// <summary>
    /// Determines whether the supplied notable-date span covers the supplied day, matches the requested territory and
    /// calendar context, and is flagged as a non-working day.
    /// </summary>
    /// <param name="notable">The notable date under test.</param>
    /// <param name="day">The date to test.</param>
    /// <param name="territoryCode">The territory context, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar context, or <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the notable date covers the day and is non-working in context.</returns>
    private static bool EntryCoversDay(NotableDate notable, DateTime day, string? territoryCode, Type? calendarType)
    {
        if (!notable.IsNonWorkingDay) return false;
        return day < notable.Date.Date || day > notable.EndDate.Date
            ? false
            : ContextMatches(notable.TerritoryCode, notable.CalendarType, territoryCode, calendarType);
    }

    /// <summary>
    /// Determines whether the entry's owned territory and calendar match the requested context.
    /// </summary>
    /// <param name="ownedTerritory">The territory code stored on the entry.</param>
    /// <param name="ownedCalendar">The calendar type stored on the entry.</param>
    /// <param name="requestedTerritory">The territory context of the query.</param>
    /// <param name="requestedCalendar">The calendar context of the query.</param>
    /// <returns><see langword="true" /> when the contexts match; otherwise, <see langword="false" />.</returns>
    private static bool ContextMatches(string? ownedTerritory, Type? ownedCalendar, string? requestedTerritory, Type? requestedCalendar)
    {
        if (requestedCalendar is not null && ownedCalendar is not null && ownedCalendar != requestedCalendar)
            return false;

        if (string.IsNullOrEmpty(requestedTerritory) || string.IsNullOrEmpty(ownedTerritory))
            return true;

        if (!TerritoryCode.TryParse(requestedTerritory, out TerritoryCode requested))
            return false;
        return !TerritoryCode.TryParse(ownedTerritory, out TerritoryCode owned)
            ? false
            : requested.Contains(owned) || owned.Contains(requested);
    }
}
