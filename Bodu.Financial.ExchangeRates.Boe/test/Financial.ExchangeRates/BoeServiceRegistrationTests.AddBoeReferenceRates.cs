// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeServiceRegistrationTests.AddBoeReferenceRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    public void AddBoeReferenceRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddBoeReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<BoeExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddBoeReferenceRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeExchangeRateProvider? concrete = provider.GetService<BoeExchangeRateProvider>();
        IDatedExchangeRateProvider? dated = provider.GetService<IDatedExchangeRateProvider>();
        IExchangeRateProvider? simple = provider.GetService<IExchangeRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named Bank of England HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates();
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
    public void AddBoeReferenceRates_ShouldBindOptionsFromConfiguration()
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
        services.AddFinancialService(configuration).AddBoeReferenceRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeExchangeRateOptions options = provider.GetRequiredService<IOptions<BoeExchangeRateOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual(5, options.OnDemandWindowDays);
        Assert.AreEqual(new Uri("https://mirror.example/db/"), options.Endpoint.BaseUrl);
        Assert.AreEqual("test-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates(configure: o => o.Endpoint.UserAgent = "callback-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeExchangeRateOptions options = provider.GetRequiredService<IOptions<BoeExchangeRateOptions>>().Value;

        Assert.AreEqual("callback-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoeFinancialServiceBuilderExtensions.AddBoeReferenceRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates(configure: o => o.Series = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<BoeExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<BoeExchangeRateProvider>());
    }
}
