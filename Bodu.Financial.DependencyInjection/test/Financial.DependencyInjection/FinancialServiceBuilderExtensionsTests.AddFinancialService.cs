// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddFinancialService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that no exchange-rate provider is registered by default.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenNoProviderSupplied_ShouldNotRegisterExchangeRateProvider()
    {
        ServiceProvider provider = new ServiceCollection().AddFinancialService().Services.BuildServiceProvider();

        Assert.IsNull(provider.GetService<IExchangeRateProvider>());
    }
}
