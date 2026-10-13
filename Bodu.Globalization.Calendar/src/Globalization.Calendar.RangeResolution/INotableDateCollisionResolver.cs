// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCollisionResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Resolves a group of colliding notable-date occurrences, used when the resource's same-day or span collision policy
/// is <see cref="CollisionPolicy.Custom" />.
/// </summary>
/// <remarks>
/// <para>
/// The resolver is consulted once per group: under the same-day policy, two or more occurrences that land on the same
/// emitted date, passed with that date; under the span policy, a chain of two or more occurrences connected through
/// overlapping emitted spans that holds a multi-day occurrence, passed with the chain's earliest emitted date, after
/// the same-day groups are settled. It returns the subset to keep, in the order they should appear within their date;
/// returning an empty list suppresses the group entirely. Pass an implementation to the collaborator-aware
/// <see cref="NotableDateService" /> constructor, and set the resource's same-day or span collision policy to
/// <see cref="CollisionPolicy.Custom" /> so the resolver is invoked. Without a resolver,
/// <see cref="CollisionPolicy.Custom" /> keeps every occurrence of the group.
/// </para>
/// <para>
/// A same-day group holds every occurrence emitted on the date, including one whose span ends before the queried range,
/// and a chain is gathered whole, so the resolver sees the same group whatever range is queried and its decision does
/// not depend on the range. The service consults it only for a group that can reach the result, one holding an
/// occurrence the queried range includes or, under the span policy, belonging to a chain that does, and returns the
/// kept occurrences the range includes.
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
    /// <param name="date">
    /// The emitted date a same-day group shares, or the earliest emitted date of an overlap chain.
    /// </param>
    /// <param name="colliding">The two or more colliding occurrences, in resolution order.</param>
    /// <returns>The occurrences to keep; an empty result suppresses the date entirely.</returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding);
}
