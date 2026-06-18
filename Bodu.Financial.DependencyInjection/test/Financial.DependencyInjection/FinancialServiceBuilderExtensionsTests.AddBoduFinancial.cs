// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddBoduFinancial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that no exchange-rate provider is registered by default.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenNoProviderSupplied_ShouldNotRegisterExchangeRateProvider()
    {
        ServiceProvider provider = new ServiceCollection().AddBoduFinancial().Services.BuildServiceProvider();

        Assert.IsNull(provider.GetService<IExchangeRateProvider>());
    }
}
