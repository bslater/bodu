// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbServiceRegistrationTests.AddEcbExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the one-call entry point registers the core currency lookup alongside the ECB provider.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_ShouldRegisterCoreServicesAndProvider()
    {
        ServiceCollection services = new();
        services.AddEcbExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ICurrencyLookup>());
        Assert.IsNotNull(provider.GetService<EcbRateProvider>());
    }

    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddEcbExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbRateProvider? concrete = provider.GetService<EcbRateProvider>();
        IDatedRateProvider? dated = provider.GetService<IDatedRateProvider>();
        IRateProvider? simple = provider.GetService<IRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named ECB HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(EcbFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that the ECB-specific options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Ecb:EnableDiskCache"] = "false",
                ["Financial:Ecb:CacheDirectory"] = "ecb-cache",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddEcbExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbRateProviderOptions options = provider.GetRequiredService<IOptions<EcbRateProviderOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual("ecb-cache", options.CacheDirectory);
    }

    /// <summary>
    /// Verifies that the shared <see cref="WebRateProviderOptions" /> keys - <c>BaseAddress</c>, <c>HttpTimeout</c>,
    /// <c>UserAgent</c>, <c>MaxResponseContentBufferSize</c>, <c>DefaultLookback</c>, <c>RefreshInterval</c>,
    /// <c>SynchronousNetworkFetchLogLevel</c> and <c>CurrencyAliases</c> - bind directly under the provider's section.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenSharedKeysConfigured_ShouldBindTheInheritedMembers()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Ecb:BaseAddress"] = "https://mirror.example/fx/",
                ["Financial:Ecb:HttpTimeout"] = "00:00:12",
                ["Financial:Ecb:UserAgent"] = "test-agent",
                ["Financial:Ecb:MaxResponseContentBufferSize"] = "1048576",
                ["Financial:Ecb:DefaultLookback"] = "5.00:00:00",
                ["Financial:Ecb:RefreshInterval"] = "06:00:00",
                ["Financial:Ecb:SynchronousNetworkFetchLogLevel"] = "Information",
                ["Financial:Ecb:CurrencyAliases:CNH"] = "CNY",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddEcbExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<EcbRateProviderOptions>>().Value);

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
    /// Verifies that the retired keys - the nested <c>Endpoint</c> section - no longer bind, so the shared members keep their defaults.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenRetiredKeysConfigured_ShouldNotBindThem()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Ecb:Endpoint:BaseUrl"] = "https://mirror.example/fx/",
                ["Financial:Ecb:Endpoint:HttpTimeout"] = "00:00:12",
                ["Financial:Ecb:Endpoint:UserAgent"] = "test-agent",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddEcbExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        WebRateProviderOptions options = SharedOptions.Of(provider.GetRequiredService<IOptions<EcbRateProviderOptions>>().Value);

        Assert.AreEqual(new Uri("https://www.ecb.europa.eu/stats/eurofxref/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Ecb", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates(configure: o => o.UserAgent = "callback-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbRateProviderOptions options = provider.GetRequiredService<IOptions<EcbRateProviderOptions>>().Value;

        Assert.AreEqual("callback-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbFinancialServiceBuilderExtensions.AddEcbExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates(configure: o => o.Feeds = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<EcbRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that the <see cref="OptionsValidationException" /> raised for invalid options carries the
    /// package's resource-backed validation message.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenOptionsInvalid_ShouldThrowWithResourceMessage()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates(configure: o => o.Feeds = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<EcbRateProvider>();
        });

        Assert.AreEqual(EcbResourceStrings.ResourceManager.GetString("Op_Invalid_EcbOptions"), ex.Message);
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddEcbExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<EcbRateProvider>());
    }
}
