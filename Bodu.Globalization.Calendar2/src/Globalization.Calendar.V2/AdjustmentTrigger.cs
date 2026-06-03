// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTrigger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Describes the condition under which an adjustment policy fires for a calculated occurrence.
/// </summary>
public sealed class AdjustmentTrigger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentTrigger" /> class.
    /// </summary>
    /// <param name="type">The kind of condition the trigger evaluates.</param>
    /// <param name="weekdays">
    /// The weekdays the trigger reacts to, used by <see cref="AdjustmentTriggerType.FallsOn" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="weekdays" /> is <see langword="null" />.</exception>
    public AdjustmentTrigger(AdjustmentTriggerType type, IEnumerable<DayOfWeek> weekdays)
    {
        ThrowHelper.ThrowIfNull(weekdays);

        this.Type = type;
        this.Weekdays = weekdays.ToArray();
    }

    /// <summary>
    /// Gets the kind of condition the trigger evaluates.
    /// </summary>
    /// <returns>The configured <see cref="AdjustmentTriggerType" />.</returns>
    public AdjustmentTriggerType Type { get; }

    /// <summary>
    /// Gets the weekdays the trigger reacts to.
    /// </summary>
    /// <returns>The configured weekdays; empty when the trigger does not use weekdays.</returns>
    public IReadOnlyList<DayOfWeek> Weekdays { get; }

    /// <summary>
    /// Determines whether the trigger fires for an occurrence on the supplied date.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <returns><see langword="true" /> if the trigger fires; otherwise <see langword="false" />.</returns>
    public bool IsTriggered(DateOnly date) =>
        this.Type switch
        {
            AdjustmentTriggerType.Always => true,
            AdjustmentTriggerType.FallsOn => this.Weekdays.Contains(date.DayOfWeek),
            _ => false,
        };
}
