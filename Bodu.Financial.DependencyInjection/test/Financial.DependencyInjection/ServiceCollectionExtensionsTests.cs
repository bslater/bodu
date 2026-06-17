// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
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
    /// Verifies that a configuration-bound <see cref="FinancialOptions.JsonPolicy" /> drives the financial
    /// <see cref="JsonSerializerOptions" /> registered under <see cref="FinancialServiceBuilderExtensions.JsonOptionsKey" />,
    /// so binding the policy is authoritative rather than decorative.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenJsonPolicyBound_ShouldDriveRegisteredJsonOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddBoduFinancial(configuration).Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"), options);

        Assert.AreEqual("\"19.99 USD\"", json);
    }

    /// <summary>
    /// Verifies that a configuration-bound <see cref="FinancialOptions.UnknownCurrency" /> seeds the default
    /// <see cref="MoneyParseOptions" /> registered by <c>AddBoduFinancial</c>.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenUnknownCurrencyBound_ShouldSeedDefaultParseOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:UnknownCurrency"] = nameof(UnknownCurrencyPolicy.AllowUnscaled),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddBoduFinancial(configuration).Services.BuildServiceProvider();

        MoneyParseOptions parseOptions = provider.GetRequiredService<MoneyParseOptions>();

        Assert.AreEqual(UnknownCurrencyPolicy.AllowUnscaled, parseOptions.UnknownCurrency);
    }

    /// <summary>
    /// Verifies that an explicit <c>AddFinancialJson</c> overrides a JSON policy already bound from configuration,
    /// because it is registered last under the same key.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenCalledAfterBoundPolicy_ShouldOverrideRegisteredJsonOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial(configuration)
            .AddFinancialJson(FinancialJsonPolicy.Strict)
            .Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"), options);

        Assert.AreNotEqual("\"19.99 USD\"", json, "the explicit Strict policy should override the bound Compact policy");
    }

    /// <summary>
    /// Verifies that binding an undefined enum value to <see cref="FinancialOptions" /> fails options validation, so a
    /// misconfigured policy is rejected when resolved rather than silently used.
    /// </summary>
    [TestMethod]
    public void AddBoduFinancial_WhenJsonPolicyUndefined_ShouldFailOptionsValidation()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = "999",
            })
            .Build();

        ServiceProvider provider = new ServiceCollection().AddBoduFinancial(configuration).Services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<FinancialOptions>>().Value;
        });
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
