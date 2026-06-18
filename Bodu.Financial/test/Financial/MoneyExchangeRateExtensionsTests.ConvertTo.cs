// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensionsTests.ConvertTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyExchangeRateExtensionsTests
{

    /// <summary>
    /// Verifies that the runtime-tagged dated convert path produces the expected target amount across a matrix
    /// of source/target combinations.
    /// </summary>
    [TestMethod]
    [DataRow("EUR", "USD", 100, 110.00)]      // direct EUR/USD
    [DataRow("USD", "EUR", 110, 100.00)]      // inverse (allowed)
    [DataRow("JPY", "USD", 10000, 67.00)]     // direct JPY/USD
    [DataRow("USD", "USD", 50, 50.00)]        // same-currency identity
    public void ConvertTo_WhenRateAvailable_ShouldReturnConvertedRuntimeAmount(string from, string to, double amount, double expected)
    {
        Money source = new((decimal)amount, from);

        Money result = source.ConvertTo(BuildProvider(), to, s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(to, result.IsoCode);
        Assert.AreEqual((decimal)expected, result.Amount);
    }

    /// <summary>
    /// Verifies that the dated conversion throws <see cref="ArgumentNullException" /> when the provider is null.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        Money source = new(100m, "EUR");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.ConvertTo(null!, "USD", s_asOf, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that the dated conversion throws <see cref="KeyNotFoundException" /> when no rate is available.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenRateUnavailable_ShouldThrowKeyNotFoundException()
    {
        Money source = new(100m, "GBP");        // no GBP rates in the fixture

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = source.ConvertTo(BuildProvider(), "USD", s_asOf, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that requesting conversion to the source currency under
    /// <see cref="ExchangeRateLookupOptions.AllowSameCurrencyIdentityRate" /> short-circuits without rate lookup,
    /// even when the provider has no observation for the (X, X) pair.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenSourceAndTargetAreSameCurrency_ShouldReturnSourceAmount()
    {
        Money source = new(100m, "EUR");

        Money result = source.ConvertTo(BuildProvider(), "EUR", s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(source, result);
    }

    /// <summary>
    /// Verifies that the rounding-rule override is honoured at the target currency's minor-unit boundary.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenAwayFromZeroRequested_ShouldRoundMidpointAway()
    {
        IDatedExchangeRateProvider rates = new FixedDatedExchangeRateProvider(
        [
            // 1.225 EUR/USD; 1 EUR × 1.225 = 1.225 → midpoint rounds to 1.22 banker's, 1.23 AwayFromZero.
            new ExchangeRate("EUR", "USD", s_asOf, 1.225m, "Bench"),
        ]);

        Money source = new(1m, "EUR");

        Money banker = source.ConvertTo(rates, "USD", s_asOf, ExchangeRateLookupOptions.Exact);
        Money awayFromZero = source.ConvertTo(rates, "USD", s_asOf, ExchangeRateLookupOptions.Exact, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.22m, banker.Amount);
        Assert.AreEqual(1.23m, awayFromZero.Amount);
    }
}
