// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
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
}
