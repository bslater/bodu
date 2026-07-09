// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeServiceRegistrationTests.AddBoeExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the Bank of England provider.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<BoeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddBoeExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeRateProvider? concrete = provider.GetService<BoeRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named Bank of England HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(BoeFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options, including the nested endpoint settings, bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Boe:EnableDiskCache"] = "false",
                ["Financial:Boe:OnDemandWindowDays"] = "5",
                ["Financial:Boe:Endpoint:BaseUrl"] = "https://mirror.example/db/",
                ["Financial:Boe:Endpoint:UserAgent"] = "test-agent",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddBoeExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeRateProviderOptions options = provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual(5, options.OnDemandWindowDays);
        Assert.AreEqual(new Uri("https://mirror.example/db/"), options.Endpoint.BaseUrl);
        Assert.AreEqual("test-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates(configure: o => o.Endpoint.UserAgent = "callback-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeRateProviderOptions options = provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value;

        Assert.AreEqual("callback-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoeFinancialServiceBuilderExtensions.AddBoeExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates(configure: o => o.Series = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<BoeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<BoeRateProvider>());
    }
}
