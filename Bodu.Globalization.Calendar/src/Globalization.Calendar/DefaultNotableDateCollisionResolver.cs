// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateCollisionResolver.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the default <see cref="INotableDateCollisionResolver" /> implementation. Removes exact duplicates and orders
/// the overlapping dates by a documented precedence.
/// </summary>
/// <remarks>
/// <para>
/// Same-day occurrences are ordered by the following precedence, most significant first:
/// </para>
/// <list type="number">
/// <item><description>Provenance, descending — a runtime override outranks a local rule, which outranks an imported rule.</description></item>
/// <item><description>Ascending <see cref="NotableDate.Priority" /> — a lower value indicates higher precedence.</description></item>
/// <item><description>Ascending <see cref="NotableDateCategory" /> ordinal — a lower value indicates higher significance.</description></item>
/// <item><description>Alphabetical <see cref="NotableDate.Name" />, then <see cref="NotableDate.TerritoryCode" />.</description></item>
/// </list>
/// <para>
/// The implementation is intentionally conservative: it keeps every distinct occurrence on the colliding day rather than
/// picking a single "winner". Hosts that need a single canonical entry per day should supply a custom
/// <see cref="INotableDateCollisionResolver" /> through the <see cref="NotableDateService" /> constructor and take the
/// first element.
/// </para>
/// </remarks>
public sealed class DefaultNotableDateCollisionResolver
    : INotableDateCollisionResolver
{
    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(NotableDateCollisionContext context)
    {
        if (context is null || context.Overlapping.Count == 0)
            return [];

        return [.. context.Overlapping
            .Select((notable, index) => (Notable: notable, Provenance: context.Provenances[index]))
            .OrderByDescending(pair => (int)pair.Provenance)
            .ThenBy(pair => pair.Notable.Priority)
            .ThenBy(pair => (int)pair.Notable.Category)
            .ThenBy(pair => pair.Notable.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Notable.TerritoryCode, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Notable)
            .Distinct()];
    }
}
