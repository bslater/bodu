// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueExchangeRateExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyValueExchangeRateExtensions" /> — the dated-conversion bridge for runtime-tagged money.
/// </summary>
[TestClass]
public class MoneyValueExchangeRateExtensionsTests
{
    /// <summary>
    /// Shared as-of date for the fixture.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2024, 6, 30);

    /// <summary>
    /// Returns the shared test rate table.
    /// </summary>
    /// <returns>A provider with EUR/USD=1.10, JPY/USD=0.0067, USD/EUR=0.9091 (for inverse coverage).</returns>
    private static IDatedExchangeRateProvider BuildProvider() => new FixedDatedExchangeRateTable(
    [
        new ExchangeRate("EUR", "USD", s_asOf, 1.10m, "RBA"),
        new ExchangeRate("JPY", "USD", s_asOf, 0.0067m, "RBA"),
    ]);

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
        MoneyValue source = new((decimal)amount, from);

        MoneyValue result = source.ConvertTo(BuildProvider(), to, s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(to, result.IsoCode);
        Assert.AreEqual((decimal)expected, result.Amount);
    }

    /// <summary>
    /// Verifies the typed-target overload converts to a strongly-typed <see cref="Money{TTarget}" />.
    /// </summary>
    [TestMethod]
    public void ConvertToTyped_WhenRateAvailable_ShouldReturnTypedAmount()
    {
        MoneyValue source = new(100m, "EUR");

        Money<USD> result = source.ConvertTo<USD>(BuildProvider(), s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(new Money<USD>(110m), result);
    }

    /// <summary>
    /// Verifies that the audit-bearing overload returns the lookup metadata alongside the converted amount.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenRateAvailable_ShouldReturnAmountAndAuditMetadata()
    {
        MoneyValue source = new(100m, "EUR");

        (MoneyValue target, ExchangeRateLookupResult lookup) = source.ConvertToWithRate(
            BuildProvider(),
            "USD",
            s_asOf,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(new MoneyValue(110m, "USD"), target);
        Assert.AreEqual(1.10m, lookup.Rate.Rate);
        Assert.AreEqual("RBA", lookup.Rate.Provider);
        Assert.IsFalse(lookup.Rate.IsInverted);
        Assert.AreEqual(0, lookup.OffsetDays);
    }

    /// <summary>
    /// Verifies that the inverse-direction path correctly flips the rate and marks the result as inverted.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenOnlyInverseRateAvailable_ShouldFlagInversion()
    {
        // Only EUR/USD is in the table; converting USD → EUR uses the inverse.
        MoneyValue source = new(110m, "USD");

        (MoneyValue target, ExchangeRateLookupResult lookup) = source.ConvertToWithRate(
            BuildProvider(),
            "EUR",
            s_asOf,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual("EUR", target.IsoCode);
        Assert.IsTrue(lookup.Rate.IsInverted);
        Assert.AreEqual("RBA", lookup.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the dated conversion throws <see cref="ArgumentNullException" /> when the provider is null.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        MoneyValue source = new(100m, "EUR");

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
        MoneyValue source = new(100m, "GBP");        // no GBP rates in the fixture

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
        MoneyValue source = new(100m, "EUR");

        MoneyValue result = source.ConvertTo(BuildProvider(), "EUR", s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(source, result);
    }

    /// <summary>
    /// Verifies that the rounding-rule override is honoured at the target currency's minor-unit boundary.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenAwayFromZeroRequested_ShouldRoundMidpointAway()
    {
        IDatedExchangeRateProvider rates = new FixedDatedExchangeRateTable(
        [
            // 1.225 EUR/USD; 1 EUR × 1.225 = 1.225 → midpoint rounds to 1.22 banker's, 1.23 AwayFromZero.
            new ExchangeRate("EUR", "USD", s_asOf, 1.225m, "Bench"),
        ]);

        MoneyValue source = new(1m, "EUR");

        MoneyValue banker = source.ConvertTo(rates, "USD", s_asOf, ExchangeRateLookupOptions.Exact);
        MoneyValue awayFromZero = source.ConvertTo(rates, "USD", s_asOf, ExchangeRateLookupOptions.Exact, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.22m, banker.Amount);
        Assert.AreEqual(1.23m, awayFromZero.Amount);
    }
}
