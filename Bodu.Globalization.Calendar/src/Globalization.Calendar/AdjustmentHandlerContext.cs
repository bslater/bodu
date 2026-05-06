// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerContext.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Carries the inputs supplied to an <see cref="IAdjustmentHandler" /> when a custom <see cref="ObservanceAdjustment" /> is being
/// evaluated against a calculated date.
/// </summary>
/// <remarks>
/// <para>
/// The context bundles the four facts a handler needs to make a decision: the <see cref="Date" /> the rule originally resolved
/// to, the <see cref="Adjustment" /> specification authored on the rule (whose <see cref="ObservanceAdjustment.HandlerKey" /> and
/// <see cref="ObservanceAdjustment.HandlerParameters" /> carry the handler's configuration), the originating <see cref="Rule" />
/// (including its name, tags, calendar, and surrounding adjustments), and the <see cref="TerritoryCode" /> /
/// <see cref="CalendarType" /> currently in scope.
/// </para>
/// <para>
/// Handlers should treat the context as read-only. Mutations performed on the embedded <see cref="Rule" /> or
/// <see cref="Adjustment" /> are not propagated back to the rule store; the only way to influence the resulting
/// <see cref="NotableDate" /> is through the returned <see cref="AdjustmentHandlerResult" />.
/// </para>
/// </remarks>
/// <param name="Date">The currently resolved date being considered for adjustment.</param>
/// <param name="Adjustment">The adjustment specification that triggered the handler.</param>
/// <param name="Rule">The originating <see cref="NotableDateRule" />.</param>
/// <param name="TerritoryCode">The territory currently being resolved, if any.</param>
/// <param name="CalendarType">The calendar currently being resolved, if any.</param>
public sealed record AdjustmentHandlerContext(
	DateTime Date,
	ObservanceAdjustment Adjustment,
	NotableDateRule Rule,
	string? TerritoryCode,
	Type? CalendarType);
