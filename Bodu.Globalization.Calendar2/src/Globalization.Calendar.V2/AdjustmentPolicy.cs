// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a reusable, named adjustment policy that transforms, supplements, or suppresses calculated occurrences.
/// </summary>
/// <remarks>
/// <para>
/// A policy composes four concerns: a <see cref="Scope" /> that limits where it applies, a <see cref="Trigger" /> that
/// decides whether it fires, an <see cref="Action" /> that computes the observed date, and an <see cref="Emission" />
/// that decides which occurrences are emitted. Policies are referenced by rules through stable ids.
/// </para>
/// </remarks>
public sealed class AdjustmentPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentPolicy" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the policy.</param>
    /// <param name="priority">The selection priority of the policy.</param>
    /// <param name="scope">The scope that limits where the policy applies.</param>
    /// <param name="trigger">The condition under which the policy fires.</param>
    /// <param name="action">The transformation applied when the policy fires.</param>
    /// <param name="emission">The emission behaviour applied when the policy fires.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="scope" />, <paramref name="trigger" />, <paramref name="action" />, or
    /// <paramref name="emission" /> is <see langword="null" />.
    /// </exception>
    public AdjustmentPolicy(
        string id,
        int priority,
        AdjustmentScope scope,
        AdjustmentTrigger trigger,
        AdjustmentAction action,
        AdjustmentEmission emission)
    {
        ThrowHelper.ThrowIfNull(id);
        ThrowHelper.ThrowIfNull(scope);
        ThrowHelper.ThrowIfNull(trigger);
        ThrowHelper.ThrowIfNull(action);
        ThrowHelper.ThrowIfNull(emission);

        this.Id = id;
        this.Priority = priority;
        this.Scope = scope;
        this.Trigger = trigger;
        this.Action = action;
        this.Emission = emission;
    }

    /// <summary>
    /// Gets the stable identifier of the policy.
    /// </summary>
    /// <returns>The policy id.</returns>
    public string Id { get; }

    /// <summary>
    /// Gets the selection priority of the policy.
    /// </summary>
    /// <returns>The numeric priority.</returns>
    public int Priority { get; }

    /// <summary>
    /// Gets the scope that limits where the policy applies.
    /// </summary>
    /// <returns>The <see cref="AdjustmentScope" />.</returns>
    public AdjustmentScope Scope { get; }

    /// <summary>
    /// Gets the condition under which the policy fires.
    /// </summary>
    /// <returns>The <see cref="AdjustmentTrigger" />.</returns>
    public AdjustmentTrigger Trigger { get; }

    /// <summary>
    /// Gets the transformation applied when the policy fires.
    /// </summary>
    /// <returns>The <see cref="AdjustmentAction" />.</returns>
    public AdjustmentAction Action { get; }

    /// <summary>
    /// Gets the emission behaviour applied when the policy fires.
    /// </summary>
    /// <returns>The <see cref="AdjustmentEmission" />.</returns>
    public AdjustmentEmission Emission { get; }
}
