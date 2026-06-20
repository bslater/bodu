// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxServiceRegistrationTests.AddOfxExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Ofx.DependencyInjection;

public partial class OfxServiceRegistrationTests
{
    /// <summary>
    /// Verifies that the provider is registered once and exposed through both provider interfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddOfxExchangeRates_ShouldRegisterProviderAsSingleton()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddOfxExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        OfxExchangeRateProvider? concrete = provider.GetService<OfxExchangeRateProvider>();
        IDatedExchangeRateProvider? dated = provider.GetService<IDatedExchangeRateProvider>();
        IExchangeRateProvider? simple = provider.GetService<IExchangeRateProvider>();

        Assert.IsNotNull(concrete);
        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, simple);
    }

    /// <summary>
    /// Verifies that the named OFX HTTP client is configured and resolvable.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_ShouldRegisterNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddOfxExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory? factory = provider.GetService<IHttpClientFactory>();

        Assert.IsNotNull(factory);
        using HttpClient client = factory.CreateClient(OfxFinancialServiceBuilderExtensions.HttpClientName);
        Assert.IsNotNull(client);
    }

    /// <summary>
    /// Verifies that options bind from a configuration section.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_ShouldBindOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:Ofx:ReportingInterval"] = "weekly",
                ["Financial:Ofx:DefaultLookback"] = "14.00:00:00",
            })
            .Build();

        ServiceCollection services = new();
        services.AddBoduFinancial(configuration).AddOfxExchangeRates(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OfxExchangeRateOptions options = provider.GetRequiredService<IOptions<OfxExchangeRateOptions>>().Value;

        Assert.AreEqual("weekly", options.ReportingInterval);
        Assert.AreEqual(TimeSpan.FromDays(14), options.DefaultLookback);
    }

    /// <summary>
    /// Verifies that the configure callback is applied to the bound options.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_ShouldApplyConfigureCallback()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddOfxExchangeRates(configure: o => o.UserAgent = "test-agent");
        using ServiceProvider provider = services.BuildServiceProvider();

        OfxExchangeRateOptions options = provider.GetRequiredService<IOptions<OfxExchangeRateOptions>>().Value;

        Assert.AreEqual("test-agent", options.UserAgent);
    }

    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = OfxFinancialServiceBuilderExtensions.AddOfxExchangeRates(null!);
        });
    }

    /// <summary>
    /// Verifies that invalid options fail fast through <c>ValidateOnStart</c> when the provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddOfxExchangeRates(configure: o => o.ReportingInterval = string.Empty);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<OfxExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the provider.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddOfxExchangeRates();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<OfxExchangeRateProvider>());
    }
}
