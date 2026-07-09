// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="XeExchangeRateProvider" /> addresses and interprets the XE.com charting-rates JSON
/// service, and how it acquires the authorization token that endpoint requires.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebExchangeRateProviderOptions" />, which supplies the endpoint base address, the
/// HTTP contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log
/// levels. The members declared here are XE-specific: the <see cref="ChartingRatesPath" /> queried for a pair, and the
/// two URLs (<see cref="AuthBootstrapUrl" /> and <see cref="AuthScriptBaseUrl" />) used to discover the
/// <c>Authorization: Basic</c> token by scanning the script chunks the XE website publishes. XE serves a
/// server-determined window per request rather than honouring an explicit date range, so the response is range-filtered
/// to the requested dates.
/// </para>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class XeExchangeRateOptions
    : WebExchangeRateProviderOptions
{
    /// <summary>The estimated depth, in days, of the server-determined window the XE.com charting endpoint returns — approximately ten years, matching the deepest range the XE currency charts expose.</summary>
    internal const int EstimatedChartingWindowDays = 3650;

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateOptions" /> class with the XE.com host as its base
    /// address and an estimated ten-year rolling history window.
    /// </summary>
    /// <remarks>
    /// The charting endpoint accepts no date bounds — XE returns a server-determined window — so the advertised
    /// <see cref="WebExchangeRateProviderOptions.HistoryAvailability" /> is an estimate of that window (<see cref="EstimatedChartingWindowDays" />
    /// days, matching the deepest range the XE currency charts expose) rather than a published contract. Adjust the
    /// property if the observed window differs.
    /// </remarks>
    public XeExchangeRateOptions()
    {
        BaseAddress = new Uri("https://www.xe.com/");
        HistoryAvailability = ExchangeRateHistoryAvailability.RollingDays(EstimatedChartingWindowDays);
    }

    /// <summary>
    /// Gets or sets the relative charting-rates endpoint path. The source-currency and destination-currency codes are
    /// supplied as the <c>fromCurrency</c> and <c>toCurrency</c> query parameters when the request is issued.
    /// </summary>
    /// <value>The charting-rates path; defaults to <c>api/protected/charting-rates/</c>.</value>
    public string ChartingRatesPath { get; set; } = "api/protected/charting-rates/";

    /// <summary>
    /// Gets or sets the page URL whose referenced script chunks are scanned to discover the authorization token. The
    /// chunk references it carries are resolved against this URL's origin.
    /// </summary>
    /// <value>The bootstrap page URL; defaults to <c>https://www.xe.com/currencycharts</c>.</value>
    public Uri AuthBootstrapUrl { get; set; } = new Uri("https://www.xe.com/currencycharts");

    /// <summary>
    /// Gets or sets the base URL against which lazily-loaded script chunk names, reconstructed from the webpack runtime
    /// chunk's identifier-to-hash map, are resolved when no bootstrap-referenced chunk yields the token. Must end with
    /// a trailing slash so relative <c>static/chunks/…</c> names resolve under it.
    /// </summary>
    /// <value>The script base URL; defaults to <c>https://www.xe.com/_next/</c>.</value>
    public Uri AuthScriptBaseUrl { get; set; } = new Uri("https://www.xe.com/_next/");

    /// <summary>
    /// Validates the XE-specific options, ensuring the charting-rates path and the two token-acquisition URLs are
    /// specified.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every XE-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ChartingRatesPath))
        {
            error = XeResourceStrings.Arg_Invalid_XeOptionsChartingRatesPath;
            return false;
        }

        if (AuthBootstrapUrl is null)
        {
            error = XeResourceStrings.Arg_Invalid_XeOptionsAuthBootstrapUrl;
            return false;
        }

        if (AuthScriptBaseUrl is null)
        {
            error = XeResourceStrings.Arg_Invalid_XeOptionsAuthScriptBaseUrl;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Maps an ISO code through the configured currency aliases, returning the code unchanged when no alias exists.
    /// </summary>
    /// <param name="isoCode">The ISO code to map.</param>
    /// <returns>The aliased symbol component, or <paramref name="isoCode" /> when unmapped.</returns>
    internal string MapCurrencyCode(string isoCode) =>
        MapCurrency(isoCode);
}
