// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.AddOverrideProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddOverrideProvider</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.AddOverrideProvider(null!, new MutableNotableDateRuleOverrideProvider());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>AddOverrideProvider</c> throws when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddOverrideProvider((INotableDateRuleOverrideProvider)null!);
        });

        Assert.AreEqual("provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an authored override addition surfaces through the resolved service when registered via
    /// <c>AddOverrideProvider</c>.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenStaticProviderSupplied_ShouldSurfaceAdditionInService()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider(new StaticOverrideProvider(
                additions: new[] { Fixed("Override Add", 7, 1) },
                removals: Array.Empty<RuleRemoval>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Override Add"));
    }

    /// <summary>
    /// Verifies that an authored override removal surfaces through the resolved service when registered via
    /// <c>AddOverrideProvider</c>.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenRemovalAuthored_ShouldSuppressBaseRule()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Suppress Me", 6, 1)))
            .AddOverrideProvider(new StaticOverrideProvider(
                additions: Array.Empty<NotableDateRule>(),
                removals: new[] { new RuleRemoval("Suppress Me") }));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Suppress Me"));
    }

    /// <summary>
    /// Verifies that successive <c>AddOverrideProvider</c> calls accumulate so that contributions compose.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenCalledMultipleTimes_ShouldAccumulateProviders()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider(new StaticOverrideProvider(new[] { Fixed("First Override", 7, 1) }, Array.Empty<RuleRemoval>()))
            .AddOverrideProvider(new StaticOverrideProvider(new[] { Fixed("Second Override", 8, 1) }, Array.Empty<RuleRemoval>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.IsTrue(emitted.Any(n => n.Name == "First Override"));
        Assert.IsTrue(emitted.Any(n => n.Name == "Second Override"));
    }

    /// <summary>
    /// Verifies that a <see cref="MutableNotableDateRuleOverrideProvider" /> registered through the builder gets its
    /// <c>Changed</c> event automatically wired to <see cref="INotableDateService.Reload" />.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenMutableProviderRegistered_ShouldAutoReloadOnChange()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider(overrides);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Live"));

        overrides.AddRule(Fixed("Live", 9, 9));

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Live"));
    }
}
