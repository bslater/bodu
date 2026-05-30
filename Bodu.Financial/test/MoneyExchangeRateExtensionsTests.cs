// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class MoneyExchangeRateExtensionsTests
{
    private static readonly DateOnly s_d1 = new(2024, 1, 3);

    private static FixedDatedExchangeRateTable BuildProvider() => new(new[]
    {
        new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
    });

    /// <summary>
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertTo" /> converts the supplied amount through the
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
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertToWithRate" /> returns the converted amount and the
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
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertTo" /> resolves an inverse rate when the requested
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
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertTo" /> applied to the same currency returns the
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
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertTo" /> throws <see cref="KeyNotFoundException" />
    /// when the provider cannot satisfy the request.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenRateMissing_ShouldThrowKeyNotFoundException()
    {
        Money<Bodu.Financial.Currencies.USD> amount = new(100m);
        FixedDatedExchangeRateTable empty = new(Array.Empty<ExchangeRate>());

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
            amount.ConvertTo<Bodu.Financial.Currencies.USD, Bodu.Financial.Currencies.JPY>(
                empty,
                s_d1,
                new ExchangeRateLookupOptions(ExchangeRateDateResolution.Exact, AllowSameCurrencyIdentityRate: false)));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertTo" /> throws when the provider is
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

    /// <summary>
    /// Verifies that <see cref="MoneyExchangeRateExtensions.ConvertToWithRate" /> throws when the provider is
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
