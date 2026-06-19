// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExchangeRateExtensionsTests.ConvertTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyExchangeRateExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo" /> converts the supplied amount through the
    /// resolved exchange rate and rounds to the target's decimal-place count.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void ConvertTo_WhenRateAvailable_ShouldReturnConvertedAmount()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);

        Money<Bodu.Financial.Currencies.AUD> converted = amount.ConvertTo<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.AUD>(
            BuildProvider(),
            s_d1,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(150.00m, converted.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo" /> resolves an inverse rate when the requested
    /// direction is not stored directly.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenUsingInverseRate_ShouldReturnReciprocalConversion()
    {
        Money<Bodu.Financial.Currencies.AUD> amount = new(150m);

        Money<Bodu.Financial.Currencies.USD> converted = amount.ConvertTo<Bodu.Financial.Currencies.AUD, Bodu.Financial.Currencies.USD>(
            BuildProvider(),
            s_d1,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(100.00m, converted.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo" /> applied to the same currency returns the
    /// original amount (via the identity rate of <c>1</c>).
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenSameCurrency_ShouldReturnOriginalAmount()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);

        Money<Bodu.Financial.Currencies.USD> converted = amount.ConvertTo<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.USD>(
            BuildProvider(),
            s_d1,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(100m, converted.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo" /> throws <see cref="KeyNotFoundException" />
    /// when the provider cannot satisfy the request.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenRateMissing_ShouldThrowKeyNotFoundException()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);
        FixedDatedExchangeRateProvider empty = new([]);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
            amount.ConvertTo<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.JPY>(
                empty,
                s_d1,
                new ExchangeRateLookupOptions(ExchangeRateDateResolution.Exact, allowSameCurrencyIdentityRate: false)));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo" /> throws when the provider is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => amount.ConvertTo<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.AUD>(
                null!,
                s_d1,
                ExchangeRateLookupOptions.Exact),
            "provider");
    }
}
