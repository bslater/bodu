// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.AddFinancialService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    /// Verifies that the configuration-driven overload binds <see cref="FinancialOptions" /> from the named section.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenConfigurationSupplied_ShouldBindOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddFinancialService(configuration).Services.BuildServiceProvider();

        FinancialOptions options = provider.GetRequiredService<IOptions<FinancialOptions>>().Value;

        Assert.AreEqual(FinancialJsonPolicy.Compact, options.JsonPolicy);
    }

    /// <summary>
    /// Verifies that a configuration-bound <see cref="FinancialOptions.JsonPolicy" /> drives the financial
    /// <see cref="JsonSerializerOptions" /> registered under <see cref="FinancialServiceBuilderExtensions.JsonOptionsKey" />,
    /// so binding the policy is authoritative rather than decorative.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenJsonPolicyBound_ShouldDriveRegisteredJsonOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddFinancialService(configuration).Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreEqual("\"19.99 USD\"", json);
    }

    /// <summary>
    /// Verifies that binding an undefined enum value to <see cref="FinancialOptions" /> fails options validation, so a
    /// misconfigured policy is rejected when resolved rather than silently used.
    /// </summary>
    [TestMethod]
    public void AddFinancialService_WhenJsonPolicyUndefined_ShouldFailOptionsValidation()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = "999",
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddFinancialService(configuration).Services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<FinancialOptions>>().Value;
        });
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

        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICurrencyLookup)));
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
