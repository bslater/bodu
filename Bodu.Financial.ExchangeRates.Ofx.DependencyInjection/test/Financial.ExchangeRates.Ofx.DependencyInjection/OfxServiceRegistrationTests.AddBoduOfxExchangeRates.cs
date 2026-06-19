// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxServiceRegistrationTests.AddBoduOfxExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Ofx.DependencyInjection;

public partial class OfxServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the OFX provider.
    /// </summary>
    [TestMethod]
    public void AddBoduOfxExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoduOfxExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<OfxExchangeRateProvider>());
    }
}
