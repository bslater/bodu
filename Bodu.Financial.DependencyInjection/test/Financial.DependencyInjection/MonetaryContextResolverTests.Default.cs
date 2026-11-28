// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Default" /> returns <see cref="MonetaryContext.Default" />
    /// when the options configure no default context.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Default_WhenNotConfigured_ShouldReturnMonetaryContextDefault()
    {
        ServiceProvider provider = new ServiceCollection().AddFinancialService().Services.BuildServiceProvider();

        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        Assert.AreSame(MonetaryContext.Default, resolver.Default);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Default" /> returns the context that the bound
    /// <see cref="FinancialOptions.DefaultMonetaryContext" /> settings describe.
    /// </summary>
    [TestMethod]
    public void Default_WhenConfigured_ShouldReturnTheConfiguredContext()
    {
        (_, ServiceProvider provider) = Build(new()
        {
            [DefaultKey + "Rounding"] = "AwayFromZero",
            [DefaultKey + "CashRounding"] = "CurrencyCashIncrement",
        });

        MonetaryContext context = provider.GetRequiredService<IMonetaryContextResolver>().Default;

        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, context.Rounding);
        Assert.AreEqual(CashRoundingPolicy.CurrencyCashIncrement, context.CashRounding);
    }
}
