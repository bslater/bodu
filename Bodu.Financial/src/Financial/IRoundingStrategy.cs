// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRoundingStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Rounds a raw monetary amount to a given number of fractional digits. Implementations encapsulate the rounding
/// convention applied at operation boundaries — banker's rounding, away-from-zero, and so on.
/// </summary>
public interface IRoundingStrategy
{
    /// <summary>
    /// Rounds <paramref name="value" /> to <paramref name="scale" /> fractional digits.
    /// </summary>
    /// <param name="value">The raw amount to round.</param>
    /// <param name="scale">The number of fractional digits to round to.</param>
    /// <returns>The rounded amount.</returns>
    decimal Round(decimal value, int scale);
}
