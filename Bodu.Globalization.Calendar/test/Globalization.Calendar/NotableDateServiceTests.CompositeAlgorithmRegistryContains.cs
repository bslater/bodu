// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.CompositeAlgorithmRegistryContains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="NotableDateService" />'s private <c>CompositeAlgorithmRegistry.Contains</c> implementation
/// directly. The method is not invoked from any production path (the rule resolver uses <c>TryGet</c>) so the only
/// way to exercise it is reflection-based instantiation of the private nested type.
/// </summary>
/// <remarks>
/// <para>
/// The composite is a layered <see cref="INotableDateAlgorithmRegistry" /> that consults the host-supplied registry
/// first and falls back to the plugin-supplied registry. The three branches under test are: primary contains the key
/// (short-circuit true), primary misses but fallback contains the key (true), and neither layer contains the key
/// (false).
/// </para>
/// </remarks>
[TestClass]
public sealed class NotableDateServiceCompositeAlgorithmRegistryContainsTests
{
    /// <summary>
    /// Verifies that <c>Contains</c> returns <see langword="true" /> when the primary (host) registry contains the
    /// supplied key — the short-circuit branch of the <c>||</c> expression.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPrimaryContainsKey_ShouldReturnTrue()
    {
        INotableDateAlgorithmRegistry primary = new NotableDateAlgorithmRegistry()
            .Register("primary.only", new StubAlgorithm());
        INotableDateAlgorithmRegistry fallback = new NotableDateAlgorithmRegistry();

        INotableDateAlgorithmRegistry composite = BuildComposite(primary, fallback);

        Assert.IsTrue(composite.Contains("primary.only"));
    }

    /// <summary>
    /// Verifies that <c>Contains</c> returns <see langword="true" /> when the primary registry misses but the
    /// fallback (plugin) registry contains the supplied key.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPrimaryMissesButFallbackContainsKey_ShouldReturnTrue()
    {
        INotableDateAlgorithmRegistry primary = new NotableDateAlgorithmRegistry();
        INotableDateAlgorithmRegistry fallback = new NotableDateAlgorithmRegistry()
            .Register("plugin.only", new StubAlgorithm());

        INotableDateAlgorithmRegistry composite = BuildComposite(primary, fallback);

        Assert.IsTrue(composite.Contains("plugin.only"));
    }

    /// <summary>
    /// Verifies that <c>Contains</c> returns <see langword="false" /> when neither the primary nor the fallback
    /// registry contains the supplied key — the double-miss branch.
    /// </summary>
    [TestMethod]
    public void Contains_WhenNeitherLayerContainsKey_ShouldReturnFalse()
    {
        INotableDateAlgorithmRegistry primary = new NotableDateAlgorithmRegistry();
        INotableDateAlgorithmRegistry fallback = new NotableDateAlgorithmRegistry();

        INotableDateAlgorithmRegistry composite = BuildComposite(primary, fallback);

        Assert.IsFalse(composite.Contains("nowhere"));
    }

    /// <summary>
    /// Constructs the private nested <c>CompositeAlgorithmRegistry</c> on <see cref="NotableDateService" /> via
    /// reflection. The class is private but its surface (<see cref="INotableDateAlgorithmRegistry" />) is public.
    /// </summary>
    /// <param name="primary">The host-supplied registry, consulted first.</param>
    /// <param name="fallback">The plugin-supplied registry, consulted on primary misses.</param>
    /// <returns>The constructed composite registry.</returns>
    private static INotableDateAlgorithmRegistry BuildComposite(
        INotableDateAlgorithmRegistry primary,
        INotableDateAlgorithmRegistry fallback)
    {
        Type compositeType = typeof(NotableDateService)
            .GetNestedType("CompositeAlgorithmRegistry", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CompositeAlgorithmRegistry nested type was not found on NotableDateService.");

        var instance = Activator.CreateInstance(
            compositeType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: new object[] { primary, fallback },
            culture: null)
            ?? throw new InvalidOperationException("Failed to instantiate CompositeAlgorithmRegistry.");

        return (INotableDateAlgorithmRegistry)instance;
    }

    private sealed class StubAlgorithm
        : INotableDateAlgorithm
    {
        public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
            new(year, 1, 1);
    }
}
