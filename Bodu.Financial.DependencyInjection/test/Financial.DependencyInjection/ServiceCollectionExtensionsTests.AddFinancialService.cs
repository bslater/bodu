// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.AddFinancialService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddFinancialService</c> registers an <see cref="ICurrencyLookup" /> resolvable as a singleton.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddFinancialService_WhenCalled_ShouldRegisterCurrencyLookupSingleton()
    {
        ServiceProvider provider = new ServiceCollection().AddFinancialService().Services.BuildServiceProvider();

        ICurrencyLookup first = provider.GetRequiredService<ICurrencyLookup>();
        ICurrencyLookup second = provider.GetRequiredService<ICurrencyLookup>();

        Assert.IsInstanceOfType<CurrencyLookupService>(first);
        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that repeated registration resolves the same <see cref="ICurrencyLookup" /> instance.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenCalledTwice_ShouldRegisterLookupIdempotently()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFinancialService();
        services.AddFinancialService();

        Assert.ContainsSingle(d => d.ServiceType == typeof(ICurrencyLookup), services);
    }

    /// <summary>
    /// Verifies that the builder-callback overload throws when the callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenConfigureNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ServiceCollection().AddFinancialService((Action<IFinancialServiceBuilder>)null!);
        });
    }

    /// <summary>
    /// Verifies that a null service collection is rejected.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenServicesNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ServiceCollectionExtensions.AddFinancialService(null!);
        });
    }
}
