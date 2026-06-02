// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCollisionResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves the canonical ordering, deduplication, or suppression of multiple <see cref="NotableDate" /> instances that
/// fall on the same day.
/// </summary>
/// <remarks>
/// <para>
/// When two or more rules resolve to the same day for a given territory — for example a fixed bank holiday and a
/// substitute observance, or Easter Monday landing on Anzac Day — the service consults its registered
/// <see cref="INotableDateCollisionResolver" /> via a <see cref="NotableDateCollisionContext" /> to decide how the
/// overlapping dates should be presented. The default implementation is
/// <see cref="DefaultNotableDateCollisionResolver" />, which removes exact duplicates and orders results by provenance,
/// then ascending <see cref="NotableDate.Priority" />, then <see cref="NotableDate.Category" />, then
/// <see cref="NotableDate.Name" />.
/// </para>
/// <para>
/// Supply a custom implementation via the <c>collisionResolver</c> parameter of the <see cref="NotableDateService" />
/// constructor to apply application-specific de-duplication, suppression, or ordering logic.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// An implementation that retains only the highest-precedence occurrence by ascending priority:
/// </para>
/// <code>
///<![CDATA[
/// public sealed class WinnerTakesAllCollisionResolver : INotableDateCollisionResolver
/// {
///     public IReadOnlyList<NotableDate> Resolve(NotableDateCollisionContext context)
///     {
///         NotableDate winner = context.Overlapping
///             .OrderBy(d => d.Priority)
///             .ThenBy(d => (int)d.Category)
///             .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
///             .First();
///
///         return new[] { winner };
///     }
/// }
///]]>
/// </code>
/// </example>
public interface INotableDateCollisionResolver
{
    /// <summary>
    /// Reorders, deduplicates, or filters the notable dates that share a calendar day.
    /// </summary>
    /// <param name="context">
    /// The collision context carrying the shared day, the overlapping occurrences, and their provenance.
    /// </param>
    /// <returns>The ordered notable dates to expose to consumers.</returns>
    IReadOnlyList<NotableDate> Resolve(NotableDateCollisionContext context);
}
