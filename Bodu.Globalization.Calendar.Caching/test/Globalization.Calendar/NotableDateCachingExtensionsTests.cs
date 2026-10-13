// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="NotableDateCachingExtensions.AddCachedNotableDateService(IServiceCollection, Action{NotableDateCachingOptions}, Func{IServiceProvider, INotableDateCache})" />
/// registration decorates the notable-date service.
/// </summary>
[TestClass]
public sealed class NotableDateCachingExtensionsTests
{
    /// <summary>
    /// Verifies that the registration replaces the resolved <see cref="INotableDateService" /> with a caching decorator.
    /// </summary>
    [TestMethod]
    public void AddCachedNotableDateService_WhenServiceRegistered_ShouldResolveCachingDecorator()
    {
        var services = new ServiceCollection();
        services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
        services.AddCachedNotableDateService(cacheFactory: _ => new InMemoryNotableDateCache());

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService resolved = provider.GetRequiredService<INotableDateService>();

        Assert.IsInstanceOfType<CachingNotableDateService>(resolved);
    }

    /// <summary>
    /// Verifies that the caching decorator resolves the same occurrences the wrapped service would.
    /// </summary>
    [TestMethod]
    public void AddCachedNotableDateService_WhenResolvingHoliday_ShouldReturnOccurrences()
    {
        var services = new ServiceCollection();
        services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
        services.AddCachedNotableDateService(cacheFactory: _ => new InMemoryNotableDateCache());

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> newYear = service.Resolve(new DateOnly(2026, 1, 1), "US");
        IReadOnlyList<NotableDate> cached = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.IsTrue(newYear.Count > 0);
        Assert.AreEqual(newYear.Count, cached.Count);
    }

    /// <summary>
    /// Verifies that invalid caching options fail validation with the package's resource message.
    /// </summary>
    [TestMethod]
    public void AddCachedNotableDateService_WhenOptionsInvalid_ShouldFailWithResourceMessage()
    {
        var services = new ServiceCollection();
        services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
        services.AddCachedNotableDateService(configure: o => o.Ttl = TimeSpan.Zero, cacheFactory: _ => new InMemoryNotableDateCache());
        using ServiceProvider provider = services.BuildServiceProvider();
        string? expected = CalendarCachingResourceStrings.ResourceManager.GetString("Op_Invalid_CachingOptions", CultureInfo.CurrentCulture);

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<NotableDateCachingOptions>>().Value;
        });

        Assert.AreEqual(expected, ex.Failures.Single());
    }

    /// <summary>
    /// Verifies that registering the caching decorator without a notable-date service throws.
    /// </summary>
    [TestMethod]
    public void AddCachedNotableDateService_WhenNoServiceRegistered_ShouldThrowInvalidOperationException()
    {
        var services = new ServiceCollection();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = services.AddCachedNotableDateService();
        });
    }
}
