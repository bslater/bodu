// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a <see cref="CachingRateProvider" />: the on-disk cache location, the default time a cached rate stays
/// fresh, and per-provider overrides of that default.
/// </summary>
/// <remarks>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case.
/// </para>
/// <para>
/// The <c>Cache*LogLevel</c> members set the <see cref="LogLevel" /> at which each cache diagnostic is logged, so
/// consumers can re-tune verbosity per concern without category-wide log filters. The per-lookup hit and miss events
/// default to <see cref="LogLevel.Trace" /> because they run on the read hot path; the range events default to
/// <see cref="LogLevel.Debug" />. The per-serve <see cref="RateProvenanceLogLevel" /> records the lineage of every
/// served rate — live versus cache hit, the backend identity, and the served data's age — and also defaults to
/// <see cref="LogLevel.Debug" />. Set any member to <see cref="LogLevel.None" /> to suppress that event entirely.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new CachingRateOptions
/// {
///     CacheDirectory = "/var/cache/fx",        // null/blank -> a bodu-exchange-rates temp folder
///     DefaultExpiry = TimeSpan.FromHours(24),  // applies to any source without an override
/// };
///
/// // Override the default for specific sources, keyed by the name the source is cached under.
/// options.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);
/// options.ProviderExpiry["Yahoo"] = TimeSpan.FromHours(1);
///
/// TimeSpan rbaExpiry = options.GetExpiry("RBA");      // 7 days
/// TimeSpan ecbExpiry = options.GetExpiry("ECB");      // 24 hours (the default)
///]]>
/// </code>
/// </example>
public sealed class CachingRateOptions
{
    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-exchange-rates</c> folder under the system
    /// temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the default duration a cached rate stays fresh, applied to any provider without a specific override
    /// in <see cref="ProviderExpiry" />.
    /// </summary>
    /// <value>The default caching duration; defaults to 24 hours.</value>
    /// <remarks>
    /// Validation requires only a strictly positive duration; there is no upper bound. This property and the
    /// <see cref="ProviderExpiry" /> overrides accept an arbitrarily large value, so an extreme duration leaves cached
    /// rows fresh indefinitely and effectively disables expiry for the affected provider.
    /// </remarks>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets a value indicating whether the provider consults the inner source's advertised
    /// <see cref="RateHistoryAvailability" /> before delegating a miss, skipping or clamping fetches for dates the
    /// source has declared it cannot serve.
    /// </summary>
    /// <value><see langword="true" /> to respect the advertised history; defaults to <see langword="true" />.</value>
    /// <remarks>
    /// The clamp applies only when the inner provider implements <see cref="IHistoricalRateProvider" />; a non-aware
    /// inner is treated as unbounded and never skipped. A skipped single-date lookup surfaces as an ordinary miss, and
    /// a range window that starts before the advertised earliest date is fetched from that earliest date while the
    /// whole requested window is still recorded as covered, so the unavailable prefix is not refetched until normal
    /// expiry. Disable this to forward every request to the inner source unchanged.
    /// </remarks>
    public bool RespectHistoryAvailability { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a range lookup that misses the direct pair's coverage skips the
    /// inverse-pair probe when the direct pair holds any fresh coverage at all, probing the inverse only when the
    /// direct pair's coverage is completely empty.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to skip the inverse probe when the direct pair carries fresh coverage; defaults to
    /// <see langword="false" />, matching the historical behaviour of always probing the inverse on a direct miss.
    /// </value>
    /// <remarks>
    /// On a direct-coverage miss the provider normally issues a second backend read for the inverse pair. For a
    /// workload that only ever fetches one orientation — the common case — that probe is a guaranteed-empty read
    /// doubling backend I/O on every refetch. Enabling this option treats any fresh direct coverage as evidence the
    /// pair is actively fetched in the direct orientation and goes straight to the refetch, at the cost of a redundant
    /// refetch in the rare configuration where the inverse pair alone holds complete coverage for the window. The
    /// option has no effect when <see cref="DefaultLookupOptions" /> disallows inverse serves.
    /// </remarks>
    public bool SkipInverseRangeProbeWhenDirectCovered { get; set; }

    /// <summary>
    /// Gets the per-provider expiry overrides, keyed by the provider name supplied to the caching provider.
    /// </summary>
    /// <value>
    /// A map from provider name to the duration its cached rates stay fresh. Providers absent from the map use
    /// <see cref="DefaultExpiry" />.
    /// </value>
    public IDictionary<string, TimeSpan> ProviderExpiry { get; } = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the level at which a single-date lookup served from the cache is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel CacheHitLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the level at which a single-date cache miss resolved from a source and then cached is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel CacheMissLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the level at which a range lookup served entirely from the cache is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel CacheRangeHitLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a range lookup refetched from a source and re-cached is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel CacheRangeRefetchLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a fetch skipped or clamped because of the inner source's advertised history (see
    /// <see cref="RespectHistoryAvailability" />) is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel HistoryClampLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which the provenance of every served rate is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    /// <remarks>
    /// This per-serve diagnostic records the lineage of each result — whether it was resolved live or from the cache,
    /// the cache backend that served it, and the served data's age — and so is richer than the per-concern hit and miss
    /// events on the read hot path. Set it to <see cref="LogLevel.None" /> to suppress provenance entirely.
    /// </remarks>
    public LogLevel RateProvenanceLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the lookup options applied by the timeless <see cref="IRateProvider.GetRate(string, string)" />
    /// surface, which resolves the rate for the current UTC date.
    /// </summary>
    /// <value>The lookup options used for timeless lookups; defaults to <see cref="RateLookupOptions.Exact" />.</value>
    public RateLookupOptions DefaultLookupOptions { get; set; } = RateLookupOptions.Exact;

    /// <summary>
    /// Resolves the caching duration for a provider, returning its specific override when present and the default
    /// otherwise.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <returns>The duration cached rates for <paramref name="provider" /> stay fresh.</returns>
    public TimeSpan GetExpiry(string provider) =>
        ProviderExpiry.TryGetValue(provider, out TimeSpan expiry) ? expiry : DefaultExpiry;

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="DefaultExpiry" /> or any <see cref="ProviderExpiry" /> entry is not strictly positive,
    /// when <see cref="ProviderExpiry" /> or <see cref="DefaultLookupOptions" /> is <see langword="null" />, or when
    /// any <c>Cache*LogLevel</c>, <see cref="HistoryClampLogLevel" />, or <see cref="RateProvenanceLogLevel" /> is not
    /// a defined <see cref="LogLevel" />.
    /// </exception>
    /// <remarks>
    /// This throwing form preserves the <c>ParamName</c> of the offending option, which callers rely on. The
    /// dependency-injection registration instead wires <see cref="TryValidate" /> into <c>ValidateOnStart</c>, which
    /// reports the same invariants without throwing.
    /// </remarks>
    public void Validate()
    {
        if (DefaultExpiry <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, DefaultExpiry),
                nameof(DefaultExpiry));
        }

