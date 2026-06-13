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
}
