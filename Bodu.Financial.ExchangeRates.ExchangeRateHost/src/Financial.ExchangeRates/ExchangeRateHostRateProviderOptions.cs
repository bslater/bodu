// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="ExchangeRateHostRateProvider" /> addresses and interprets the exchangerate.host
/// foreign-exchange REST service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log levels.
/// The members declared here are exchangerate.host-specific: the required <see cref="ApiKey" /> presented as the
/// <c>access_key</c> query parameter, and the <see cref="TimeSeriesPath" /> and <see cref="HistoricalPath" /> endpoint
/// paths. A pair request spanning more than a single day is served from the time-series endpoint; a single-day request
/// is served from the historical (single-date) endpoint, which carries the target date in a <c>date</c> query
/// parameter.
/// </para>
/// <para>
/// exchangerate.host denominates its response against a source currency and returns the requested quote currencies. A
/// request for a source or quote the account's plan does not permit surfaces as a fetch failure rather than being
/// pre-empted here. Every member other than <see cref="ApiKey" /> carries a working default, so the options bind
/// cleanly through <c>Microsoft.Extensions.Options</c>; the dependency-injection package binds this type from
/// configuration and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class ExchangeRateHostRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>The inception of exchangerate.host's historical foreign-exchange data: 1 January 1999.</summary>
    internal static readonly DateOnly HistoryEpoch = new(1999, 1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateHostRateProviderOptions" /> class with the
    /// exchangerate.host host as its base address and a history floor at the January 1999 inception of its historical
    /// data.
    /// </summary>
    public ExchangeRateHostRateProviderOptions()
    {
        BaseAddress = new Uri("https://api.exchangerate.host/");
        HistoryAvailability = RateHistoryAvailability.Since(HistoryEpoch);
    }

    /// <summary>
    /// Gets or sets the exchangerate.host API access key presented as the <c>access_key</c> query parameter on every
    /// request.
    /// </summary>
    /// <value>The API access key. There is no default; a value is required.</value>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative time-series endpoint path used when a request spans more than one day.
    /// </summary>
    /// <value>The time-series path; defaults to <c>timeseries</c>.</value>
    public string TimeSeriesPath { get; set; } = "timeseries";

    /// <summary>
    /// Gets or sets the relative single-date endpoint path used when a request covers exactly one day. The target date
    /// is presented in a <c>date</c> query parameter rather than embedded in the path.
    /// </summary>
    /// <value>The historical path; defaults to <c>historical</c>.</value>
    public string HistoricalPath { get; set; } = "historical";

    /// <summary>
    /// Validates the exchangerate.host-specific options, ensuring the access key is present and the endpoint paths are
    /// usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every exchangerate.host-specific invariant holds; otherwise
    /// <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            error = ExchangeRateHostResourceStrings.Arg_Invalid_ExchangeRateHostOptionsApiKey;
            return false;
        }

        if (string.IsNullOrWhiteSpace(TimeSeriesPath))
        {
            error = ExchangeRateHostResourceStrings.Arg_Invalid_ExchangeRateHostOptionsTimeSeriesPath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(HistoricalPath))
        {
            error = ExchangeRateHostResourceStrings.Arg_Invalid_ExchangeRateHostOptionsHistoricalPath;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Maps an ISO 4217 code to the symbol component exchangerate.host expects, applying any configured currency
    /// aliases.
    /// </summary>
    /// <param name="isoCode">The ISO code to map.</param>
    /// <returns>The aliased symbol, or <paramref name="isoCode" /> when unmapped.</returns>
    internal string MapSymbol(string isoCode) => MapCurrency(isoCode);
}
