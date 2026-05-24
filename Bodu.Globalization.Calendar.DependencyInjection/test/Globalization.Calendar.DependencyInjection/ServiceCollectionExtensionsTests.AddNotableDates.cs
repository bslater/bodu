// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.AddNotableDates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.DependencyInjection;

public partial class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, IConfiguration?, string)" />
    /// throws when the service collection is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenServicesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ServiceCollectionExtensions.AddNotableDates((IServiceCollection)null!);
        });

        Assert.AreEqual("services", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the builder-callback overload throws when the callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenConfigureCallbackIsNull_ShouldThrowArgumentNullException()
    {
        IServiceCollection services = new ServiceCollection();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddNotableDates((Action<INotableDateServiceBuilder>)null!);
        });

        Assert.AreEqual("configure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the configuration overload throws when the section name is empty.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenSectionNameIsEmpty_ShouldThrowArgumentException()
    {
        IServiceCollection services = new ServiceCollection();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddNotableDates(configuration: null, sectionName: string.Empty);
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering with no rule providers resolves an <see cref="INotableDateService" /> backed by an
    /// empty effective rule set.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenNoRuleProvidersRegistered_ShouldResolveServiceWithEmptyRuleSet()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates();

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsNotNull(service);
        Assert.AreEqual(0, service.GetNotableDates(2026).Count);
    }

    /// <summary>
    /// Verifies that the resolved <see cref="INotableDateService" /> is registered as a singleton — the same instance
    /// is returned on subsequent resolutions.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenResolvedTwice_ShouldReturnSameSingletonInstance()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates();

        using ServiceProvider provider = services.BuildServiceProvider();

        INotableDateService first = provider.GetRequiredService<INotableDateService>();
        INotableDateService second = provider.GetRequiredService<INotableDateService>();

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that repeated <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, IConfiguration?, string)" />
    /// calls do not register the service factory more than once.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenInvokedRepeatedly_ShouldRegisterServiceFactoryIdempotently()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates();
        services.AddNotableDates();
        services.AddNotableDates();

        int factoryCount = services.Count(d => d.ServiceType == typeof(INotableDateService));
        Assert.AreEqual(1, factoryCount);
    }

    /// <summary>
    /// Verifies that the builder-callback overload returns the same builder instance the callback received.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenConfigureCallbackSupplied_ShouldReturnSameBuilderInstance()
    {
        IServiceCollection services = new ServiceCollection();
        INotableDateServiceBuilder? captured = null;

        INotableDateServiceBuilder returned = services.AddNotableDates(builder =>
        {
            captured = builder;
        });

        Assert.AreSame(captured, returned);
    }

    /// <summary>
    /// Verifies that providers registered via <c>AddRuleProvider</c> are surfaced in the resolved service's queries.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenRuleProviderRegistered_ShouldSurfaceRuleProviderInService()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Custom Day", 6, 1)));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Custom Day"));
    }

    /// <summary>
    /// Verifies that the working week selected via <see cref="NotableDateOptions.WorkingDays" /> drives the resolved
    /// service's classification of weekend days.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenWorkingDaysConfigured_ShouldRespectThemAsWorkingWeek()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .UseWorkingDays(WorkingDaysOfWeek.MondayToSaturday);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday.ToWeekPattern(), service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOptions" /> populated from an <see cref="IConfiguration" /> section is
    /// honoured by the resolved service.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenConfigurationBindsWorkingDays_ShouldHonourBoundValue()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:WorkingDays"] = "MondayToSaturday",
                ["NotableDates:DefaultTerritoryCode"] = "AU-NSW",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();
        NotableDateOptions opts = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday.ToWeekPattern(), service.WorkingWeek);
        Assert.AreEqual("AU-NSW", opts.DefaultTerritoryCode);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOptions.RegisterAsAmbientDefault" /> wires the resolved singleton into
    /// <see cref="NotableDateContext.Default" /> on first resolution.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenRegisterAsAmbientDefaultSet_ShouldAssignNotableDateContextDefaultOnFirstResolve()
    {
        NotableDateContext.Reset();
        try
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNotableDates()
                .RegisterAsAmbientDefault();

            using ServiceProvider provider = services.BuildServiceProvider();
            INotableDateService service = provider.GetRequiredService<INotableDateService>();

            Assert.AreSame(service, NotableDateContext.Default);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOptions.RegisterAsAmbientDefault" /> defaults to <see langword="false" />
    /// so that downstream tests are not affected by accidental ambient assignment.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenRegisterAsAmbientDefaultUnset_ShouldNotAssignNotableDateContextDefault()
    {
        NotableDateContext.Reset();
        try
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNotableDates();

            using ServiceProvider provider = services.BuildServiceProvider();
            INotableDateService service = provider.GetRequiredService<INotableDateService>();

            // The lazy fallback is a fresh instance distinct from the DI-resolved service.
            Assert.AreNotSame(service, NotableDateContext.Default);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that the configuration overload throws when the section name is whitespace.
    /// </summary>
    /// <param name="sectionName">The section name argument under test.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("   ")]
    public void AddNotableDates_WhenSectionNameIsBlank_ShouldThrowArgumentException(string sectionName)
    {
        IServiceCollection services = new ServiceCollection();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddNotableDates(configuration: null, sectionName: sectionName);
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOptions.WorkingDays" /> bound from configuration drives the resolved
    /// service's <see cref="INotableDateService.WorkingWeek" /> for every named preset.
    /// </summary>
    /// <param name="workingDays">The enum member to bind and verify.</param>
    [TestMethod]
    [DataRow(nameof(WorkingDaysOfWeek.MondayToFriday))]
    [DataRow(nameof(WorkingDaysOfWeek.MondayToSaturday))]
    [DataRow(nameof(WorkingDaysOfWeek.SundayToThursday))]
    [DataRow(nameof(WorkingDaysOfWeek.SaturdayToWednesday))]
    public void AddNotableDates_WhenConfigurationBindsKnownWorkingDaysValue_ShouldHonourIt(string workingDays)
    {
        WorkingDaysOfWeek expected = Enum.Parse<WorkingDaysOfWeek>(workingDays);

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:WorkingDays"] = workingDays,
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.AreEqual(expected.ToWeekPattern(), service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that the resolved <see cref="NotableDateService" /> is disposed when the owning
    /// <see cref="ServiceProvider" /> is disposed — the container honours <see cref="IDisposable" /> on registered
    /// singletons.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenServiceProviderDisposed_ShouldDisposeResolvedSingleton()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates();

        ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        provider.Dispose();

        // After disposal a subsequent call into the service that touches its internal ThreadLocal must throw, proving
        // the container called Dispose on the singleton (which disposes the ThreadLocal field).
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = service.GetNotableDates(2026);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider" /> registered through the builder is
    /// auto-wired to <see cref="INotableDateService.Reload" /> on its <c>Changed</c> event.
    /// </summary>
    [TestMethod]
    public void AddNotableDates_WhenMutableOverrideRegistered_ShouldAutoReloadServiceOnChange()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider(overrides);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        // No mutations yet — the override rule must not appear.
        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Auto"));

        overrides.AddRule(Fixed("Auto", 7, 1));

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Auto"));
    }
}
