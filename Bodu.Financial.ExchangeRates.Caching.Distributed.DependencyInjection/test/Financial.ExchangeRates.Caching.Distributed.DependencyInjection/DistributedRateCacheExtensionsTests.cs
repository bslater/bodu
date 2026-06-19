// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection;

/// <summary>
/// Verifies the dependency-injection wiring of the distributed (Redis-capable) exchange-rate cache.
/// </summary>
[TestClass]
public sealed partial class DistributedRateCacheExtensionsTests
{
    /// <summary>
    /// Builds a service provider after applying the supplied registration against a fresh service collection.
    /// </summary>
    /// <param name="register">The registration callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        return services.BuildServiceProvider();
    }
}
