// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaServiceRegistrationTests.AddRbaExchangeRates.cs" company="Bodu Pty. Ltd.">
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
    public void AddRbaExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddRbaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<RbaRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddRbaExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaRateProvider? concrete = provider.GetService<RbaRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named RBA HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(RbaFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that the RBA-specific options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Rba:EnableDiskCache"] = "true",
                ["Financial:Rba:CacheDirectory"] = "rba-cache",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddRbaExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaRateProviderOptions options = provider.GetRequiredService<IOptions<RbaRateProviderOptions>>().Value;

        Assert.IsTrue(options.EnableDiskCache);
        Assert.AreEqual("rba-cache", options.CacheDirectory);
    }

    /// <summary>
    /// Verifies that the shared <see cref="WebRateProviderOptions" /> keys - <c>BaseAddress</c>, <c>HttpTimeout</c>,
    /// <c>UserAgent</c>, <c>MaxResponseContentBufferSize</c>, <c>DefaultLookback</c>, <c>RefreshInterval</c>,
    /// <c>SynchronousNetworkFetchLogLevel</c> and <c>CurrencyAliases</c> - bind directly under the provider's section.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenSharedKeysConfigured_ShouldBindTheInheritedMembers()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Rba:BaseAddress"] = "https://mirror.example/fx/",
                ["Financial:Rba:HttpTimeout"] = "00:00:12",
                ["Financial:Rba:UserAgent"] = "test-agent",
                ["Financial:Rba:MaxResponseContentBufferSize"] = "1048576",
                ["Financial:Rba:DefaultLookback"] = "5.00:00:00",
                ["Financial:Rba:RefreshInterval"] = "06:00:00",
                ["Financial:Rba:SynchronousNetworkFetchLogLevel"] = "Information",
                ["Financial:Rba:CurrencyAliases:CNH"] = "CNY",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddRbaExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<RbaRateProviderOptions>>().Value);

        Assert.AreEqual(new Uri("https://mirror.example/fx/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(12), options.HttpTimeout);
        Assert.AreEqual("test-agent", options.UserAgent);
        Assert.AreEqual(1_048_576L, options.MaxResponseContentBufferSize);
        Assert.AreEqual(TimeSpan.FromDays(5), options.DefaultLookback);
        Assert.AreEqual(TimeSpan.FromHours(6), options.RefreshInterval);
        Assert.AreEqual(Microsoft.Extensions.Logging.LogLevel.Information, options.SynchronousNetworkFetchLogLevel);
        Assert.AreEqual("CNY", options.CurrencyAliases["CNH"]);
    }

    /// <summary>
    /// Verifies that the retired keys - <c>BaseUrl</c> and <c>CurrentEraRefreshInterval</c> - no longer bind, so the shared members keep their defaults.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenRetiredKeysConfigured_ShouldNotBindThem()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Rba:BaseUrl"] = "https://mirror.example/fx/",
                ["Financial:Rba:CurrentEraRefreshInterval"] = "06:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddRbaExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<RbaRateProviderOptions>>().Value);

        Assert.AreEqual(new Uri("https://www.rba.gov.au/statistics/tables/xls-hist/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Rba", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        RbaRateProviderOptions options = provider.GetRequiredService<IOptions<RbaRateProviderOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RbaFinancialServiceBuilderExtensions.AddRbaExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates(configure: o => o.Eras = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<RbaRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that the <see cref="OptionsValidationException" /> raised for invalid options carries the
    /// package's resource-backed validation message.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenOptionsInvalid_ShouldThrowWithResourceMessage()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates(configure: o => o.Eras = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<RbaRateProvider>();
        });

        Assert.AreEqual(RbaResourceStrings.ResourceManager.GetString("Op_Invalid_RbaOptions"), ex.Message);
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddRbaExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddRbaExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<RbaRateProvider>());
    }
}
