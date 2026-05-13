// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateCollisionResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the default <see cref="INotableDateCollisionResolver" /> implementation. Sorts overlapping dates by category, falls back to
/// alphabetic name, and removes exact duplicates.
/// </summary>
/// <remarks>
/// <para>
/// This resolver removes structurally equal <see cref="NotableDate" /> entries first, then orders the remaining dates ascending by
/// <see cref="NotableDateCategory" /> integer value (lower values indicate higher significance) and then alphabetically by
/// <see cref="NotableDate.Name" />. It is used automatically when no custom <see cref="INotableDateCollisionResolver" /> is supplied
/// to <see cref="NotableDateService" />.
/// </para>
/// <para>
/// The implementation is intentionally conservative: it keeps every distinct occurrence on the colliding day rather than picking a
/// single "winner". Hosts that need a single canonical entry per day should supply a custom
/// <see cref="INotableDateCollisionResolver" /> through the <see cref="NotableDateService" /> constructor.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Given two notable dates resolving to 25 April 2027 — Anzac Day (a public commemoration) and Easter Sunday (a religious
/// observance) — the default resolver returns them in category-ascending order:
/// </para>
/// <code>
/// IReadOnlyList&lt;NotableDate&gt; resolved = new DefaultNotableDateCollisionResolver().Resolve(
///     new DateTime(2027, 4, 25),
///     new[]
///     {
///         new NotableDate { Date = new DateTime(2027, 4, 25), Name = "Easter Sunday",
///                           Category = NotableDateCategory.Religious },
///         new NotableDate { Date = new DateTime(2027, 4, 25), Name = "Anzac Day",
///                           Category = NotableDateCategory.Remembrance },
///     });
///
/// // resolved[0].Name == "Anzac Day"     (Remembrance has the lower category ordinal)
/// // resolved[1].Name == "Easter Sunday"
/// </code>
/// </example>
public sealed class DefaultNotableDateCollisionResolver : INotableDateCollisionResolver
{
    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateTime date, IReadOnlyList<NotableDate> overlapping)
    {
        if (overlapping is null || overlapping.Count == 0)
            return [];

        return [.. overlapping
            .Distinct()
            .OrderBy(d => (int)d.Category)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
