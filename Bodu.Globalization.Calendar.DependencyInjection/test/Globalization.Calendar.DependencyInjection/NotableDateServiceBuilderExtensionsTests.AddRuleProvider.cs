// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.AddRuleProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that the instance overload of <c>AddRuleProvider</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.AddRuleProvider(null!, new InMemoryRuleProvider());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the instance overload of <c>AddRuleProvider</c> throws when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddRuleProvider((INotableDateRuleProvider)null!);
        });

        Assert.AreEqual("provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the factory overload of <c>AddRuleProvider</c> throws when the factory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenFactoryIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddRuleProvider((Func<IServiceProvider, INotableDateRuleProvider>)null!);
        });

        Assert.AreEqual("factory", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the factory overload of <c>AddRuleProvider</c> resolves the provider lazily from the container.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenFactorySupplied_ShouldResolveProviderFromContainer()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddRuleProvider(_ => new InMemoryRuleProvider(Fixed("Lazy", 7, 1)));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.Contains(n => n.Name == "Lazy", service.GetNotableDates(2026));
    }

    /// <summary>
    /// Verifies that the typed overload of <c>AddRuleProvider</c> resolves the provider via the container.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenTypedRegistrationUsed_ShouldResolveProviderViaContainer()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddRuleProvider<EmptyRuleProvider>();

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        // The resolved service composes the empty provider — query must succeed and return no rules.
        Assert.IsEmpty(service.GetNotableDates(2026));
    }

    /// <summary>
    /// Verifies that <c>AddRuleProviders</c> accepts a sequence and registers every element.
    /// </summary>
    [TestMethod]
    public void AddRuleProviders_WhenSequenceSupplied_ShouldRegisterEveryProvider()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddRuleProviders(
        [
            new InMemoryRuleProvider(Fixed("From A", 1, 1)),
            new InMemoryRuleProvider(Fixed("From B", 2, 1)),
            new InMemoryRuleProvider(Fixed("From C", 3, 1)),
        ]);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.Contains(n => n.Name == "From A", emitted);
        Assert.Contains(n => n.Name == "From B", emitted);
        Assert.Contains(n => n.Name == "From C", emitted);
    }

    /// <summary>
    /// Verifies that <c>AddRuleProviders</c> throws when the providers sequence is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddRuleProviders_WhenSequenceIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddRuleProviders(null!);
        });

        Assert.AreEqual("providers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>AddRuleProviders</c> throws when the sequence contains a <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void AddRuleProviders_WhenSequenceContainsNullElement_ShouldThrowArgumentException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddRuleProviders(
            [
                new InMemoryRuleProvider(Fixed("X")),
                null!,
            ]);
        });

        Assert.AreEqual("providers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that successive <c>AddRuleProvider</c> calls accumulate rather than overwrite.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenCalledMultipleTimes_ShouldAccumulateProviders()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddRuleProvider(new InMemoryRuleProvider(Fixed("First", 1, 1)));
        builder.AddRuleProvider(new InMemoryRuleProvider(Fixed("Second", 2, 1)));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.Contains(n => n.Name == "First", emitted);
        Assert.Contains(n => n.Name == "Second", emitted);
    }

    /// <summary>
    /// Verifies that fluent extension calls return the same builder instance so calls can be chained.
    /// </summary>
    [TestMethod]
    public void AddRuleProvider_WhenCalled_ShouldReturnSameBuilderInstance()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        INotableDateServiceBuilder returned = builder.AddRuleProvider(new InMemoryRuleProvider());

        Assert.AreSame(builder, returned);
    }

    /// <summary>
    /// Verifies that <c>AddRuleProviders</c> with an empty sequence is a no-op — the resolved service still functions
    /// and returns no notable dates.
    /// </summary>
    [TestMethod]
    public void AddRuleProviders_WhenSequenceIsEmpty_ShouldResolveServiceWithNoRules()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddRuleProviders([]);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.IsEmpty(service.GetNotableDates(2026));
    }

    /// <summary>
    /// A degenerate <see cref="INotableDateRuleProvider" /> used by <c>AddRuleProvider&lt;TProvider&gt;</c> tests.
    /// </summary>
    private sealed class EmptyRuleProvider : INotableDateRuleProvider
    {
        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => [];
    }
}
