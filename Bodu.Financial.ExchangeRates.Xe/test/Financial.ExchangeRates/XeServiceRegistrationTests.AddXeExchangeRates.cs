// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeServiceRegistrationTests.AddXeExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class XeServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the XE provider.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddXeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<XeRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddXeExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        XeRateProvider? concrete = provider.GetService<XeRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named XE HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(XeFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Xe:ChartingRatesPath"] = "api/protected/other-rates/",
                ["Financial:Xe:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddXeExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        XeRateProviderOptions options = provider.GetRequiredService<IOptions<XeRateProviderOptions>>().Value;

        Assert.AreEqual("api/protected/other-rates/", options.ChartingRatesPath);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        XeRateProviderOptions options = provider.GetRequiredService<IOptions<XeRateProviderOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = XeFinancialServiceBuilderExtensions.AddXeExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates(configure: o => o.ChartingRatesPath = string.Empty);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<XeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<XeRateProvider>());
    }
}
