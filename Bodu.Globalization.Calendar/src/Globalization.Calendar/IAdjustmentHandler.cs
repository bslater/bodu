// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandler.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Implements custom evaluation and modification logic for an <see cref="ObservanceAdjustment" /> whose
/// <see cref="ObservanceAdjustment.Trigger" /> or <see cref="ObservanceAdjustment.Action" /> is set to
/// <see cref="AdjustmentTrigger.Custom" /> or <see cref="AdjustmentAction.Custom" />.
/// </summary>
/// <remarks>
/// <para>
/// Custom handlers are looked up by <see cref="ObservanceAdjustment.HandlerKey" /> from an <see cref="IAdjustmentHandlerRegistry" />.
/// They receive the resolved date plus the surrounding context and return whether the adjustment activates and, if so, the new date.
/// </para>
/// <para>
/// Register handlers via <see cref="AdjustmentHandlerRegistry" /> and supply the registry to the <see cref="NotableDateService" />
/// constructor via the <c>adjustmentHandlers</c> parameter.
/// </para>
/// </remarks>
/// <example>
/// <para>A custom handler that shifts the observed date when it coincides with another named notable date:</para>
/// <code>
/// public sealed class ConflictAvoidanceHandler : IAdjustmentHandler
/// {
///     private readonly string _conflictingRuleName;
///
///     public ConflictAvoidanceHandler(string conflictingRuleName)
///         => _conflictingRuleName = conflictingRuleName;
///
///     public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context)
///     {
///         // Access parameters supplied on the ObservanceAdjustment:
///         bool checkEnabled = context.Adjustment.HandlerParameters?.ContainsKey("enabled") == true;
///         if (!checkEnabled)
///             return new AdjustmentHandlerResult(Activated: false, AdjustedDate: context.Date);
///
///         // Shift by one day to avoid the conflict:
///         DateTime shifted = context.Date.AddDays(1);
///         return new AdjustmentHandlerResult(Activated: true, AdjustedDate: shifted, IsNonWorkingOverride: true);
///     }
/// }
/// </code>
/// </example>
public interface IAdjustmentHandler
{
    /// <summary>
    /// Evaluates the adjustment and, if active, computes the new date.
    /// </summary>
    /// <param name="context">The current adjustment context, including the date being evaluated and surrounding metadata.</param>
    /// <returns>An <see cref="AdjustmentHandlerResult" /> describing whether the handler activated and what date it produced.</returns>
    AdjustmentHandlerResult Apply(AdjustmentHandlerContext context);
}
