// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollisionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines how the resolver treats multiple distinct notable dates that resolve to the same day or overlapping span.
/// </summary>
/// <remarks>
/// <para>
/// The recommended default is <see cref="KeepAll" />, because multiple notable dates can legitimately occur on the same
/// day. The other policies thin a same-day group by priority or category, or defer to an
/// <see cref="INotableDateCollisionResolver" /> when set to <see cref="Custom" />.
/// </para>
/// </remarks>
/// <seealso cref="INotableDateCollisionResolver" />
/// <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity, priority, and observed-date resolution (guide)</seealso>
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
