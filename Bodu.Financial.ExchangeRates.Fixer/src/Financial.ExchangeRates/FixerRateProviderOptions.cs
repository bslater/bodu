// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="FixerRateProvider" /> addresses and interprets the Fixer (<c>fixer.io</c>)
/// foreign-exchange REST service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log levels.
/// The members declared here are Fixer-specific: the required <see cref="ApiKey" /> presented as the <c>access_key</c>
/// query parameter, and the <see cref="TimeSeriesPath" /> and <see cref="HistoricalPath" /> templates. A pair request
/// spanning more than a single day is served from the time-series endpoint; a single-day request is served from the
/// historical (single-date) endpoint.
/// </para>
/// <para>
/// Fixer denominates its response against a base currency (EUR on the free plan; an arbitrary base on paid plans) and
/// returns the requested quote symbols. A request for a base or quote the account's plan does not permit surfaces as a
/// fetch failure rather than being pre-empted here. Every member other than <see cref="ApiKey" /> carries a working
/// default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c>; the dependency-injection package
/// binds this type from configuration and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class FixerRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>The placeholder token replaced by the ISO date when building a path from <see cref="HistoricalPath" />.</summary>
    internal const string DatePlaceholder = "{date}";

    /// <summary>The inception of Fixer's historical foreign-exchange data: 1 January 1999.</summary>
    internal static readonly DateOnly HistoryEpoch = new(1999, 1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FixerRateProviderOptions" /> class with the Fixer host as its base
    /// address and a history floor at the January 1999 inception of Fixer's historical data.
    /// </summary>
    public FixerRateProviderOptions()
    {
        BaseAddress = new Uri("https://data.fixer.io/api/");
        HistoryAvailability = RateHistoryAvailability.Since(HistoryEpoch);
    }

    /// <summary>
    /// Gets or sets the Fixer API access key presented as the <c>access_key</c> query parameter on every request.
    /// </summary>
    /// <value>The API access key. There is no default; a value is required.</value>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative time-series endpoint path used when a request spans more than one day.
    /// </summary>
    /// <value>The time-series path; defaults to <c>timeseries</c>.</value>
    public string TimeSeriesPath { get; set; } = "timeseries";

    /// <summary>
    /// Gets or sets the relative single-date endpoint path template used when a request covers exactly one day. The
    /// <c>{date}</c> placeholder is replaced by the ISO date before the request is issued.
    /// </summary>
    /// <value>The historical path template; defaults to <c>{date}</c>.</value>
    public string HistoricalPath { get; set; } = DatePlaceholder;

    /// <summary>
    /// Validates the Fixer-specific options, ensuring the access key is present and the endpoint templates are usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every Fixer-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            error = FixerResourceStrings.Arg_Invalid_FixerOptionsApiKey;
            return false;
        }

        if (string.IsNullOrWhiteSpace(TimeSeriesPath))
        {
            error = FixerResourceStrings.Arg_Invalid_FixerOptionsTimeSeriesPath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(HistoricalPath) || !HistoricalPath.Contains(DatePlaceholder, StringComparison.Ordinal))
        {
            error = FixerResourceStrings.Arg_Invalid_FixerOptionsHistoricalPath;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Maps an ISO 4217 code to the symbol component Fixer expects, applying any configured currency aliases.
    /// </summary>
    /// <param name="isoCode">The ISO code to map.</param>
    /// <returns>The aliased symbol, or <paramref name="isoCode" /> when unmapped.</returns>
    internal string MapSymbol(string isoCode) => MapCurrency(isoCode);
}
