// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooServiceRegistrationTests.AddYahooExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class YahooServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the Yahoo provider.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddYahooExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<YahooExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddYahooExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddYahooExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        YahooExchangeRateProvider? concrete = provider.GetService<YahooExchangeRateProvider>();
        IDatedExchangeRateProvider? dated = provider.GetService<IDatedExchangeRateProvider>();
        IExchangeRateProvider? simple = provider.GetService<IExchangeRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named Yahoo HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddYahooExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(YahooFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Yahoo:BaseAddress"] = "https://query2.finance.yahoo.com/",
                ["Financial:Yahoo:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddYahooExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        YahooExchangeRateOptions options = provider.GetRequiredService<IOptions<YahooExchangeRateOptions>>().Value;

        Assert.AreEqual(new Uri("https://query2.finance.yahoo.com/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddYahooExchangeRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        YahooExchangeRateOptions options = provider.GetRequiredService<IOptions<YahooExchangeRateOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = YahooFinancialServiceBuilderExtensions.AddYahooExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddYahooExchangeRates(configure: o => o.ChartPath = "no-placeholder");
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<YahooExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddYahooExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddYahooExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<YahooExchangeRateProvider>());
    }
}
