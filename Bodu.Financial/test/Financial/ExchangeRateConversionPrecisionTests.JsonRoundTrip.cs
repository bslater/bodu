// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateConversionPrecisionTests.JsonRoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public sealed partial class ExchangeRateConversionPrecisionTests
{

    /// <summary>
    /// Verifies that an inverted rate keeps its precise division semantics across a JSON round-trip.
    /// </summary>
    [TestMethod]
    public void JsonRoundTrip_WhenInvertedFromObservedRate_ShouldPreservePreciseConversion()
    {
        var inverted = ExchangeRate.FromObservedRate(CurrencyCode.JPY, CurrencyCode.USD, Date, 156.42m, "ECB", isInverted: true);

        string json = JsonSerializer.Serialize(inverted);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json);

        Assert.AreEqual(inverted, recovered);
        Assert.AreEqual(1m, recovered.Convert(156.42m));
    }
}