        if (ProviderExpiry is null)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderExpiryNull, nameof(ProviderExpiry));

        foreach (KeyValuePair<string, TimeSpan> entry in ProviderExpiry)
        {
            if (entry.Value <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, entry.Value),
                    nameof(ProviderExpiry));
            }
        }

        if (DefaultLookupOptions is null)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_LookupOptionsNull, nameof(DefaultLookupOptions));

        if (!AreLogLevelsDefined())
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_LogLevelUndefined, nameof(CacheHitLogLevel));
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The throwing <see cref="Validate" /> method is expressed in terms of this method, and the dependency-injection
    /// registration wires it into <c>ValidateOnStart</c> so misconfiguration fails fast at application startup.
    /// </remarks>
    public bool TryValidate(out string? error)
    {
        if (DefaultExpiry <= TimeSpan.Zero)
        {
            error = string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, DefaultExpiry);
            return false;
        }

        if (ProviderExpiry is null)
        {
            error = CachingResourceStrings.Arg_Invalid_ProviderExpiryNull;
            return false;
        }

        foreach (KeyValuePair<string, TimeSpan> entry in ProviderExpiry)
        {
            if (entry.Value <= TimeSpan.Zero)
            {
                error = string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, entry.Value);
                return false;
            }
        }

        if (DefaultLookupOptions is null)
        {
            error = CachingResourceStrings.Arg_Invalid_LookupOptionsNull;
            return false;
        }

        if (!AreLogLevelsDefined())
        {
            error = CachingResourceStrings.Arg_Invalid_LogLevelUndefined;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Reports whether every configurable log level is a defined <see cref="LogLevel" /> value.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when every <c>Cache*LogLevel</c> property, <see cref="HistoryClampLogLevel" />, and
    /// <see cref="RateProvenanceLogLevel" /> is defined; otherwise <see langword="false" />.
    /// </returns>
    private bool AreLogLevelsDefined() =>
        Enum.IsDefined(CacheHitLogLevel)
        && Enum.IsDefined(CacheMissLogLevel)
        && Enum.IsDefined(CacheRangeHitLogLevel)
        && Enum.IsDefined(CacheRangeRefetchLogLevel)
        && Enum.IsDefined(HistoryClampLogLevel)
        && Enum.IsDefined(RateProvenanceLogLevel);
}
