// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceCollectionExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.V2;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.V2.DependencyInjection;

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

        Assert.AreEqual(1, holidays.Count);
        Assert.AreEqual("new-years-day", holidays[0].NotableDateId);
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

        Assert.AreEqual(1, service.Resolve(new DateOnly(2026, 1, 1), "XX").Count);
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
}
