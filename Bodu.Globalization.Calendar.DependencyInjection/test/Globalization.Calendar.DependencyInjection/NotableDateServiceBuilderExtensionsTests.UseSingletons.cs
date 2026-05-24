// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.UseSingletons.cs" company="PlaceholderCompany">
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
}
