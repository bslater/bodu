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
/// two URLs (<see cref="AuthBootstrapUrl" /> and <see cref="AuthChunkBaseUrl" />) from which the
/// <c>Authorization: Basic</c> token is discovered by inspecting the XE website's published script bundle. XE serves a
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
    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateOptions" /> class with the XE.com host as its base
    /// address.
    /// </summary>
    public XeExchangeRateOptions()
    {
        BaseAddress = new Uri("https://www.xe.com/");
    }

    /// <summary>
    /// Gets or sets the relative charting-rates endpoint path. The source-currency and destination-currency codes are
    /// supplied as the <c>fromCurrency</c> and <c>toCurrency</c> query parameters when the request is issued.
    /// </summary>
    /// <value>The charting-rates path; defaults to <c>api/protected/charting-rates/</c>.</value>
    public string ChartingRatesPath { get; set; } = "api/protected/charting-rates/";

    /// <summary>
    /// Gets or sets the page URL from which the name of the current XE application script bundle is discovered while
    /// acquiring the authorization token.
    /// </summary>
    /// <value>The bootstrap page URL; defaults to <c>https://www.xe.com/currencycharts</c>.</value>
    public Uri AuthBootstrapUrl { get; set; } = new Uri("https://www.xe.com/currencycharts");

    /// <summary>
    /// Gets or sets the base URL under which the discovered XE application script bundle is downloaded while acquiring
    /// the authorization token. The discovered script file name is appended to this URL.
    /// </summary>
    /// <value>The chunk base URL; defaults to <c>https://www.xe.com/_next/static/chunks/pages/</c>.</value>
    public Uri AuthChunkBaseUrl { get; set; } = new Uri("https://www.xe.com/_next/static/chunks/pages/");

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

        if (AuthChunkBaseUrl is null)
        {
            error = XeResourceStrings.Arg_Invalid_XeOptionsAuthChunkBaseUrl;
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
