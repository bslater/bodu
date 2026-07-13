// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the read-through, freshness, and version behaviour of <see cref="CachingNotableDateService" />.
/// </summary>
[TestClass]
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>The fixed base instant tests resolve against.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds a caching service over a counting inner service and an in-memory cache, exposing all three so a test can
    /// assert cache population and inner invocation counts.
    /// </summary>
    /// <param name="timeProvider">The clock the service measures against.</param>
    /// <param name="ttl">The time-to-live to configure.</param>
    /// <param name="inner">The counting inner service.</param>
    /// <param name="cache">The in-memory cache.</param>
    /// <param name="versionSource">An optional resource provider observed for reloads.</param>
    /// <returns>The caching service.</returns>
    private static CachingNotableDateService BuildService(
        TimeProvider timeProvider,
        TimeSpan ttl,
        out CountingNotableDateService inner,
        out InMemoryNotableDateCache cache,
        INotableDateResourceProvider? versionSource = null)
    {
        inner = new CountingNotableDateService();
        cache = new InMemoryNotableDateCache();
        return new CachingNotableDateService(
            inner,
            cache,
            new NotableDateCachingOptions { Ttl = ttl, ResourceVersion = "fixed" },
            versionSource,
            timeProvider);
    }
}
