// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the <see cref="IMonetaryContextResolver" /> that <c>AddFinancialService</c> registers, over contexts bound
/// from configuration and registered with
/// <see cref="FinancialServiceBuilderExtensions.AddMonetaryContext(IFinancialServiceBuilder, string, MonetaryContext)" />.
/// </summary>
[TestClass]
public sealed partial class MonetaryContextResolverTests
{
    /// <summary>The configuration key prefix of the default context's settings.</summary>
    private const string DefaultKey = "Financial:DefaultMonetaryContext:";

    /// <summary>The configuration key prefix of the named contexts' settings.</summary>
    private const string NamedKey = "Financial:MonetaryContexts:";

    /// <summary>
    /// Builds a configuration over the specified settings and a service provider whose financial services bind it.
    /// </summary>
    /// <param name="settings">The configuration settings, keyed by path.</param>
    /// <param name="configure">An optional callback that composes the financial builder further.</param>
    /// <param name="loggerProvider">An optional logger provider that receives the container's log entries.</param>
    /// <returns>The configuration and the built service provider.</returns>
    private static (IConfigurationRoot Configuration, ServiceProvider Provider) Build(
        Dictionary<string, string?> settings,
        Action<IFinancialServiceBuilder>? configure = null,
        ILoggerProvider? loggerProvider = null)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        if (loggerProvider is not null)
            services.AddLogging(logging => logging.AddProvider(loggerProvider));

        IFinancialServiceBuilder builder = services.AddFinancialService(configuration);
        configure?.Invoke(builder);

        return (configuration, services.BuildServiceProvider());
    }

    /// <summary>
    /// Sets a value in every provider of the configuration and reloads it, raising the change notification the options
    /// monitor follows.
    /// </summary>
    /// <param name="configuration">The configuration to change.</param>
    /// <param name="key">The configuration key to set.</param>
    /// <param name="value">The new value.</param>
    private static void SetAndReload(IConfigurationRoot configuration, string key, string? value)
    {
        foreach (IConfigurationProvider provider in configuration.Providers)
            provider.Set(key, value);

        configuration.Reload();
    }
}
