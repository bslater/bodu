// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheKey.cs" company="PlaceholderCompany." />
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Identifies a unique entry within the chronological range-resolution cache.
/// </summary>
/// <param name="RuleName">The notable-date rule name.</param>
/// <param name="AnchorYear">The civil year of the rule's anchor date.</param>
/// <param name="TerritoryCode">The territory the entry was materialized for, or <see langword="null" /> for territory-neutral.</param>
/// <param name="CalendarType">The calendar type the entry was materialized against, or <see langword="null" /> for calendar-neutral.</param>
/// <remarks>
/// <para>
/// Cache keys preserve the rule's authored territory and calendar context so that the same rule materialized against multiple
/// territories does not collapse into a single entry.
/// </para>
/// </remarks>
internal readonly record struct NotableDateCacheKey(
    string RuleName,
    int AnchorYear,
    string? TerritoryCode,
    Type? CalendarType);
