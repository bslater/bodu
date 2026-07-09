// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaServiceRegistrationTests.AddOandaExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class OandaServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the OANDA provider.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddOandaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<OandaExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddOandaExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOandaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        OandaExchangeRateProvider? concrete = provider.GetService<OandaExchangeRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named OANDA HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOandaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(OandaFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Oanda:Period"] = "weekly",
                ["Financial:Oanda:Price"] = "bid",
                ["Financial:Oanda:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddOandaExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OandaExchangeRateOptions options = provider.GetRequiredService<IOptions<OandaExchangeRateOptions>>().Value;

        Assert.AreEqual("weekly", options.Period);
        Assert.AreEqual("bid", options.Price);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOandaExchangeRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        OandaExchangeRateOptions options = provider.GetRequiredService<IOptions<OandaExchangeRateOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = OandaFinancialServiceBuilderExtensions.AddOandaExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOandaExchangeRates(configure: o => o.Price = "spot");
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<OandaExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddOandaExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOandaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<OandaExchangeRateProvider>());
    }
}
