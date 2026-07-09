// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.FromRuntime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.FromRuntime" /> adopts a runtime-tagged rate whose ISO
    /// codes match the type parameters.
    /// </summary>
    [TestMethod]
    public void FromRuntime_WhenIsoCodesMatch_ShouldReturnTypedRate()
    {
        var runtime = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, SampleDate, 1.52m, SampleProvider);

        var typed = ExchangeRate<USD, AUD>.FromRuntime(runtime);

        Assert.AreEqual(1.52m, typed.Rate);
        Assert.AreEqual("USD", typed.FromIsoCode);
        Assert.AreEqual("AUD", typed.ToIsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.FromRuntime" /> throws
    /// <see cref="InvalidOperationException" /> when the runtime rate's ISO codes differ from the type parameters.
    /// </summary>
    [TestMethod]
    public void FromRuntime_WhenIsoCodesDiffer_ShouldThrowInvalidOperationException()
    {
        var runtime = new ExchangeRate(CurrencyCode.EUR, CurrencyCode.AUD, SampleDate, 1.52m, SampleProvider);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = ExchangeRate<USD, AUD>.FromRuntime(runtime);
        });
    }
}
