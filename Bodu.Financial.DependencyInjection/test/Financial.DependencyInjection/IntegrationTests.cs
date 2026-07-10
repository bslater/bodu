// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies end-to-end composition through the builder-callback registration shape.
/// </summary>
[TestClass]
public sealed class IntegrationTests
{
    /// <summary>
    /// Verifies that a full composition resolves the currency lookup, named context, and exchange-rate provider
    /// together.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenComposedViaCallback_ShouldResolveAllServices()
    {
        MonetaryContext tax = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        ServiceProvider provider = new ServiceCollection()
            .AddFinancialService(builder => builder
                .AddMonetaryContext("Tax", tax)
                .AddExchangeRateProvider(new FixedRateTable(new Dictionary<(string, string), decimal>
                {
                    { ("USD", "AUD"), 1.5m },
                })))
            .Services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<ICurrencyLookup>());
        Assert.AreSame(tax, provider.GetRequiredKeyedService<MonetaryContext>("Tax"));

        IRateProvider rates = provider.GetRequiredService<IRateProvider>();
        Assert.AreEqual(1.5m, rates.GetRate("USD", "AUD"));
    }
}
