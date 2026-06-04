// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Provides an <see cref="IAdjustmentHandler" /> with the information needed to compute an observed date: the calculated
/// occurrence date, the requesting territory, the firing policy, and access to the surrounding resolution context.
/// </summary>
public sealed class AdjustmentHandlerContext
{
    /// <summary>
    /// The predicate reporting whether a candidate day is already claimed by another non-working occurrence.
    /// </summary>
    private readonly Func<DateOnly, bool> _isOccupied;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentHandlerContext" /> class.
    /// </summary>
    /// <param name="baseDate">The calculated (actual) occurrence date.</param>
    /// <param name="territory">The territory code the resolution was requested for.</param>
    /// <param name="policy">The adjustment policy whose custom action is firing.</param>
    /// <param name="isOccupied">A predicate reporting whether a candidate day is already claimed.</param>
    /// <param name="resolutionContext">The resolution context used to resolve referenced rules.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territory" />, <paramref name="policy" />, <paramref name="isOccupied" />, or
    /// <paramref name="resolutionContext" /> is <see langword="null" />.
    /// </exception>
    public AdjustmentHandlerContext(
        DateOnly baseDate,
        string territory,
        AdjustmentPolicy policy,
        Func<DateOnly, bool> isOccupied,
        StrategyResolutionContext resolutionContext)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(policy);
        ThrowHelper.ThrowIfNull(isOccupied);
        ThrowHelper.ThrowIfNull(resolutionContext);

        this.BaseDate = baseDate;
        this.Territory = territory;
        this.Policy = policy;
        this._isOccupied = isOccupied;
        this.ResolutionContext = resolutionContext;
    }

    /// <summary>
    /// Gets the calculated (actual) occurrence date the handler adjusts.
    /// </summary>
    /// <returns>The calculated date.</returns>
    public DateOnly BaseDate { get; }

    /// <summary>
    /// Gets the territory code the resolution was requested for.
    /// </summary>
    /// <returns>The requested territory code.</returns>
    public string Territory { get; }

    /// <summary>
    /// Gets the adjustment policy whose custom action is firing.
    /// </summary>
    /// <returns>The firing <see cref="AdjustmentPolicy" />.</returns>
    public AdjustmentPolicy Policy { get; }

    /// <summary>
    /// Gets the resolution context the handler can use to resolve referenced rules for the same year.
    /// </summary>
    /// <returns>The <see cref="StrategyResolutionContext" />.</returns>
    public StrategyResolutionContext ResolutionContext { get; }

    /// <summary>
    /// Determines whether a candidate day is already claimed by another non-working occurrence.
    /// </summary>
    /// <param name="date">The candidate day to test.</param>
    /// <returns><see langword="true" /> when the day is already claimed; otherwise <see langword="false" />.</returns>
    public bool IsOccupied(DateOnly date) =>
        this._isOccupied(date);
}
