// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityDirection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines whether a higher or lower numeric priority value wins a same-day collision.
/// </summary>
/// <remarks>
/// <para>
/// The direction applies to same-day collision resolution only: <see cref="CollisionPolicy.HighestPriorityOnly" /> and
/// the priority tie-break of <see cref="CollisionPolicy.CategoryPriority" />. Elsewhere priority has a fixed meaning:
/// the adjustment policies a rule references are evaluated in ascending numeric <c>priority</c>, observed dates are
/// placed by earliest actual date and then the higher rule <c>priority</c>, the rule chosen within a concept is the one
/// whose territory matches most specifically, and overrides apply in document order. The recommended default is
/// <see cref="HigherWins" />.
/// </para>
/// </remarks>
public enum PriorityDirection
{
    /// <summary>
    /// A higher numeric priority value wins.
    /// </summary>
    HigherWins = 0,

    /// <summary>
    /// A lower numeric priority value wins.
    /// </summary>
    LowerWins,
}
