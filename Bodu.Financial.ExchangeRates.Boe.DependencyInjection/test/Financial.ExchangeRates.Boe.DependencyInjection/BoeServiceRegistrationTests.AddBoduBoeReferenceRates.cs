// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeServiceRegistrationTests.AddBoduBoeReferenceRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Boe.DependencyInjection;

public partial class BoeServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the Bank of England provider.
    /// </summary>
    [TestMethod]
    public void AddBoduBoeReferenceRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoduBoeReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<BoeExchangeRateProvider>());
    }
}
