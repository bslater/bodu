// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the registration semantics of the <see cref="ServiceCollectionExtensions.AddBoduFinancial(IServiceCollection, IConfiguration?, string)" />
/// family.
/// </summary>
[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddBoduFinancial</c> registers an <see cref="ICurrencyLookup" /> resolvable as a singleton.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddBoduFinancial_WhenCalled_ShouldRegisterCurrencyLookupSingleton()
    {
        ServiceProvider provider = new ServiceCollection().AddBoduFinancial().Services.BuildServiceProvider();

        ICurrencyLookup first = provider.GetRequiredService<ICurrencyLookup>();
        ICurrencyLookup second = provider.GetRequiredService<ICurrencyLookup>();

        Assert.IsInstanceOfType<CurrencyLookupService>(first);
        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that the configuration-driven overload binds <see cref="FinancialOptions" /> from the named section.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenConfigurationSupplied_ShouldBindOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
                ["Financial:UnknownCurrency"] = nameof(UnknownCurrencyPolicy.AllowUnscaled),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddBoduFinancial(configuration).Services.BuildServiceProvider();

        FinancialOptions options = provider.GetRequiredService<IOptions<FinancialOptions>>().Value;

        Assert.AreEqual(FinancialJsonPolicy.Compact, options.JsonPolicy);
        Assert.AreEqual(UnknownCurrencyPolicy.AllowUnscaled, options.UnknownCurrency);
    }

    /// <summary>
    /// Verifies that repeated registration resolves the same <see cref="ICurrencyLookup" /> instance.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenCalledTwice_ShouldRegisterLookupIdempotently()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddBoduFinancial();
        services.AddBoduFinancial();

        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICurrencyLookup)));
    }

    /// <summary>
    /// Verifies that the builder-callback overload throws when the callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenConfigureNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ServiceCollection().AddBoduFinancial((Action<IFinancialServiceBuilder>)null!);
        });
    }

    /// <summary>
    /// Verifies that a null service collection is rejected.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenServicesNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ServiceCollectionExtensions.AddBoduFinancial(null!);
        });
    }
}
