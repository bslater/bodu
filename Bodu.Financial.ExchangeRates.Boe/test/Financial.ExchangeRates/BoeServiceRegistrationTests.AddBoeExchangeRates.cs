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
    /// Verifies that the Bank of England-specific options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Boe:EnableDiskCache"] = "false",
                ["Financial:Boe:CacheDirectory"] = "boe-cache",
                ["Financial:Boe:QueryPath"] = "_iadb-mirror.asp",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddBoeExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeRateProviderOptions options = provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual("boe-cache", options.CacheDirectory);
        Assert.AreEqual("_iadb-mirror.asp", options.QueryPath);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates(configure: o => o.UserAgent = "callback-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        BoeRateProviderOptions options = provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value;

        Assert.AreEqual("callback-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that the shared <see cref="WebRateProviderOptions" /> keys - <c>BaseAddress</c>, <c>HttpTimeout</c>,
    /// <c>UserAgent</c>, <c>MaxResponseContentBufferSize</c>, <c>DefaultLookback</c>, <c>RefreshInterval</c>,
    /// <c>SynchronousNetworkFetchLogLevel</c> and <c>CurrencyAliases</c> - bind directly under the provider's section.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenSharedKeysConfigured_ShouldBindTheInheritedMembers()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Boe:BaseAddress"] = "https://mirror.example/iadb/",
                ["Financial:Boe:HttpTimeout"] = "00:00:12",
                ["Financial:Boe:UserAgent"] = "test-agent",
                ["Financial:Boe:MaxResponseContentBufferSize"] = "1048576",
                ["Financial:Boe:DefaultLookback"] = "5.00:00:00",
                ["Financial:Boe:RefreshInterval"] = "06:00:00",
                ["Financial:Boe:SynchronousNetworkFetchLogLevel"] = "Information",
                ["Financial:Boe:CurrencyAliases:DOLLAR"] = "USD",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddBoeExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value);

        Assert.AreEqual(new Uri("https://mirror.example/iadb/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(12), options.HttpTimeout);
        Assert.AreEqual("test-agent", options.UserAgent);
        Assert.AreEqual(1_048_576L, options.MaxResponseContentBufferSize);
        Assert.AreEqual(TimeSpan.FromDays(5), options.DefaultLookback);
        Assert.AreEqual(TimeSpan.FromHours(6), options.RefreshInterval);
        Assert.AreEqual(Microsoft.Extensions.Logging.LogLevel.Information, options.SynchronousNetworkFetchLogLevel);
        Assert.AreEqual("USD", options.CurrencyAliases["DOLLAR"]);
    }

    /// <summary>
    /// Verifies that the retired keys - the nested <c>Endpoint</c> section and <c>OnDemandWindowDays</c> - no longer
    /// bind, so the shared members keep their defaults.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenRetiredKeysConfigured_ShouldNotBindThem()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Boe:Endpoint:BaseUrl"] = "https://mirror.example/db/",
                ["Financial:Boe:Endpoint:HttpTimeout"] = "00:00:12",
                ["Financial:Boe:Endpoint:UserAgent"] = "test-agent",
                ["Financial:Boe:OnDemandWindowDays"] = "5",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddBoeExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<BoeRateProviderOptions>>().Value);

        Assert.AreEqual(new Uri("https://www.bankofengland.co.uk/boeapps/database/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Boe", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromDays(10), options.DefaultLookback);
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
    /// Verifies that the <see cref="OptionsValidationException" /> raised for invalid options carries the
    /// package's resource-backed validation message.
    /// </summary>
    [TestMethod]
    public void AddBoeExchangeRates_WhenOptionsInvalid_ShouldThrowWithResourceMessage()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeExchangeRates(configure: o => o.Series = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<BoeRateProvider>();
        });

        Assert.AreEqual(BoeResourceStrings.ResourceManager.GetString("Op_Invalid_BoeOptions"), ex.Message);
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
