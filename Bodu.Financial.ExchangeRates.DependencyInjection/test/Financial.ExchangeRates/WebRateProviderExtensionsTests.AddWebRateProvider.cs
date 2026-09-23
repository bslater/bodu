// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.AddWebRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderExtensionsTests
{
    /// <summary>
    /// Verifies that registering a web rate provider resolves the concrete provider type as a singleton.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddWebRateProvider_WhenRegistered_ShouldResolveProviderAsSingleton()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();

        var first = provider.GetRequiredService<TestRateProvider>();
        var second = provider.GetRequiredService<TestRateProvider>();

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that the provider is also reachable through <see cref="IDatedRateProvider" /> and
    /// <see cref="IRateProvider" />, and that all three resolve the same instance rather than separate ones.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenRegistered_ShouldExposeSameInstanceThroughBothProviderContracts()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();

        var concrete = provider.GetRequiredService<TestRateProvider>();
        var dated = provider.GetRequiredService<IDatedRateProvider>();
        var timeless = provider.GetRequiredService<IRateProvider>();

        Assert.AreSame(concrete, dated);
        Assert.AreSame(concrete, timeless);
    }

    /// <summary>
    /// Verifies that registering the same provider twice does not produce a duplicate registration, because the
    /// machinery registers through <c>TryAddSingleton</c>.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenRegisteredTwice_ShouldRegisterProviderOnce()
    {
        ServiceCollection services = CreateServices();
        services
            .AddFinancialService()
            .AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
                TestHttpClientName,
                null,
                TestSectionName,
                TestValidationMessage,
                DefaultConfigure,
                null,
                static (client, options, loggerFactory, timeProvider) =>
                    new TestRateProvider(client, options, loggerFactory, timeProvider));

        int registrations = services.Count(d => d.ServiceType == typeof(TestRateProvider));

        Assert.AreEqual(1, registrations);
    }

    /// <summary>
    /// Verifies that the registration returns the same builder instance it was given, so provider registrations chain.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenRegistered_ShouldReturnSameBuilderForChaining()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddFinancialService();

        IFinancialServiceBuilder returned = builder.AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
            TestHttpClientName,
            null,
            TestSectionName,
            TestValidationMessage,
            DefaultConfigure,
            null,
            static (client, options, loggerFactory, timeProvider) =>
                new TestRateProvider(client, options, loggerFactory, timeProvider));

        Assert.AreSame(builder, returned);
    }

    /// <summary>
    /// Verifies that the factory delegate receives the resolved options instance, so a provider observes the same
    /// configuration the container validated.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenResolved_ShouldPassConfiguredOptionsToFactory()
    {
        var expected = new Uri("https://configured.example.test/");
        using ServiceProvider provider = CreateServices(configure: o => o.BaseAddress = expected)
            .BuildServiceProvider();

        var instance = provider.GetRequiredService<TestRateProvider>();

        Assert.AreEqual(expected, instance.Options.BaseAddress);
    }

    /// <summary>
    /// Verifies that the factory delegate receives the ambient <see cref="ILoggerFactory" /> when logging is registered.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenLoggingRegistered_ShouldPassLoggerFactoryToFactory()
    {
        ServiceCollection services = CreateServices();
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();
        var instance = provider.GetRequiredService<TestRateProvider>();

        Assert.IsNotNull(instance.LoggerFactory);
    }

    /// <summary>
    /// Verifies that the factory delegate receives a <see langword="null" /> <see cref="TimeProvider" /> when none is
    /// registered, since the registration resolves it optionally rather than requiring it.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenNoTimeProviderRegistered_ShouldPassNullTimeProviderToFactory()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();

        var instance = provider.GetRequiredService<TestRateProvider>();

        Assert.IsNull(instance.SuppliedTimeProvider);
    }

    /// <summary>
    /// Verifies that the factory delegate receives the ambient <see cref="TimeProvider" /> when one is registered.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenTimeProviderRegistered_ShouldPassItToFactory()
    {
        ServiceCollection services = CreateServices();
        services.AddSingleton(TimeProvider.System);

        using ServiceProvider provider = services.BuildServiceProvider();
        var instance = provider.GetRequiredService<TestRateProvider>();

        Assert.AreSame(TimeProvider.System, instance.SuppliedTimeProvider);
    }
}
