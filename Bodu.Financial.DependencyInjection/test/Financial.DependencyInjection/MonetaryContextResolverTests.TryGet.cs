// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.TryGet(string, out MonetaryContext)" /> returns
    /// <see langword="true" /> and the configured context for a configured name.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenNameConfigured_ShouldReturnTrueAndTheContext()
    {
        (_, ServiceProvider provider) = Build(new() { [NamedKey + "Retail:Rounding"] = "AwayFromZero" });

        bool found = provider.GetRequiredService<IMonetaryContextResolver>().TryGet("Retail", out MonetaryContext? context);

        Assert.IsTrue(found);
        Assert.IsNotNull(context);
        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, context.Rounding);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.TryGet(string, out MonetaryContext)" /> returns
    /// <see langword="true" /> and the context registered with <c>AddMonetaryContext</c> when the options configure no
    /// context of that name.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenNameOnlyRegistered_ShouldReturnTrueAndTheRegisteredContext()
    {
        MonetaryContext tax = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Unrounded };
        (_, ServiceProvider provider) = Build(new(), builder => builder.AddMonetaryContext("Tax", tax));

        bool found = provider.GetRequiredService<IMonetaryContextResolver>().TryGet("Tax", out MonetaryContext? context);

        Assert.IsTrue(found);
        Assert.AreSame(tax, context);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.TryGet(string, out MonetaryContext)" /> returns
    /// <see langword="false" /> and <see langword="null" /> for a name no context is configured or registered under.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenNameUnknown_ShouldReturnFalseAndNull()
    {
        (_, ServiceProvider provider) = Build(new());

        bool found = provider.GetRequiredService<IMonetaryContextResolver>().TryGet("Unknown", out MonetaryContext? context);

        Assert.IsFalse(found);
        Assert.IsNull(context);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.TryGet(string, out MonetaryContext)" /> returns
    /// <see langword="false" /> for a white-space name rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenNameBlank_ShouldReturnFalseAndNull()
    {
        (_, ServiceProvider provider) = Build(new());

        bool found = provider.GetRequiredService<IMonetaryContextResolver>().TryGet(" ", out MonetaryContext? context);

        Assert.IsFalse(found);
        Assert.IsNull(context);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.TryGet(string, out MonetaryContext)" /> rejects a
    /// <see langword="null" /> name with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenNameNull_ShouldThrowArgumentNullException()
    {
        IMonetaryContextResolver resolver = Build(new()).Provider.GetRequiredService<IMonetaryContextResolver>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.TryGet(null!, out _);
        });

        Assert.AreEqual("name", ex.ParamName);
    }
}
