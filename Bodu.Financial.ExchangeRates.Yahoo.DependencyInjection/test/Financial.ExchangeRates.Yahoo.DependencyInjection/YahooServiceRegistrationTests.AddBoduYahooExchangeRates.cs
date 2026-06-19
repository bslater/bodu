// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooServiceRegistrationTests.AddBoduYahooExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection;

public partial class YahooServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the Yahoo provider.
    /// </summary>
    [TestMethod]
    public void AddBoduYahooExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoduYahooExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<YahooExchangeRateProvider>());
    }
}
