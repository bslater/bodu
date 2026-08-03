// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Configures a <see cref="CachingNotableDateService" />: how long a computed year stays fresh, the resource-version
/// token used when no resource provider is observed, the on-disk cache location, and the log levels of the per-lookup
/// hit and miss diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case.
/// </para>
/// <para>
/// Freshness has two independent triggers. <see cref="Ttl" /> expires a cached year a fixed duration after it was
/// computed, a safety net against drift. A change in the resource currently in effect changes the version token and
/// invalidates every entry computed under the previous version, so a reload always forces a recompute regardless of the
/// time-to-live.
/// </para>
/// </remarks>
public sealed class NotableDateCachingOptions
{
    /// <summary>
    /// Gets or sets the duration a computed year stays fresh after it was computed.
    /// </summary>
    /// <value>The time-to-live; defaults to 30 days.</value>
    /// <remarks>
    /// Validation requires only a strictly positive duration; there is no upper bound. Because notable-date resolution
    /// is deterministic for a given resource version, the version trigger handles data changes and this time-to-live is
    /// a coarse safety net; an extreme duration effectively disables the time-based trigger.
    /// </remarks>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the maximum fraction of <see cref="Ttl" /> that is deterministically shaved off per territory, so
    /// territories warmed together do not all expire — and recompute — at the same instant.
    /// </summary>
    /// <value>
    /// A fraction in <c>[0, 1)</c>; defaults to <c>0</c>, which disables jitter and preserves the exact configured
    /// time-to-live.
    /// </value>
    /// <remarks>
    /// The reduction is derived from a stable hash of the normalized territory (not from randomness), so a given
    /// territory's effective time-to-live is identical across instances and processes and deterministic under test. The
    /// jitter is keyed by territory rather than by territory and year so the batch and per-year read paths — which
    /// share one time-to-live per resolution — always agree on freshness. Jitter only ever shortens the duration, so no
    /// year is served longer than the configured time-to-live allows; the trade-off is that a territory may recompute
    /// up to this fraction of its time-to-live early.
    /// </remarks>
    public double TtlJitter { get; set; }

    /// <summary>
    /// Gets or sets the fraction of the effective time-to-live after which a cache hit, while still served immediately,
    /// additionally schedules a single background recompute of the served year, so a hot territory is recomputed before
    /// it expires and no caller ever absorbs the recompute latency.
    /// </summary>
    /// <value>
    /// A fraction in <c>[0, 1)</c>; defaults to <c>0</c>, which disables refresh-ahead entirely and preserves the plain
    /// serve-until-expiry behaviour.
    /// </value>
    /// <remarks>
    /// <para>
    /// Refresh-ahead is access-triggered: no timers run, and only a year actually served from the cache can schedule a
    /// recompute. When a hit's served entry is older than this fraction of the effective (post-jitter) time-to-live, at
    /// most one background recompute per territory, year, and resource version is started; concurrent aged hits for the
    /// same key join the pending recompute rather than duplicating it. Because the fraction is below <c>1</c>, the
    /// recompute always begins before the entry expires, so a continuously hot territory never surfaces a miss.
    /// </para>
    /// <para>
    /// The background recompute shares the single-flight guard with genuine misses, so a miss that arrives while a
    /// refresh is computing is served the refreshed value; such a joining caller is counted as neither a hit nor a
    /// miss. A recompute that fails is swallowed after logging — the hit it piggybacked on was already served — and the
    /// next aged hit schedules a fresh attempt. Disposing the service prevents new recomputes from being scheduled and
    /// abandons pending ones without draining them.
    /// </para>
    /// </remarks>
    public double RefreshAheadFraction { get; set; }

    /// <summary>
    /// Gets or sets the resource-version token used to key cache entries when the caching service observes no
    /// <see cref="INotableDateResourceProvider" />.
    /// </summary>
    /// <value>
    /// The fixed version token, or <see langword="null" /> to use a built-in default. Ignored when a resource provider
    /// is observed, in which case the token is derived from the resource in effect.
    /// </value>
    /// <remarks>
    /// Set this to the data version of the resource the wrapped service resolves against when that service is not
    /// reloadable, so bumping it invalidates the cache after a data update. When a reloadable resource provider is
    /// observed, the token is derived from the resource identity and reload generation instead and this value is
    /// unused.
    /// </remarks>
    public string? ResourceVersion { get; set; }

    /// <summary>
    /// Gets or sets the directory used by the default on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-notable-dates</c> folder under the system
    /// temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the level at which a year served from the cache is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel CacheHitLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the level at which a year recomputed on a cache miss and cached is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel CacheMissLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Ttl" /> is not strictly positive, or when <see cref="CacheHitLogLevel" /> or
    /// <see cref="CacheMissLogLevel" /> is not a defined <see cref="LogLevel" />.
    /// </exception>
    /// <remarks>
    /// This throwing form preserves the <c>ParamName</c> of the offending option. The dependency-injection registration
    /// instead wires <see cref="TryValidate" /> into <c>ValidateOnStart</c>.
    /// </remarks>
    public void Validate()
    {
        if (Ttl <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_TtlNotPositive, Ttl),
                nameof(Ttl));
        }

        if (TtlJitter is < 0 or >= 1 || double.IsNaN(TtlJitter))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_TtlJitterOutOfRange, TtlJitter),
                nameof(TtlJitter));
        }

        if (RefreshAheadFraction is < 0 or >= 1 || double.IsNaN(RefreshAheadFraction))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_RefreshAheadFractionOutOfRange, RefreshAheadFraction),
                nameof(RefreshAheadFraction));
        }

        if (!AreLogLevelsDefined())
            throw new ArgumentException(CalendarCachingResourceStrings.Arg_Invalid_LogLevelUndefined, nameof(CacheHitLogLevel));
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    public bool TryValidate(out string? error)
    {
        if (Ttl <= TimeSpan.Zero)
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_TtlNotPositive, Ttl);
            return false;
        }

        if (TtlJitter is < 0 or >= 1 || double.IsNaN(TtlJitter))
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_TtlJitterOutOfRange, TtlJitter);
            return false;
        }

        if (RefreshAheadFraction is < 0 or >= 1 || double.IsNaN(RefreshAheadFraction))
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_RefreshAheadFractionOutOfRange, RefreshAheadFraction);
            return false;
        }

        if (!AreLogLevelsDefined())
        {
            error = CalendarCachingResourceStrings.Arg_Invalid_LogLevelUndefined;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Reports whether every configurable log level is a defined <see cref="LogLevel" /> value.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when every configured log level is defined; otherwise <see langword="false" />.
    /// </returns>
    private bool AreLogLevelsDefined() =>
        Enum.IsDefined(CacheHitLogLevel)
        && Enum.IsDefined(CacheMissLogLevel);
}
