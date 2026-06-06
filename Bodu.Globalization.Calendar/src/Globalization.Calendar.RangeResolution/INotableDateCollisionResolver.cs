// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCollisionResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Resolves a set of notable-date occurrences that share the same emitted date, used when the resource's same-day
/// collision policy is <see cref="CollisionPolicy.Custom" />.
/// </summary>
/// <remarks>
/// <para>
/// The resolver is consulted once per group of two or more occurrences that land on the same emitted date. It returns
/// the subset to keep, in the order they should appear; returning an empty list suppresses the date entirely. Pass an
/// implementation to the collaborator-aware <see cref="NotableDateService" /> constructor, and set the resource's
/// same-day collision policy to <see cref="CollisionPolicy.Custom" /> so the resolver is invoked.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Keep only the highest-priority occurrence on a contested day, breaking ties by name.
/// public sealed class HighestPriorityResolver : INotableDateCollisionResolver
/// {
///     public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)
///     {
///         int best = colliding.Max(n => n.Priority);
///         return colliding
///             .Where(n => n.Priority == best)
///             .OrderBy(n => n.DisplayName, StringComparer.Ordinal)
///             .ToList();
///     }
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="CollisionPolicy" /> <seealso cref="NotableDateService" />
/// <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity, priority, and observed-date
/// resolution (guide)</seealso>
public interface INotableDateCollisionResolver
{
    /// <summary>
    /// Selects which of the colliding occurrences to keep.
    /// </summary>
    /// <param name="date">The emitted date the occurrences share.</param>
    /// <param name="colliding">The two or more occurrences emitted on the date, in resolution order.</param>
    /// <returns>The occurrences to keep; an empty result suppresses the date entirely.</returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding);
}
