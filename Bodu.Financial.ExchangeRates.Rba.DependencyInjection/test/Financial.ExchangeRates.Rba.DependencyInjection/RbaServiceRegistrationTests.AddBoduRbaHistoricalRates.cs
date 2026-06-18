// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaServiceRegistrationTests.AddBoduRbaHistoricalRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Rba.DependencyInjection;

public partial class RbaServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the RBA provider.
    /// </summary>
    [TestMethod]
    public void AddBoduRbaHistoricalRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoduRbaHistoricalRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<RbaExchangeRateProvider>());
    }
}
