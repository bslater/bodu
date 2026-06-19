// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbServiceRegistrationTests.AddBoduEcbReferenceRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;

public partial class EcbServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the ECB provider.
    /// </summary>
    [TestMethod]
    public void AddBoduEcbReferenceRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoduEcbReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<EcbExchangeRateProvider>());
    }
}
