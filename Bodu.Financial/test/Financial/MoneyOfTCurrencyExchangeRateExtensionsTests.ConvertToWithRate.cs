// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExchangeRateExtensionsTests.ConvertToWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyExchangeRateExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertToWithRate" /> returns the converted amount and the
    /// complete <see cref="ExchangeRateLookupResult" /> that produced it.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenRateAvailable_ShouldReturnConvertedAmountAndMetadata()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);

        MoneyConversionResult<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.AUD> result = amount.ConvertToWithRate<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.AUD>(
            BuildProvider(),
            s_d1,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(100m, result.SourceAmount.Amount);
        Assert.AreEqual(150.00m, result.TargetAmount.Amount);
        Assert.AreEqual(1.50m, result.ExchangeRate.Rate.Rate);
        Assert.AreEqual("RBA", result.ExchangeRate.Rate.Provider);
        Assert.IsFalse(result.ExchangeRate.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertToWithRate" /> throws when the provider is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => amount.ConvertToWithRate<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.AUD>(
                null!,
                s_d1,
                ExchangeRateLookupOptions.Exact),
            "provider");
    }
}
