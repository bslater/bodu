// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.Reload.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that a reload of the bound configuration that changes a setting changes the context the resolver
    /// returns, without rebuilding the container.
    /// </summary>
    [TestMethod]
    public void Reload_WhenSettingChanges_ShouldServeTheRebuiltContext()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new() { [DefaultKey + "Rounding"] = "AwayFromZero" });
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        SetAndReload(configuration, DefaultKey + "Rounding", "ToZero");

        Assert.AreEqual(MidpointRounding.ToZero, ((MidpointRoundingStrategy)resolver.Default.Rounding).Mode);
    }

    /// <summary>
    /// Verifies that a reload that adds a named context makes the resolver find it.
    /// </summary>
    [TestMethod]
    public void Reload_WhenContextAdded_ShouldResolveTheNewName()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new());
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        SetAndReload(configuration, NamedKey + "Retail:Rounding", "Stochastic");

        Assert.IsTrue(resolver.TryGet("Retail", out MonetaryContext? context));
        Assert.AreSame(StochasticRoundingStrategy.Shared, context.Rounding);
    }

    /// <summary>
    /// Verifies that a reload whose settings fail validation leaves the contexts built before in effect.
    /// </summary>
    [TestMethod]
    public void Reload_WhenSettingsAreInvalid_ShouldKeepThePreviousContexts()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new()
        {
            [NamedKey + "Invoicing:ScalePolicy"] = "Custom",
            [NamedKey + "Invoicing:CustomScale"] = "4",
        });
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        SetAndReload(configuration, NamedKey + "Invoicing:CustomScale", "40");

        Assert.AreEqual(4, resolver.Get("Invoicing").CustomScale);
    }

    /// <summary>
    /// Verifies that a reload whose settings fail validation is logged as an error carrying the
    /// <see cref="OptionsValidationException" /> that names the failure.
    /// </summary>
    [TestMethod]
    public void Reload_WhenSettingsAreInvalid_ShouldLogTheFailure()
    {
        var logs = new CapturingLoggerProvider();
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new(), loggerProvider: logs);
        _ = provider.GetRequiredService<IMonetaryContextResolver>();

        SetAndReload(configuration, DefaultKey + "ScalePolicy", "Custom");

        (_, EventId eventId, _, Exception? exception) = logs.Logger.Entries.Single(entry => entry.Level == LogLevel.Error);
        Assert.AreEqual(1001, eventId.Id);
        Assert.IsInstanceOfType<OptionsValidationException>(exception);
        StringAssert.Contains(((OptionsValidationException)exception).Failures.Single(), "default monetary context");
    }

    /// <summary>
    /// Verifies that a reload that makes invalid settings valid again is applied.
    /// </summary>
    [TestMethod]
    public void Reload_WhenSettingsBecomeValidAgain_ShouldApplyThem()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new() { [DefaultKey + "ScalePolicy"] = "Unrounded" });
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();
        SetAndReload(configuration, DefaultKey + "ScalePolicy", "Custom");

        SetAndReload(configuration, DefaultKey + "CustomScale", "6");

        Assert.AreEqual(ScalePolicy.Custom, resolver.Default.ScalePolicy);
        Assert.AreEqual(6, resolver.Default.CustomScale);
    }

    /// <summary>
    /// Verifies that a reload whose value the configuration binder cannot convert, which fails in the options system
    /// before any rebuild, leaves the contexts built before in effect.
    /// </summary>
    [TestMethod]
    public void Reload_WhenValueCannotBeConverted_ShouldKeepThePreviousContexts()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new() { [DefaultKey + "Rounding"] = "AwayFromZero" });
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        _ = Assert.ThrowsExactly<AggregateException>(() =>
        {
            SetAndReload(configuration, DefaultKey + "Rounding", "Sideways");
        });

        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, resolver.Default.Rounding);
    }
}
