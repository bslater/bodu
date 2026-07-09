// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the common warm-up and discovery surface <see cref="WebExchangeRateProvider" /> contributes to every
/// provider — <see cref="WebExchangeRateProvider.LoadPairAsync" /> and
/// <see cref="WebExchangeRateProvider.GetLoadedPairs" /> — exercised through a bulk, single-base feed double.
/// </summary>
[TestClass]
public partial class WebExchangeRateProviderTests
{
    private static readonly DateOnly Known = new(2023, 1, 3);
    private static readonly DateOnly RangeStart = new(2023, 1, 1);
    private static readonly DateOnly RangeEnd = new(2023, 1, 31);

    /// <summary>
    /// Creates a cold bulk-feed provider seeded with a single AUD/USD observation on <see cref="Known" />.
    /// </summary>
    /// <returns>A newly constructed provider.</returns>
    private static TestBulkWebExchangeRateProvider CreateProvider() =>
        new(new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, Known, 0.6828m, "SEED"));
}
