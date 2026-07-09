// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaServiceRegistrationTests.AddRbaHistoricalRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the RBA provider.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddRbaHistoricalRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<RbaExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddRbaHistoricalRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaHistoricalRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaExchangeRateProvider? concrete = provider.GetService<RbaExchangeRateProvider>();
        IDatedExchangeRateProvider? dated = provider.GetService<IDatedExchangeRateProvider>();
        IExchangeRateProvider? simple = provider.GetService<IExchangeRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named RBA HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaHistoricalRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(RbaFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Rba:EnableDiskCache"] = "false",
                ["Financial:Rba:CurrentEraRefreshInterval"] = "06:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddRbaHistoricalRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaExchangeRateOptions options = provider.GetRequiredService<IOptions<RbaExchangeRateOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual(TimeSpan.FromHours(6), options.CurrentEraRefreshInterval);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaHistoricalRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaExchangeRateOptions options = provider.GetRequiredService<IOptions<RbaExchangeRateOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RbaFinancialServiceBuilderExtensions.AddRbaHistoricalRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaHistoricalRates(configure: o => o.Eras = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<RbaExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddRbaHistoricalRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaHistoricalRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<RbaExchangeRateProvider>());
    }
}
