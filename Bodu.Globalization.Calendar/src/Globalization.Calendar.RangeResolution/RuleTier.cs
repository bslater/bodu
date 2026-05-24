// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleTier.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Classifies a <see cref="NotableDateRule" /> by the tier in which it must be processed by the chronological
/// range-resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The tier dictates processing order so that each anchor type is evaluated only after the rules it depends on. Tiers
/// are assigned once at service construction by <see cref="RuleStaticAnalysis" />.
/// </para>
/// </remarks>
internal enum RuleTier
{
    /// <summary>
    /// The rule produces its date directly without referencing any other rule. Includes
    /// <see cref="DateResolutionStrategy.Fixed" /> and <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> rules.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// The rule's date is calculated as an offset from another rule whose tier is <see cref="Fixed" />.
    /// </summary>
    OffsetFromFixed,

    /// <summary>
    /// The rule's date is computed by an algorithm. Includes <see cref="DateResolutionStrategy.Algorithm" /> rules.
    /// </summary>
    Algorithmic,

    /// <summary>
    /// The rule's date is calculated as an offset from a rule whose tier is <see cref="Algorithmic" /> (or transitively
    /// from another <see cref="OffsetFromAlgorithmic" /> rule rooted at an algorithmic anchor).
    /// </summary>
    OffsetFromAlgorithmic,
}
