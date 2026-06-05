// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.UseSingletons.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>UseAlgorithmRegistry</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseAlgorithmRegistry_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.UseAlgorithmRegistry(null!, new NotableDateAlgorithmRegistry());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseAlgorithmRegistry</c> throws when the registry is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseAlgorithmRegistry_WhenRegistryIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.UseAlgorithmRegistry(null!);
        });

        Assert.AreEqual("registry", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseAlgorithmRegistry</c> registers the supplied registry so it is resolvable from the
    /// container.
    /// </summary>
    [TestMethod]
    public void UseAlgorithmRegistry_WhenInvoked_ShouldRegisterRegistryAsResolvableSingleton()
    {
        NotableDateAlgorithmRegistry registry = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.UseAlgorithmRegistry(registry);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(registry, provider.GetRequiredService<INotableDateAlgorithmRegistry>());
    }

    /// <summary>
    /// Verifies that successive <c>UseAlgorithmRegistry</c> calls replace rather than accumulate.
    /// </summary>
    [TestMethod]
    public void UseAlgorithmRegistry_WhenCalledTwice_ShouldReplaceFirstRegistration()
    {
        NotableDateAlgorithmRegistry first = new();
        NotableDateAlgorithmRegistry second = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .UseAlgorithmRegistry(first)
            .UseAlgorithmRegistry(second);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(second, provider.GetRequiredService<INotableDateAlgorithmRegistry>());
    }

    /// <summary>
    /// Verifies that <c>UseCollisionResolver</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseCollisionResolver_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.UseCollisionResolver(null!, new DefaultNotableDateCollisionResolver());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseCollisionResolver</c> throws when the resolver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseCollisionResolver_WhenResolverIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.UseCollisionResolver(null!);
        });

        Assert.AreEqual("resolver", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseCollisionResolver</c> registers the supplied instance and that successive calls replace.
    /// </summary>
    [TestMethod]
    public void UseCollisionResolver_WhenCalledTwice_ShouldReplaceFirstRegistration()
    {
        DefaultNotableDateCollisionResolver first = new();
        DefaultNotableDateCollisionResolver second = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.UseCollisionResolver(first).UseCollisionResolver(second);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(second, provider.GetRequiredService<INotableDateCollisionResolver>());
    }

    /// <summary>
    /// Verifies that <c>UseNameLocalizer</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseNameLocalizer_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.UseNameLocalizer(null!, new StubNameLocalizer());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseNameLocalizer</c> throws when the localizer is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseNameLocalizer_WhenLocalizerIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.UseNameLocalizer(null!);
        });

        Assert.AreEqual("localizer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseNameLocalizer</c> registers the supplied instance and that successive calls replace.
    /// </summary>
    [TestMethod]
    public void UseNameLocalizer_WhenCalledTwice_ShouldReplaceFirstRegistration()
    {
        StubNameLocalizer first = new();
        StubNameLocalizer second = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.UseNameLocalizer(first).UseNameLocalizer(second);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(second, provider.GetRequiredService<INotableDateNameLocalizer>());
    }

    /// <summary>
    /// Verifies that <c>UseAdjustmentHandlers</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseAdjustmentHandlers_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.UseAdjustmentHandlers(null!, new AdjustmentHandlerRegistry());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseAdjustmentHandlers</c> throws when the registry is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseAdjustmentHandlers_WhenRegistryIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.UseAdjustmentHandlers(null!);
        });

        Assert.AreEqual("registry", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseAdjustmentHandlers</c> registers the supplied registry and that successive calls replace.
    /// </summary>
    [TestMethod]
    public void UseAdjustmentHandlers_WhenCalledTwice_ShouldReplaceFirstRegistration()
    {
        AdjustmentHandlerRegistry first = new();
        AdjustmentHandlerRegistry second = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.UseAdjustmentHandlers(first).UseAdjustmentHandlers(second);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(second, provider.GetRequiredService<IAdjustmentHandlerRegistry>());
    }

    /// <summary>
    /// Verifies that <c>UseResourcePathResolver</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseResourcePathResolver_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.UseResourcePathResolver(null!, new ResourcePathResolver());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseResourcePathResolver</c> throws when the resolver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UseResourcePathResolver_WhenResolverIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.UseResourcePathResolver(null!);
        });

        Assert.AreEqual("resolver", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>UseResourcePathResolver</c> registers the supplied resolver and that successive calls replace.
    /// </summary>
    [TestMethod]
    public void UseResourcePathResolver_WhenCalledTwice_ShouldReplaceFirstRegistration()
    {
        ResourcePathResolver first = new();
        ResourcePathResolver second = new();

        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.UseResourcePathResolver(first).UseResourcePathResolver(second);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.AreSame(second, provider.GetRequiredService<IResourcePathResolver>());
    }

    /// <summary>
    /// A trivial <see cref="INotableDateNameLocalizer" /> used only as an identity sentinel for the registration
    /// tests above.
    /// </summary>
    private sealed class StubNameLocalizer : INotableDateNameLocalizer
    {
        /// <inheritdoc />
        public string GetDisplayName(NotableDate notableDate, System.Globalization.CultureInfo? culture = null) =>
            notableDate?.Name ?? string.Empty;
    }
}
