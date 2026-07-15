// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the dependency-injection wiring of the startup notable-date cache warm-up registration.
/// </summary>
[TestClass]
public sealed class NotableDateCacheWarmupExtensionsTests
{
    /// <summary>
    /// Verifies that the registration adds exactly one hosted warm-up service.
    /// </summary>
    [TestMethod]
    public void AddNotableDateCacheWarmup_WhenRegistered_ShouldAddOneHostedService()
    {
        var services = new ServiceCollection();
        services.AddNotableDateCacheWarmup(configure: warmup => warmup.Territories.Add("US"));
        using ServiceProvider provider = services.BuildServiceProvider();

        List<IHostedService> hosted = provider.GetServices<IHostedService>().ToList();

        Assert.HasCount(1, hosted);
        Assert.IsInstanceOfType<NotableDateCacheWarmupService>(hosted[0]);
    }

    /// <summary>
    /// Verifies that the warm-up options bind from the default configuration section.
    /// </summary>
    [TestMethod]
    public void AddNotableDateCacheWarmup_WhenConfigurationProvided_ShouldBindOptions()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:NotableDateCacheWarmup:Territories:0"] = "US",
                ["Calendar:NotableDateCacheWarmup:YearsAhead"] = "3",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddNotableDateCacheWarmup(config);
        using ServiceProvider provider = services.BuildServiceProvider();

        NotableDateCacheWarmupOptions options = provider.GetRequiredService<IOptions<NotableDateCacheWarmupOptions>>().Value;

        Assert.HasCount(1, options.Territories);
        Assert.AreEqual("US", options.Territories[0]);
        Assert.AreEqual(3, options.YearsAhead);
    }

    /// <summary>
    /// Verifies that invalid warm-up options fail fast when the hosted service is resolved.
    /// </summary>
    [TestMethod]
    public void AddNotableDateCacheWarmup_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        var services = new ServiceCollection();
        services.AddNotableDateCacheWarmup();
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IHostedService>();
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service collection is rejected.
    /// </summary>
    [TestMethod]
    public void AddNotableDateCacheWarmup_WhenServicesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateCacheWarmupExtensions.AddNotableDateCacheWarmup(null!);
        });

        Assert.AreEqual("services", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank section name is rejected.
    /// </summary>
    [TestMethod]
    public void AddNotableDateCacheWarmup_WhenSectionNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddNotableDateCacheWarmup(sectionName: "  ");
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }
}
