// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceCollectionExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Verifies that the dependency-injection registration helpers register a resolvable, singleton
/// <see cref="INotableDateService" /> over a loaded resource.
/// </summary>
[TestClass]
public sealed class NotableDateServiceCollectionExtensionsTests
{
    /// <summary>
    /// A minimal notable-date document used by the registration tests.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that the resource overload registers a service that resolves the document.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithResource_RegistersResolvableService()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();
        IReadOnlyList<NotableDate> holidays = service.Resolve(new DateOnly(2026, 1, 1), "XX");

        CollectionAssert.AreEqual(
            new[] { ("new-years-day", new DateOnly(2026, 1, 1)) },
            holidays.Select(r => (r.NotableDateId, r.Date)).ToArray());
    }

    /// <summary>
    /// Verifies that the factory overload defers resource construction to resolution time.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithFactory_RegistersResolvableService()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(_ => NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.HasCount(1, service.Resolve(new DateOnly(2026, 1, 1), "XX"));
    }

    /// <summary>
    /// Verifies that the registration is a singleton, returning the same instance on repeated resolution.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_RegistersSingleton()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService first = provider.GetRequiredService<INotableDateService>();
        INotableDateService second = provider.GetRequiredService<INotableDateService>();

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that a null resource is rejected.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WhenResourceIsNull_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ServiceCollection().AddNotableDateService((NotableDateResource)null!);
        });
    }

    /// <summary>
    /// Verifies that the reloadable registration resolves the initially-loaded resource before any reload, emitting the
    /// 1 January occurrence from the January document.
    /// </summary>
    [TestMethod]
    public void AddReloadableNotableDateService_BeforeReload_ResolvesInitialResource()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddReloadableNotableDateService(NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.AreEqual(new DateOnly(2026, 1, 1), service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// Verifies that, after a runtime reload performed through the registered provider, the resolved service reflects the
    /// new resource, emitting the 1 February occurrence from the reloaded document.
    /// </summary>
    [TestMethod]
    public void AddReloadableNotableDateService_AfterProviderReload_ReflectsNewResource()
    {
        const string februaryXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="February" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        ServiceProvider provider = new ServiceCollection()
            .AddReloadableNotableDateService(NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();
        provider.GetRequiredService<MutableNotableDateResourceProvider>().Reload(NotableDateResourceLoader.Load(februaryXml));

        Assert.AreEqual(new DateOnly(2026, 2, 1), service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// A minimal document whose single rule dispatches to a custom algorithm key, used to prove options wiring.
    /// </summary>
    private const string AlgorithmXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di.algorithm">
      <NotableDates>
        <NotableDate id="custom-day" displayName="Custom Day" category="Observance">
          <Rules><Rule id="x"><Strategy><Algorithm key="di-test-day" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a registry mapping the test algorithm key to a fixed 15 July date.
    /// </summary>
    /// <returns>The populated registry.</returns>
    private static Algorithms.NotableDateAlgorithmRegistry TestRegistry() =>
        new Algorithms.NotableDateAlgorithmRegistry()
            .Register("di-test-day", new FixedJulyAlgorithm());

    /// <summary>A test algorithm resolving 15 July of the requested year.</summary>
    private sealed class FixedJulyAlgorithm : Algorithms.INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 7, 15);
    }

    /// <summary>
    /// Verifies that the options overload composes the supplied algorithm registry into the registered service, so a
    /// document rule dispatching to a custom key resolves.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithOptions_ComposesAlgorithmRegistry()
    {
        Algorithms.NotableDateAlgorithmRegistry registry = TestRegistry();
        NotableDateResource resource = NotableDateResourceLoader.Load(AlgorithmXml, _ => null, registry);

        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(resource, new NotableDateServiceOptions { Algorithms = registry })
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.AreEqual(
            new DateOnly(2026, 7, 15),
            service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// Verifies that the factory-plus-options-factory overload composes both factories' products into the registered
    /// service.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithFactoryAndOptionsFactory_ComposesAlgorithmRegistry()
    {
        Algorithms.NotableDateAlgorithmRegistry registry = TestRegistry();

        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(
                _ => NotableDateResourceLoader.Load(AlgorithmXml, _ => null, registry),
                _ => new NotableDateServiceOptions { Algorithms = registry })
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.HasCount(1, service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX"));
    }

    /// <summary>
    /// Verifies that keyed registrations register independent services resolvable by their keys, leaving no unkeyed
    /// registration behind.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithServiceKeys_RegistersIndependentKeyedServices()
    {
        const string secondXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di.second">
          <NotableDates>
            <NotableDate id="second-day" displayName="Second Day" category="Observance">
              <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService("first", NotableDateResourceLoader.Load(Xml))
            .AddNotableDateService("second", NotableDateResourceLoader.Load(secondXml))
            .BuildServiceProvider();

        INotableDateService first = provider.GetRequiredKeyedService<INotableDateService>("first");
        INotableDateService second = provider.GetRequiredKeyedService<INotableDateService>("second");

        Assert.AreEqual(new DateOnly(2026, 1, 1), first.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
        Assert.AreEqual(new DateOnly(2026, 3, 1), second.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
        Assert.IsNull(provider.GetService<INotableDateService>());
    }

    /// <summary>
    /// Verifies that the keyed factory overload defers resource construction and resolves by key.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WithServiceKeyAndFactory_RegistersResolvableKeyedService()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService("keyed", _ => NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredKeyedService<INotableDateService>("keyed");

        Assert.HasCount(1, service.Resolve(new DateOnly(2026, 1, 1), "XX"));
    }

    /// <summary>
    /// Verifies that registration is idempotent: a second registration for the same (unkeyed) service leaves the first
    /// in place rather than stacking a replacement.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WhenRegisteredTwice_KeepsFirstRegistration()
    {
        const string secondXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di.second">
          <NotableDates>
            <NotableDate id="second-day" displayName="Second Day" category="Observance">
              <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        ServiceProvider provider = new ServiceCollection()
            .AddNotableDateService(NotableDateResourceLoader.Load(Xml))
            .AddNotableDateService(NotableDateResourceLoader.Load(secondXml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.AreEqual(
            new DateOnly(2026, 1, 1),
            service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// Verifies that the reloadable factory overload defers initial-resource construction and still reflects a later
    /// reload.
    /// </summary>
    [TestMethod]
    public void AddReloadableNotableDateService_WithFactory_ResolvesAndReflectsReload()
    {
        const string februaryXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.di">
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="February" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        ServiceProvider provider = new ServiceCollection()
            .AddReloadableNotableDateService(_ => NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        INotableDateService service = provider.GetRequiredService<INotableDateService>();
        Assert.AreEqual(new DateOnly(2026, 1, 1), service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);

        provider.GetRequiredService<MutableNotableDateResourceProvider>().Reload(NotableDateResourceLoader.Load(februaryXml));
        Assert.AreEqual(new DateOnly(2026, 2, 1), service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// Verifies that the reloadable registration exposes the same provider instance under both the concrete type and
    /// <see cref="INotableDateResourceProvider" />.
    /// </summary>
    [TestMethod]
    public void AddReloadableNotableDateService_RegistersProviderUnderBothContracts()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddReloadableNotableDateService(NotableDateResourceLoader.Load(Xml))
            .BuildServiceProvider();

        Assert.AreSame(
            provider.GetRequiredService<MutableNotableDateResourceProvider>(),
            provider.GetRequiredService<INotableDateResourceProvider>());
    }

    /// <summary>
    /// Verifies that every registration overload returns the same service collection, preserving the documented
    /// chaining contract.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_Always_ReturnsSameServiceCollection()
    {
        ServiceCollection services = new();
        NotableDateResource resource = NotableDateResourceLoader.Load(Xml);

        Assert.AreSame(services, services.AddNotableDateService(resource));
        Assert.AreSame(services, services.AddNotableDateService(_ => resource));
        Assert.AreSame(services, services.AddNotableDateService("key", resource));
        Assert.AreSame(services, services.AddReloadableNotableDateService(resource));
    }

    /// <summary>
    /// Verifies that a null factory, key, or services argument is rejected by the new overloads.
    /// </summary>
    [TestMethod]
    public void AddNotableDateService_WhenNewOverloadArgumentsAreNull_Throws()
    {
        ServiceCollection services = new();
        NotableDateResource resource = NotableDateResourceLoader.Load(Xml);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddNotableDateService((Func<IServiceProvider, NotableDateResource>)null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddNotableDateService((string)null!, resource);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddNotableDateService("key", (NotableDateResource)null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddReloadableNotableDateService((Func<IServiceProvider, NotableDateResource>)null!);
        });
    }
}
