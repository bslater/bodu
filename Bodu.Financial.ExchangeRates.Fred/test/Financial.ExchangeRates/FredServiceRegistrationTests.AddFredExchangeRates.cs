// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredServiceRegistrationTests.AddFredExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class FredServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the FRED provider.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddFredExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<FredRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddFredExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddFredExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        FredRateProvider? concrete = provider.GetService<FredRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named FRED HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddFredExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(FredFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Fred:ApiKey"] = "bound-key",
                ["Financial:Fred:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddFredExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        FredRateProviderOptions options = provider.GetRequiredService<IOptions<FredRateProviderOptions>>().Value;

        Assert.AreEqual("bound-key", options.ApiKey);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = FredFinancialServiceBuilderExtensions.AddFredExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options (a missing API key) fail fast through <c>ValidateOnStart</c> when the provider is
    /// resolved.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_WhenApiKeyMissing_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddFredExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<FredRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddFredExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddFredExchangeRates(configure: o => o.ApiKey = "test-key");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<FredRateProvider>());
    }
}
