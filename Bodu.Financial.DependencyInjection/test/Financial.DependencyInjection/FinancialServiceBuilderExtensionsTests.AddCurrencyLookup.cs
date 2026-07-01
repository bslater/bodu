// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddCurrencyLookup.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddCurrencyLookup</c> replaces the default lookup registration.
    /// </summary>
    [TestMethod]
    public void AddCurrencyLookup_WhenCustomType_ShouldReplaceDefault()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddFinancialService()
            .AddCurrencyLookup<CustomLookup>()
            .Services.BuildServiceProvider();

        Assert.IsInstanceOfType<CustomLookup>(provider.GetRequiredService<ICurrencyLookup>());
    }
}
