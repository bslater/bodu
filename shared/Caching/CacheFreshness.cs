// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CacheFreshness.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Caching;

/// <summary>
/// Provides the shared freshness convention every Bodu read-through cache applies: a value stamped with a caching
/// instant remains fresh for a caching duration, and a value stamped implausibly far in the future of the evaluation
/// instant is rejected rather than trusted.
/// </summary>
/// <remarks>
/// <para>
/// Freshness is a strict less-than comparison — a value exactly one duration old is stale — so every backend that
/// evaluates freshness through this helper agrees to the boundary. The clock-skew tolerance lets a value stamped
/// marginally ahead of the evaluating clock survive validation, which matters when a writer and a reader observe
/// slightly different clocks.
/// </para>
/// <para>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs
/// it (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal static class CacheFreshness
{
    /// <summary>
    /// The clock-skew tolerance applied when validating a caching instant: a value stamped more than this far in the
    /// future of the evaluation instant is treated as invalid rather than fresh.
    /// </summary>
    public static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reports whether a value stamped at <paramref name="stampedAtUtc" /> is still fresh at <paramref name="asOf" />
    /// under the supplied caching duration.
    /// </summary>
    /// <param name="stampedAtUtc">The instant the value was cached or fetched.</param>
    /// <param name="duration">The duration the value remains fresh after it was stamped.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="asOf" /> is within <paramref name="duration" /> of
    /// <paramref name="stampedAtUtc" />; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsFresh(DateTimeOffset stampedAtUtc, TimeSpan duration, DateTimeOffset asOf) =>
        asOf - stampedAtUtc < duration;

    /// <summary>
    /// Reports whether a value stamped at <paramref name="stampedAtUtc" /> is not implausibly far in the future of
    /// <paramref name="asOf" />, allowing the shared clock-skew tolerance.
    /// </summary>
    /// <param name="stampedAtUtc">The caching instant to validate.</param>
    /// <param name="asOf">The instant against which the caching instant is checked for implausible future stamps.</param>
    /// <returns>
    /// <see langword="false" /> when <paramref name="stampedAtUtc" /> is more than the clock-skew tolerance ahead of
    /// <paramref name="asOf" />; otherwise <see langword="true" />.
    /// </returns>
    public static bool IsWithinClockSkew(DateTimeOffset stampedAtUtc, DateTimeOffset asOf) =>
        stampedAtUtc <= asOf + ClockSkewTolerance;
}
