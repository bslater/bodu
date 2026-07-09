// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behaviour of the <see cref="RateBookExtensions" /> provider-wrapping sugar.
/// </summary>
[TestClass]
public partial class RateBookExtensionsTests
{
    private static readonly CurrencyPair UsdAud = new(CurrencyCode.USD, CurrencyCode.AUD);

    private static RateSeries Series(string provider, decimal rate) =>
        new(UsdAud, provider, [(new DateOnly(2024, 1, 1), rate)]);
}
