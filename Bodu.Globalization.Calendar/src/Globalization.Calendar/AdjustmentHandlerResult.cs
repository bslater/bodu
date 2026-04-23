// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerResult.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Captures the output produced by an <see cref="IAdjustmentHandler" />.
/// </summary>
/// <param name="Activated">Whether the handler considered its trigger satisfied.</param>
/// <param name="AdjustedDate">The new date when <paramref name="Activated" /> is <see langword="true" />, otherwise the unchanged date.</param>
/// <param name="IsNonWorkingOverride">Optional override for the resulting date's non-working flag. <see langword="null" /> preserves the existing value.</param>
public sealed record AdjustmentHandlerResult(bool Activated, DateTime AdjustedDate, bool? IsNonWorkingOverride = null);
