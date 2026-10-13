// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="RbaRateProvider" /> downloads, caches, and interprets RBA exchange-rate data.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract (timeout, user agent and response-size cap), the synchronous-access, look-back and refresh behavior, the
/// advertised history, the currency-alias map, and the per-concern log levels. The constructor points
/// <see cref="WebRateProviderOptions.BaseAddress" /> at the RBA's historical statistics path, identifies the requests
/// with a Bodu user agent, and maps the RBA's <c>SDR</c> units label to the ISO code <c>XDR</c>. The members declared
/// here are RBA-specific: the <see cref="Eras" /> catalogue and the on-disk cache settings, which are off by default.
/// </para>
/// <para>
/// The absolute URL of an era workbook is its <see cref="RbaEraWorkbook.FileName" /> resolved against
/// <see cref="WebRateProviderOptions.BaseAddress" />. An on-demand load reads every era the
/// <see cref="WebRateProviderOptions.DefaultLookback" /> window ending on the requested date reaches, so a date early
/// in an era also loads the era before it. The inherited <see cref="WebRateProviderOptions.RefreshInterval" /> governs
/// the open-ended current era, in memory and in the on-disk cache: a cached copy older than the interval is downloaded
/// again, and in memory the loaded current era serves for good a request that ends before the day it was downloaded, or
/// on a day it returned rates for, while a request that reaches a later day downloads the era again once the interval
/// has passed. A closed era is loaded once, and its cached copy never expires.
/// </para>
/// <para>
/// The RBA names each column's currency with a units label, so <see cref="WebRateProviderOptions.CurrencyAliases" />
/// maps a label found among its keys to the ISO 4217 code its value gives while a workbook is read. The provider
/// advertises the depth of its <see cref="Eras" /> catalogue while
/// <see cref="WebRateProviderOptions.HistoryAvailability" /> is left at
/// <see cref="RateHistoryAvailability.Unbounded" />, and any other value set there in its place. Every member carries a
/// working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c>; the dependency-injection
/// registration binds this type from configuration and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class RbaRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProviderOptions" /> class with the RBA historical statistics
    /// path as its base address, a Bodu user agent, and the <c>SDR</c> to <c>XDR</c> currency alias.
    /// </summary>
    public RbaRateProviderOptions()
    {
        BaseAddress = new Uri("https://www.rba.gov.au/statistics/tables/xls-hist/");
        UserAgent = "Bodu.Financial.ExchangeRates.Rba";
        CurrencyAliases["SDR"] = "XDR";
    }

    /// <summary>
    /// Gets or sets the catalog of era files to draw from.
    /// </summary>
    /// <value>The era catalog; defaults to <see cref="RbaEraWorkbook.Default" />.</value>
    public IReadOnlyList<RbaEraWorkbook> Eras { get; set; } = RbaEraWorkbook.Default;

    /// <summary>
    /// Gets or sets a value indicating whether downloaded era files are persisted to an on-disk cache.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to enable the on-disk cache; otherwise <see langword="false" />. Defaults to
    /// <see langword="false" />.
    /// </value>
    public bool EnableDiskCache { get; set; }

    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-rba</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Validates the RBA-specific options, ensuring the era catalogue is not empty.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every RBA-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (Eras is null || Eras.Count == 0)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsEras;
            return false;
        }

        error = null;
        return true;
    }
}
