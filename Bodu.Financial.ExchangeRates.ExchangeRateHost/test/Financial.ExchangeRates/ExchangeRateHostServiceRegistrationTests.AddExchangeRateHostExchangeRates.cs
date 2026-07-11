// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostServiceRegistrationTests.AddExchangeRateHostExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateHostServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the exchangerate.host
    /// provider.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddExchangeRateHostExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<ExchangeRateHostRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddExchangeRateHostExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddExchangeRateHostExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        ExchangeRateHostRateProvider? concrete = provider.GetService<ExchangeRateHostRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named exchangerate.host HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddExchangeRateHostExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(ExchangeRateHostFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:ExchangeRateHost:ApiKey"] = "bound-key",
                ["Financial:ExchangeRateHost:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddExchangeRateHostExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ExchangeRateHostRateProviderOptions options = provider.GetRequiredService<IOptions<ExchangeRateHostRateProviderOptions>>().Value;

        Assert.AreEqual("bound-key", options.ApiKey);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExchangeRateHostFinancialServiceBuilderExtensions.AddExchangeRateHostExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options (a missing API key) fail fast through <c>ValidateOnStart</c> when the provider is
    /// resolved.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_WhenApiKeyMissing_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddExchangeRateHostExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<ExchangeRateHostRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddExchangeRateHostExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddExchangeRateHostExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<ExchangeRateHostRateProvider>());
    }
}
