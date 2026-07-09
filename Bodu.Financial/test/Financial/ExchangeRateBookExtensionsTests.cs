// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.Extensions;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behaviour of the <see cref="ExchangeRateBookExtensions" /> provider-wrapping sugar.
/// </summary>
[TestClass]
public partial class ExchangeRateBookExtensionsTests
{
    private static readonly ExchangeRatePair UsdAud = new(CurrencyCode.USD, CurrencyCode.AUD);

    private static ExchangeRateSeries Series(string provider, decimal rate) =>
        new(UsdAud, provider, [(new DateOnly(2024, 1, 1), rate)]);
}
