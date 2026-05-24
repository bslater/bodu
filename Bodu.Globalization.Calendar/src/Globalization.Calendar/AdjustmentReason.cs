// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentReason.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Describes why a <see cref="NotableDate" /> was shifted from its originally calculated date by an
/// <see cref="ObservanceAdjustment" />.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="AdjustmentReason" /> is attached to a <see cref="NotableDate" /> (via
/// <see cref="NotableDate.AdjustmentReason" />) when, and only when, the date was relocated by a triggered observance
/// adjustment. The originally calculated date is preserved on <see cref="OriginalDate" /> so consumers can render
/// observed-vs-actual labels (for example "Observed Monday 27 April — Anzac Day fell on Sunday") and so that diagnostic
/// and audit code can explain why a holiday is showing in an unexpected place.
/// </para>
/// <para>
/// The presence of this reason is what flips <see cref="NotableDate.WasAdjusted" /> to <see langword="true" />. The
/// base occurrence — the date the rule originally resolved to — is still emitted alongside the shifted occurrence so
/// that a single rule produces both the actual and the observed dates. <see cref="HandlerKey" /> is populated only when
/// a custom <see cref="IAdjustmentHandler" /> performed the shift via <see cref="AdjustmentAction.Custom" />, allowing
/// diagnostics to trace the responsible component by registry key.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Read the adjustment reason off a resolved <see cref="NotableDate" /> and render it to a user. For Anzac Day 2025
/// (which falls on a Friday and is unaffected) and Anzac Day 2026 (which falls on Saturday 25 April and is rolled to
/// Monday 27 April for observance), the resulting <see cref="NotableDate" /> values look like this:
/// </para>
/// <code>
///<![CDATA[
/// foreach (NotableDate date in service.GetNotableDates(2026, territoryCode: "AU"))
/// {
///     if (date.AdjustmentReason is { } reason)
///     {
///         // date.Date           == 2026-04-27 (Monday — observed)
///         // reason.OriginalDate == 2026-04-25 (Saturday — actual)
///         // reason.Trigger      == AdjustmentTrigger.IfWeekend
///         // reason.Action       == AdjustmentAction.MoveToNextMonday
///         Console.WriteLine(
///             $"{date.Name}: observed {date.Date:d} (actual {reason.OriginalDate:d}, " +
///             $"shifted because {reason.Trigger} via {reason.Action})");
///     }
/// }
///]]>
/// </code>
/// </example>
public sealed record AdjustmentReason
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentReason" /> class.
    /// </summary>
    /// <param name="originalDate">The date that was originally calculated before the adjustment fired.</param>
    /// <param name="trigger">The trigger condition that activated the adjustment.</param>
    /// <param name="action">The action that the adjustment performed.</param>
    /// <param name="handlerKey">
    /// Optional key identifying the custom handler when <paramref name="action" /> is
    /// <see cref="AdjustmentAction.Custom" />.
    /// </param>
    public AdjustmentReason(DateTime originalDate, AdjustmentTrigger trigger, AdjustmentAction action, string? handlerKey = null)
    {
        OriginalDate = originalDate;
        Trigger = trigger;
        Action = action;
        HandlerKey = handlerKey;
    }

    /// <summary>
    /// Gets the date that was originally calculated for the notable date before the adjustment fired.
    /// </summary>
    /// <returns>The unshifted anchor date as produced by the rule's <see cref="DateResolutionStrategy" />.</returns>
    public DateTime OriginalDate { get; }

    /// <summary>
    /// Gets the trigger condition that activated the adjustment.
    /// </summary>
    /// <returns>
    /// The <see cref="AdjustmentTrigger" /> value supplied on the <see cref="ObservanceAdjustment" />.
    /// </returns>
    public AdjustmentTrigger Trigger { get; }

    /// <summary>
    /// Gets the action that the adjustment performed.
    /// </summary>
    /// <returns>
    /// The <see cref="AdjustmentAction" /> value supplied on the <see cref="ObservanceAdjustment" />.
    /// </returns>
    public AdjustmentAction Action { get; }

    /// <summary>
    /// Gets the optional key identifying the custom handler that performed a <see cref="AdjustmentAction.Custom" />
    /// action.
    /// </summary>
    /// <returns>
    /// The <see cref="ObservanceAdjustment.HandlerKey" /> of the custom handler, or <see langword="null" /> for
    /// built-in actions.
    /// </returns>
    public string? HandlerKey { get; }
}
