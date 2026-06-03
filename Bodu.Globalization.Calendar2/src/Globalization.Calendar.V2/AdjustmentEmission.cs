// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentEmission.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Describes which occurrences an adjustment policy emits and the reason recorded against an observed occurrence.
/// </summary>
public sealed class AdjustmentEmission
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentEmission" /> class.
    /// </summary>
    /// <param name="mode">The emission mode applied when the policy fires.</param>
    /// <param name="reason">The human-readable reason recorded against an observed occurrence, if any.</param>
    /// <param name="nonWorking">The non-working-day flag applied to the observed occurrence, if specified.</param>
    public AdjustmentEmission(EmissionMode mode, string? reason, bool? nonWorking)
    {
        this.Mode = mode;
        this.Reason = reason;
        this.NonWorking = nonWorking;
    }

    /// <summary>
    /// Gets the emission mode applied when the policy fires.
    /// </summary>
    /// <returns>The configured <see cref="EmissionMode" />.</returns>
    public EmissionMode Mode { get; }

    /// <summary>
    /// Gets the reason recorded against an observed occurrence.
    /// </summary>
    /// <returns>The reason text, or <see langword="null" /> when none is configured.</returns>
    public string? Reason { get; }

    /// <summary>
    /// Gets the non-working-day flag applied to the observed occurrence.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when the rule's default applies.</returns>
    public bool? NonWorking { get; }
}
