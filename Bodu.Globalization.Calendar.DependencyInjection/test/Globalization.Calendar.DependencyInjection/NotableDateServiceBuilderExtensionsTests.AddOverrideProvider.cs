// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.AddOverrideProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

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
                additions: [Fixed("Override Add", 7, 1)],
                removals: []));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.Contains(n => n.Name == "Override Add", service.GetNotableDates(2026));
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
                additions: [],
                removals: [new RuleRemoval("Suppress Me")]));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.DoesNotContain(n => n.Name == "Suppress Me", service.GetNotableDates(2026));
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
            .AddOverrideProvider(new StaticOverrideProvider([Fixed("First Override", 7, 1)], []))
            .AddOverrideProvider(new StaticOverrideProvider([Fixed("Second Override", 8, 1)], []));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.Contains(n => n.Name == "First Override", emitted);
        Assert.Contains(n => n.Name == "Second Override", emitted);
    }

    /// <summary>
    /// Verifies that the typed <c>AddOverrideProvider&lt;TOverride&gt;()</c> overload throws when the builder is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddOverrideProviderTyped_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.AddOverrideProvider<EmptyOverrideProvider>(null!);
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the typed <c>AddOverrideProvider&lt;TOverride&gt;()</c> overload registers the provider as a
    /// singleton resolvable through the container, and that additions surface via the resolved service.
    /// </summary>
    [TestMethod]
    public void AddOverrideProviderTyped_WhenInvoked_ShouldRegisterProviderResolvedFromContainer()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider<EmptyOverrideProvider>();

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        // The override provider contributes nothing; the base rule should still resolve unaffected.
        Assert.Contains(n => n.Name == "Base", service.GetNotableDates(2026));
    }

    /// <summary>
    /// Verifies that two <see cref="MutableNotableDateRuleOverrideProvider" /> instances registered via the builder
    /// are both auto-wired to <see cref="INotableDateService.Reload" />.
    /// </summary>
    [TestMethod]
    public void AddOverrideProvider_WhenMultipleMutableProvidersRegistered_ShouldAutoReloadOnEachProvidersChange()
    {
        MutableNotableDateRuleOverrideProvider a = new();
        MutableNotableDateRuleOverrideProvider b = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .AddRuleProvider(new InMemoryRuleProvider(Fixed("Base", 6, 1)))
            .AddOverrideProvider(a)
            .AddOverrideProvider(b);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        a.AddRule(Fixed("From A", 7, 1));
        b.AddRule(Fixed("From B", 8, 1));

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.Contains(n => n.Name == "From A", emitted);
        Assert.Contains(n => n.Name == "From B", emitted);
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

        Assert.DoesNotContain(n => n.Name == "Live", service.GetNotableDates(2026));

        overrides.AddRule(Fixed("Live", 9, 9));

        Assert.Contains(n => n.Name == "Live", service.GetNotableDates(2026));
    }

    /// <summary>
    /// A degenerate <see cref="INotableDateRuleOverrideProvider" /> used by the typed-registration test above. The
    /// container must be able to instantiate it via its parameterless constructor.
    /// </summary>
    private sealed class EmptyOverrideProvider : INotableDateRuleOverrideProvider
    {
        /// <inheritdoc />
        public IEnumerable<NotableDateRule> GetAdditions() => [];

        /// <inheritdoc />
        public IEnumerable<RuleRemoval> GetRemovals() => [];
    }
}
