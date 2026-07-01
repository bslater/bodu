// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddMonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddMonetaryContext</c> registers a keyed, resolvable context.
    /// </summary>
    [TestMethod]
    public void AddMonetaryContext_WhenNamed_ShouldResolveByKey()
    {
        MonetaryContext tax = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        ServiceProvider provider = new ServiceCollection()
            .AddFinancialService()
            .AddMonetaryContext("Tax", tax)
            .Services.BuildServiceProvider();

        Assert.AreSame(tax, provider.GetRequiredKeyedService<MonetaryContext>("Tax"));
    }

    /// <summary>
    /// Verifies that <c>AddMonetaryContext</c> rejects a blank name.
    /// </summary>
    [TestMethod]
    public void AddMonetaryContext_WhenNameBlank_ShouldThrowArgumentException()
    {
        IFinancialServiceBuilder builder = new ServiceCollection().AddFinancialService();

        Assert.ThrowsExactly<ArgumentException>(() => builder.AddMonetaryContext("  ", MonetaryContext.Default));
    }
}
