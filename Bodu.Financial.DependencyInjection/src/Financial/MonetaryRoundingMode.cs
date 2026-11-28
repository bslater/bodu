// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryRoundingMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies the rounding strategy that a configured <see cref="MonetaryContextOptions" /> gives the
/// <see cref="MonetaryContext" /> it builds.
/// </summary>
/// <remarks>
/// The first five members have the values and the meaning of the <see cref="MidpointRounding" /> members of the same
/// names and select a <see cref="MidpointRoundingStrategy" />; <see cref="Stochastic" /> selects
/// <see cref="StochasticRoundingStrategy.Shared" />. Configuration binds a member by its name, case-insensitively.
/// </remarks>
public enum MonetaryRoundingMode
{
    /// <summary>
    /// Rounds a midpoint to the nearest even digit (banker's rounding), the rounding of
    /// <see cref="MonetaryContext.Default" />.
    /// </summary>
    ToEven = 0,

    /// <summary>
    /// Rounds a midpoint to the nearest digit away from zero.
    /// </summary>
    AwayFromZero = 1,

    /// <summary>
    /// Rounds toward zero, discarding the digits beyond the scale.
    /// </summary>
    ToZero = 2,

    /// <summary>
    /// Rounds toward negative infinity.
    /// </summary>
    ToNegativeInfinity = 3,

    /// <summary>
    /// Rounds toward positive infinity.
    /// </summary>
    ToPositiveInfinity = 4,

    /// <summary>
    /// Rounds up with a probability equal to the fraction discarded, which leaves sums of rounded amounts unbiased.
    /// </summary>
    Stochastic = 5,
}
