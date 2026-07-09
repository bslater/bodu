// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateConversionPrecisionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that inverted exchange rates convert by dividing by the originally observed rate rather than multiplying by
/// a pre-rounded reciprocal, so the conversion is precise and survives inversion and serialization.
/// </summary>
[TestClass]
public sealed partial class ExchangeRateConversionPrecisionTests
{
    /// <summary>
    /// The observation date used across the cases.
    /// </summary>
    private static readonly DateOnly Date = new(2024, 5, 30);
}
