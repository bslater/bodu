// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

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
