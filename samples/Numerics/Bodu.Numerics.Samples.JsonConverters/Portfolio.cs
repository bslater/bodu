// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Portfolio.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;

namespace Bodu.Numerics.Samples.JsonConverters;

/// <summary>
/// A small POCO whose properties mix ordinary JSON types with several <c>Bodu.Numerics</c> types, used
/// to show that once the numerics converters are registered they compose transparently inside a larger
/// object graph — no per-property attributes required.
/// </summary>
public sealed class Portfolio
{
    /// <summary>
    /// Gets or sets the portfolio name (an ordinary JSON string).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target allocation as an exact rational fraction.
    /// </summary>
    public Fraction<int> TargetWeight { get; set; }

    /// <summary>
    /// Gets or sets the acceptable price band as a continuous interval.
    /// </summary>
    public Interval<int> PriceBand { get; set; }

    /// <summary>
    /// Gets or sets the trading windows (hour ranges) as a normalized interval set.
    /// </summary>
    public IntervalSet<int> TradingHours { get; set; }
}
