// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.Inverse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.Inverse" /> returns the typed reciprocal rate with the
    /// inversion flag toggled.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenCalled_ShouldReturnReciprocalTypedRateWithToggledInversion()
    {
        var rate = new ExchangeRate<USD, AUD>(2m, SampleDate, SampleProvider);

        ExchangeRate<AUD, USD> inverse = rate.Inverse();

        Assert.AreEqual("AUD", inverse.FromIsoCode);
        Assert.AreEqual("USD", inverse.ToIsoCode);
        Assert.AreEqual(0.5m, inverse.Rate);
        Assert.IsTrue(inverse.IsInverted);
    }
}
