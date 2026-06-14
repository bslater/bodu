// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a <see cref="CachingDatedExchangeRateProvider" />: the on-disk cache location, the default time a cached
/// rate stays fresh, and per-provider overrides of that default.
/// </summary>
/// <remarks>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new CachingExchangeRateOptions
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
public sealed class CachingExchangeRateOptions
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
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(24);

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
    /// Thrown when <see cref="DefaultExpiry" /> or any <see cref="ProviderExpiry" /> entry is not strictly positive.
    /// </exception>
    public void Validate()
    {
        if (DefaultExpiry <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, DefaultExpiry),
                nameof(DefaultExpiry));
        }

        foreach (KeyValuePair<string, TimeSpan> entry in ProviderExpiry)
        {
            if (entry.Value <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_ExpiryNotPositive, entry.Value),
                    nameof(ProviderExpiry));
            }
        }
    }
}
