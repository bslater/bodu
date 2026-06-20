// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbServiceRegistrationTests.AddEcbReferenceRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;

public partial class EcbServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddEcbReferenceRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbExchangeRateProvider? concrete = provider.GetService<EcbExchangeRateProvider>();
        IDatedExchangeRateProvider? dated = provider.GetService<IDatedExchangeRateProvider>();
        IExchangeRateProvider? simple = provider.GetService<IExchangeRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named ECB HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(EcbFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options, including the nested endpoint settings, bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Ecb:EnableDiskCache"] = "false",
                ["Financial:Ecb:RefreshInterval"] = "06:00:00",
                ["Financial:Ecb:Endpoint:BaseUrl"] = "https://mirror.example/fx/",
                ["Financial:Ecb:Endpoint:UserAgent"] = "test-agent",
            })
            .Build();

        ServiceCollection services = new();
        services.AddFinancialService(configuration).AddEcbReferenceRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbExchangeRateOptions options = provider.GetRequiredService<IOptions<EcbExchangeRateOptions>>().Value;

        Assert.IsFalse(options.EnableDiskCache);
        Assert.AreEqual(TimeSpan.FromHours(6), options.RefreshInterval);
        Assert.AreEqual(new Uri("https://mirror.example/fx/"), options.Endpoint.BaseUrl);
        Assert.AreEqual("test-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbReferenceRates(configure: o => o.Endpoint.UserAgent = "callback-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        EcbExchangeRateOptions options = provider.GetRequiredService<IOptions<EcbExchangeRateOptions>>().Value;

        Assert.AreEqual("callback-agent", options.Endpoint.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbFinancialServiceBuilderExtensions.AddEcbReferenceRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbReferenceRates(configure: o => o.Feeds = []);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<EcbExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddEcbReferenceRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddEcbReferenceRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<EcbExchangeRateProvider>());
    }
}
