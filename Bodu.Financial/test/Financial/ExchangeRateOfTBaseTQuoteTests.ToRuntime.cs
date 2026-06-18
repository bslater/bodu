// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.ToRuntime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.ToRuntime" /> returns an equivalent runtime-tagged
    /// <see cref="ExchangeRate" />.
    /// </summary>
    [TestMethod]
    public void ToRuntime_WhenCalled_ShouldReturnEquivalentRuntimeRate()
    {
        var typed = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        ExchangeRate runtime = typed.ToRuntime();

        Assert.AreEqual("USD", runtime.FromIsoCode);
        Assert.AreEqual("AUD", runtime.ToIsoCode);
        Assert.AreEqual(SampleDate, runtime.Date);
        Assert.AreEqual(1.52m, runtime.Rate);
        Assert.AreEqual(SampleProvider, runtime.Provider);
    }
}
