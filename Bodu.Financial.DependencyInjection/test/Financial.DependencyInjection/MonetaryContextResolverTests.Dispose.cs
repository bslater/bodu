// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that a disposed resolver stops following options changes and keeps serving the contexts it built last.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenDisposed_ShouldStopFollowingChanges()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new() { [DefaultKey + "Rounding"] = "AwayFromZero" });
        var resolver = new MonetaryContextResolver(provider.GetRequiredService<IOptionsMonitor<FinancialOptions>>(), provider, logger: null);

        resolver.Dispose();
        SetAndReload(configuration, DefaultKey + "Rounding", "ToZero");

        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, resolver.Default.Rounding);
    }

    /// <summary>
    /// Verifies that disposing the container disposes the registered resolver, so it stops following options changes.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenContainerDisposed_ShouldStopFollowingChanges()
    {
        (IConfigurationRoot configuration, ServiceProvider provider) = Build(new() { [DefaultKey + "Rounding"] = "AwayFromZero" });
        IMonetaryContextResolver resolver = provider.GetRequiredService<IMonetaryContextResolver>();

        provider.Dispose();
        SetAndReload(configuration, DefaultKey + "Rounding", "ToZero");

        Assert.AreSame(MidpointRoundingStrategy.AwayFromZero, resolver.Default.Rounding);
    }
}
