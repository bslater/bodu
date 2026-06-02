// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Identifies a unique entry within the chronological range-resolution cache.
/// </summary>
/// <param name="Identity">
/// The composite identity (canonical name, rule-level variant name, materialized territory, and calendar type) of the
/// cached occurrence.
/// </param>
/// <param name="AnchorYear">The civil year of the rule's anchor date.</param>
/// <remarks>
/// <para>
/// Keying on the full <see cref="NotableDateRuleIdentity" /> preserves the rule's variant
/// (<see cref="NotableDateRule.RuleName" />), materialized territory, and calendar context, so that distinct variants
/// of the same canonical name — for example western and orthodox Easter — and the same rule materialized against
/// multiple territories never collapse into a single cache entry.
/// </para>
/// </remarks>
internal readonly record struct NotableDateCacheKey(
    NotableDateRuleIdentity Identity,
    int AnchorYear);
