// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateConversionPrecisionTests.Inverse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public sealed partial class ExchangeRateConversionPrecisionTests
{

    /// <summary>
    /// Verifies that inverting a typed rate twice returns the original rate exactly, including its reported multiplier.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenAppliedTwice_ShouldReturnOriginalExactly()
    {
        var original = ExchangeRate<USD, JPY>.From(156.42m, Date, "ECB");

        ExchangeRate<USD, JPY> roundTrip = original.Inverse().Inverse();

        Assert.AreEqual(original, roundTrip);
        Assert.AreEqual(original.Rate, roundTrip.Rate);
    }
}
