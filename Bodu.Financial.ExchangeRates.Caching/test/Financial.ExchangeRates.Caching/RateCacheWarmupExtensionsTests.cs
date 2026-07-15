// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the dependency-injection wiring of the startup rate-cache warm-up registration.
/// </summary>
[TestClass]
public sealed class RateCacheWarmupExtensionsTests
{
    /// <summary>
    /// Verifies that the registration adds exactly one hosted warm-up service.
    /// </summary>
    [TestMethod]
    public void AddRateCacheWarmup_WhenRegistered_ShouldAddOneHostedService()
    {
        var services = new ServiceCollection();
        services.AddFinancialService().AddRateCacheWarmup(configure: warmup => warmup.Pairs.Add("AUD/USD"));
        using ServiceProvider provider = services.BuildServiceProvider();

        List<IHostedService> hosted = provider.GetServices<IHostedService>().ToList();

        Assert.HasCount(1, hosted);
        Assert.IsInstanceOfType<RateCacheWarmupService>(hosted[0]);
    }

    /// <summary>
    /// Verifies that the warm-up options bind from the default configuration section.
    /// </summary>
    [TestMethod]
    public void AddRateCacheWarmup_WhenConfigurationProvided_ShouldBindOptions()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:RateCacheWarmup:Pairs:0"] = "AUD/USD",
                ["Financial:RateCacheWarmup:LookbackDays"] = "7",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddFinancialService().AddRateCacheWarmup(config);
        using ServiceProvider provider = services.BuildServiceProvider();

        RateCacheWarmupOptions options = provider.GetRequiredService<IOptions<RateCacheWarmupOptions>>().Value;

        Assert.HasCount(1, options.Pairs);
        Assert.AreEqual("AUD/USD", options.Pairs[0]);
        Assert.AreEqual(7, options.LookbackDays);
    }

    /// <summary>
    /// Verifies that invalid warm-up options fail fast when the hosted service is resolved.
    /// </summary>
    [TestMethod]
    public void AddRateCacheWarmup_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        var services = new ServiceCollection();
        services.AddFinancialService().AddRateCacheWarmup();
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IHostedService>();
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected.
    /// </summary>
    [TestMethod]
    public void AddRateCacheWarmup_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RateCacheWarmupExtensions.AddRateCacheWarmup(null!);
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank section name is rejected.
    /// </summary>
    [TestMethod]
    public void AddRateCacheWarmup_WhenSectionNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddFinancialService();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddRateCacheWarmup(sectionName: "  ");
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }
}
