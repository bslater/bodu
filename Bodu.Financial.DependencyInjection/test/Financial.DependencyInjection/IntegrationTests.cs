// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies end-to-end composition through the builder-callback registration shape.
/// </summary>
[TestClass]
public sealed class IntegrationTests
{
    /// <summary>
    /// Verifies that a full composition resolves the currency lookup, named context, exchange-rate provider, and JSON
    /// options together.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenComposedViaCallback_ShouldResolveAllServices()
    {
        MonetaryContext tax = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        ServiceProvider provider = new ServiceCollection()
            .AddFinancialService(builder => builder
                .AddMonetaryContext("Tax", tax)
                .AddExchangeRateProvider(new FixedExchangeRateTable(new Dictionary<(string, string), decimal>
                {
                    { ("USD", "AUD"), 1.5m },
                }))
                .AddFinancialJson())
            .Services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<ICurrencyLookup>());
        Assert.AreSame(tax, provider.GetRequiredKeyedService<MonetaryContext>("Tax"));

        IExchangeRateProvider rates = provider.GetRequiredService<IExchangeRateProvider>();
        Assert.AreEqual(1.5m, rates.GetRate("USD", "AUD"));
    }
}
