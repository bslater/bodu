// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Computes the observed date of an occurrence for an adjustment policy whose action is
/// <see cref="AdjustmentAction.Custom" />, supplementing the engine's built-in actions with caller-supplied logic.
/// </summary>
/// <remarks>
/// <para>
/// A handler is bound to a policy through the policy's handler key and is consulted during the placement phase, after
/// the actual date has been calculated. It receives the calculated date together with the surrounding resolution
/// context, so it can resolve other rules or test whether candidate days are already claimed by non-working
/// occurrences.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Move the occurrence to the next day that is not already claimed by another non-working date.
/// public sealed class NextFreeDayHandler : IAdjustmentHandler
/// {
///     public DateOnly? Adjust(AdjustmentHandlerContext context)
///     {
///         DateOnly date = context.BaseDate;
///         while (context.IsOccupied(date))
///             date = date.AddDays(1);
///
///         return date == context.BaseDate ? null : date; // null leaves it on the calculated date
///     }
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="AdjustmentHandlerContext" /> <seealso cref="AdjustmentAction" />
/// <seealso cref="IAdjustmentHandlerRegistry" /> <seealso href="../guides/calendar/adjustment-rules.html">Observance
/// adjustment rules (guide)</seealso>
public interface IAdjustmentHandler
{
    /// <summary>
    /// Computes the observed date for an occurrence.
    /// </summary>
    /// <param name="context">The context describing the occurrence being adjusted.</param>
    /// <returns>The observed date, or <see langword="null" /> to leave the occurrence on its calculated date.</returns>
    DateOnly? Adjust(AdjustmentHandlerContext context);
}
