// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.Get.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> returns the context that the bound settings
    /// of the named context describe.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameConfigured_ShouldReturnTheConfiguredContext()
    {
        (_, ServiceProvider provider) = Build(new()
        {
            [NamedKey + "Invoicing:ScalePolicy"] = "Custom",
            [NamedKey + "Invoicing:CustomScale"] = "4",
        });

        MonetaryContext context = provider.GetRequiredService<IMonetaryContextResolver>().Get("Invoicing");

        Assert.AreEqual(ScalePolicy.Custom, context.ScalePolicy);
        Assert.AreEqual(4, context.CustomScale);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> compares a configured name case-insensitively,
    /// as configuration keys compare.
    /// </summary>
    [TestMethod]
    public void Get_WhenConfiguredNameDiffersInCase_ShouldReturnTheConfiguredContext()
    {
        (_, ServiceProvider provider) = Build(new() { [NamedKey + "Invoicing:Rounding"] = "ToZero" });

        MonetaryContext context = provider.GetRequiredService<IMonetaryContextResolver>().Get("INVOICING");

        Assert.AreEqual(MidpointRounding.ToZero, ((MidpointRoundingStrategy)context.Rounding).Mode);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> returns the context registered with
    /// <c>AddMonetaryContext</c> when the options configure no context of that name.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameOnlyRegistered_ShouldReturnTheRegisteredContext()
    {
        MonetaryContext tax = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };
        (_, ServiceProvider provider) = Build(new(), builder => builder.AddMonetaryContext("Tax", tax));

        MonetaryContext context = provider.GetRequiredService<IMonetaryContextResolver>().Get("Tax");

        Assert.AreSame(tax, context);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> prefers the configured context to the one
    /// registered under the same name, while the keyed service still resolves the registered one.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameConfiguredAndRegistered_ShouldPreferTheConfiguredContext()
    {
        MonetaryContext registered = MonetaryContext.Default with { CashRounding = CashRoundingPolicy.CurrencyCashIncrement };
        (_, ServiceProvider provider) = Build(
            new() { [NamedKey + "Tax:Rounding"] = "AwayFromZero" },
            builder => builder.AddMonetaryContext("Tax", registered));

        MonetaryContext resolved = provider.GetRequiredService<IMonetaryContextResolver>().Get("Tax");

        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, resolved.Rounding);
        Assert.AreEqual(CashRoundingPolicy.None, resolved.CashRounding);
        Assert.AreSame(registered, provider.GetRequiredKeyedService<MonetaryContext>("Tax"));
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> compares a registered name ordinally, as
    /// keyed services compare, so a name in another case is not found.
    /// </summary>
    [TestMethod]
    public void Get_WhenRegisteredNameDiffersInCase_ShouldThrowKeyNotFoundException()
    {
        (_, ServiceProvider provider) = Build(new(), builder => builder.AddMonetaryContext("Tax", MonetaryContext.Default));
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = resolver.Get("TAX");
        });
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> throws <see cref="KeyNotFoundException" />
    /// naming the name when no context is configured or registered under it.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameUnknown_ShouldThrowKeyNotFoundExceptionNamingIt()
    {
        IMonetaryContextResolver resolver = Build(new()).Provider.GetRequiredService<IMonetaryContextResolver>();

        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = resolver.Get("Unknown");
        });

        Assert.IsTrue(ex.Message.Contains("'Unknown'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> rejects a <see langword="null" /> name with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameNull_ShouldThrowArgumentNullException()
    {
        IMonetaryContextResolver resolver = Build(new()).Provider.GetRequiredService<IMonetaryContextResolver>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Get(null!);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="IMonetaryContextResolver.Get(string)" /> rejects a white-space name with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Get_WhenNameBlank_ShouldThrowArgumentException()
    {
        IMonetaryContextResolver resolver = Build(new()).Provider.GetRequiredService<IMonetaryContextResolver>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = resolver.Get("  ");
        });

        Assert.AreEqual("name", ex.ParamName);
    }
}
