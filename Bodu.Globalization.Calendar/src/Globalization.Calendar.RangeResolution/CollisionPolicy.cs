// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollisionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines how the resolver treats multiple distinct notable dates that resolve to the same day.
/// </summary>
/// <remarks>
/// <para>
/// The recommended default is <see cref="KeepAll" />, because multiple notable dates can legitimately occur on the same
/// day. The other policies thin a same-day group by priority or category, or defer to an
/// <see cref="INotableDateCollisionResolver" /> when set to <see cref="Custom" />.
/// </para>
/// <para>
/// The service applies the resource's <see cref="ResolutionPolicy.SameDayCollisionPolicy" /> to occurrences that share
/// an emitted date. <see cref="ResolutionPolicy.SpanCollisionPolicy" />, meant for overlapping multi-day spans, is
/// reserved: it is stored but not applied, so overlapping spans are never reconciled (<see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>).
/// </para>
/// <para>
/// A collision is settled once per emitted date, over every occurrence emitted on that date, whatever range is queried:
/// a multi-day occurrence takes part only in the collision of its emitted date, and when it loses there it is absent
/// from every query its span reaches into, including one whose window starts after the emitted date.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Two concepts landing on the same day: HighestPriorityOnly keeps the winner,
/// // KeepAll surfaces both. Set via the resolution policy:
/// NotableDateDocumentBuilder.Create("demo")
///     .WithResolutionPolicy(p => p.WithSameDayCollisionPolicy(CollisionPolicy.HighestPriorityOnly));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="INotableDateCollisionResolver" /> <seealso href="../guides/calendar/identity-and-resolution.html">
/// Rule identity, priority, and observed-date resolution (guide)</seealso>
public enum CollisionPolicy
{
    /// <summary>
    /// Keep every colliding occurrence.
    /// </summary>
    KeepAll = 0,

    /// <summary>
    /// Keep only the occurrence with the highest priority.
    /// </summary>
    HighestPriorityOnly,

    /// <summary>
    /// Resolve collisions using category precedence.
    /// </summary>
    CategoryPriority,

    /// <summary>
    /// Resolve collisions using a custom strategy.
    /// </summary>
    Custom,
}
