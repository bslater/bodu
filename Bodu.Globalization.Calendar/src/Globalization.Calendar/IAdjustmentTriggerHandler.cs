// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentTriggerHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Decides whether an adjustment policy whose trigger is <see cref="AdjustmentTrigger.Custom" /> fires for a calculated
/// occurrence, supplementing the engine's built-in triggers with caller-supplied logic.
/// </summary>
/// <remarks>
/// <para>
/// A trigger handler is bound to a policy through the policy's trigger handler key and is consulted during candidate
/// gathering, before the observed date is computed. It receives the calculated date together with the surrounding
/// resolution context, so it can resolve other rules for the same year when deciding whether to fire.
/// </para>
/// </remarks>
public interface IAdjustmentTriggerHandler
{
    /// <summary>
    /// Determines whether the policy fires for the calculated occurrence.
    /// </summary>
    /// <param name="context">The context describing the occurrence being evaluated.</param>
    /// <returns><see langword="true" /> if the policy should fire; otherwise <see langword="false" />.</returns>
    bool ShouldAdjust(AdjustmentTriggerContext context);
}
