// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerContext.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Captures the inputs delivered to an <see cref="IAdjustmentHandler" />.
/// </summary>
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
