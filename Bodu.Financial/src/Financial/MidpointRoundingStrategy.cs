// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MidpointRoundingStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// An <see cref="IRoundingStrategy" /> backed by a <see cref="MidpointRounding" /> mode, rounding via
/// <see cref="decimal.Round(decimal, int, MidpointRounding)" />.
/// </summary>
public sealed class MidpointRoundingStrategy
    : IRoundingStrategy
{
    /// <summary>The shared banker's-rounding (<see cref="MidpointRounding.ToEven" />) strategy.</summary>
    public static readonly MidpointRoundingStrategy ToEven = new(MidpointRounding.ToEven);

    /// <summary>The shared away-from-zero (<see cref="MidpointRounding.AwayFromZero" />) strategy.</summary>
    public static readonly MidpointRoundingStrategy AwayFromZero = new(MidpointRounding.AwayFromZero);

    /// <summary>The midpoint-rounding mode applied by this strategy.</summary>
    private readonly MidpointRounding _mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="MidpointRoundingStrategy" /> class.
    /// </summary>
    /// <param name="mode">The midpoint-rounding mode to apply.</param>
    public MidpointRoundingStrategy(MidpointRounding mode)
    {
        _mode = mode;
    }

    /// <summary>
    /// Gets the midpoint-rounding mode applied by this strategy.
    /// </summary>
    /// <returns>The configured <see cref="MidpointRounding" /> mode.</returns>
    public MidpointRounding Mode =>
        _mode;

    /// <inheritdoc />
    public decimal Round(decimal value, int scale) =>
        decimal.Round(value, scale, _mode);
}
