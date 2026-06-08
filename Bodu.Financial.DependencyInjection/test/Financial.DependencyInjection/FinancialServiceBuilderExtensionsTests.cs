// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the fluent registration surface exposed by <see cref="FinancialServiceBuilderExtensions" />.
/// </summary>
[TestClass]
public sealed class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// A stub <see cref="IExchangeRateProvider" /> for registration tests.
    /// </summary>
    private sealed class StubRateProvider
        : IExchangeRateProvider
    {
        /// <inheritdoc />
        public decimal GetRate(string fromIsoCode, string toIsoCode) => 1m;
    }

    /// <summary>
    /// An alternative <see cref="ICurrencyLookup" /> used to verify replacement. All members are unsupported; the type
    /// exists only to confirm the registered implementation type.
    /// </summary>
    private sealed class CustomLookup
        : ICurrencyLookup
    {
        /// <inheritdoc />
        public bool TryByIsoCode(string isoCode, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByCurrencyCode(Bodu.Financial.Currencies.CurrencyCode code, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByNumericCode(int numericCode, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches) => throw new NotSupportedException();

        /// <inheritdoc />
        public IReadOnlyList<CurrencyInfo> ByCulture(System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// Verifies that <c>AddCurrencyLookup</c> replaces the default lookup registration.
    /// </summary>
    [TestMethod]
    public void AddCurrencyLookup_WhenCustomType_ShouldReplaceDefault()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial()
            .AddCurrencyLookup<CustomLookup>()
            .Services.BuildServiceProvider();

        Assert.IsInstanceOfType<CustomLookup>(provider.GetRequiredService<ICurrencyLookup>());
    }

    /// <summary>
    /// Verifies that <c>AddMonetaryContext</c> registers a keyed, resolvable context.
    /// </summary>
    [TestMethod]
    public void AddMonetaryContext_WhenNamed_ShouldResolveByKey()
    {
        MonetaryContext tax = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial()
            .AddMonetaryContext("Tax", tax)
            .Services.BuildServiceProvider();

        Assert.AreSame(tax, provider.GetRequiredKeyedService<MonetaryContext>("Tax"));
    }

    /// <summary>
    /// Verifies that <c>AddMonetaryContext</c> rejects a blank name.
    /// </summary>
    [TestMethod]
    public void AddMonetaryContext_WhenNameBlank_ShouldThrowArgumentException()
    {
        IFinancialServiceBuilder builder = new ServiceCollection().AddBoduFinancial();

        Assert.ThrowsExactly<ArgumentException>(() => builder.AddMonetaryContext("  ", MonetaryContext.Default));
    }

    /// <summary>
    /// Verifies that <c>AddExchangeRateProvider</c> registers a resolvable provider.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateProvider_WhenType_ShouldRegisterProvider()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial()
            .AddExchangeRateProvider<StubRateProvider>()
            .Services.BuildServiceProvider();

        Assert.IsInstanceOfType<StubRateProvider>(provider.GetRequiredService<IExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that no exchange-rate provider is registered by default.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenNoProviderSupplied_ShouldNotRegisterExchangeRateProvider()
    {
        ServiceProvider provider = new ServiceCollection().AddBoduFinancial().Services.BuildServiceProvider();

        Assert.IsNull(provider.GetService<IExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that <c>AddFinancialJson</c> registers configured serializer options resolvable by key.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenRegistered_ShouldProvideConfiguredOptions()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial()
            .AddFinancialJson(Serialization.FinancialJsonPolicy.Compact)
            .Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        var json = JsonSerializer.Serialize(new Money(19.99m, "USD"), options);

        Assert.AreEqual("\"19.99 USD\"", json);
    }
}
