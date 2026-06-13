// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Configures how the <see cref="RbaExchangeRateProvider" /> downloads, caches, and interprets RBA exchange-rate data.
/// </summary>
/// <remarks>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </remarks>
public sealed class RbaExchangeRateOptions
{
    /// <summary>
    /// Gets or sets the base URL under which the RBA publishes its historical exchange-rate files. Should end with a
    /// trailing slash so era file names resolve as relative paths.
    /// </summary>
    /// <value>The base URL; defaults to the RBA historical statistics path.</value>
    public Uri BaseUrl { get; set; } = new("https://www.rba.gov.au/statistics/tables/xls-hist/");

    /// <summary>
    /// Gets or sets the catalogue of era files to draw from.
    /// </summary>
    /// <value>The era catalogue; defaults to <see cref="RbaEra.Default" />.</value>
    public IReadOnlyList<RbaEra> Eras { get; set; } = RbaEra.Default;

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to era downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to era downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The user-agent string; defaults to a Bodu identifier.</value>
    public string UserAgent { get; set; } = "Bodu.Financial.ExchangeRates.Rba";

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to download a missing era on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking downloads from <see cref="IDatedExchangeRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms). In those environments, set this to
    /// <see langword="false" /> and warm the cache with <see cref="RbaExchangeRateProvider.PreloadAsync" /> or
    /// <see cref="RbaExchangeRateProvider.LoadRangeAsync" /> at startup.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether downloaded era files are persisted to an on-disk cache.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to enable the on-disk cache; otherwise <see langword="false" />. Defaults to
    /// <see langword="true" />.
    /// </value>
    public bool EnableDiskCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-rba</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets how long a cached copy of the open-ended current era remains fresh before it is re-downloaded.
    /// </summary>
    /// <value>The refresh interval; defaults to 12 hours. Immutable historical eras are cached indefinitely.</value>
    public TimeSpan CurrentEraRefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the map from RBA units labels to ISO 4217 codes, applied while resolving a column's currency.
    /// </summary>
    /// <value>The alias map; defaults to mapping <c>SDR</c> to the ISO code <c>XDR</c>.</value>
    public IDictionary<string, string> CurrencyAliases { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["SDR"] = "XDR" };

    /// <summary>
    /// Validates the options, throwing when a required value is missing.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="BaseUrl" /> is <see langword="null" />, or <see cref="Eras" /> is <see langword="null" />
    /// or empty.
    /// </exception>
    public void Validate()
    {
        if (BaseUrl is null)
            throw new ArgumentException(RbaResourceStrings.Arg_Invalid_RbaOptionsBaseUrl, nameof(BaseUrl));

        if (Eras is null || Eras.Count == 0)
            throw new ArgumentException(RbaResourceStrings.Arg_Invalid_RbaOptionsEras, nameof(Eras));
    }
}
