// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves IMF (International Monetary Fund) daily exchange rates as <see cref="ExchangeRate" /> values, implementing
/// the Bodu.Financial provider contracts over the IMF SDMX Data API.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="PairWebRateProvider{TSeries}" />, which supplies the per-pair coverage
/// tracking, single-flight coalescing, fetch-and-accumulate orchestration, and diagnostic logging shared by every
/// pair-based web source; this type contributes only the IMF identity and exception text. A pair is fetched by mapping
/// it to an SDMX series key and requesting the daily data resource over the covered date range. Use
/// <see cref="WebRateProvider.LoadPairAsync" /> to warm a pair's in-memory store.
/// </para>
/// <para>
/// <strong>Daily, USD/SDR-anchored.</strong> The provider requests the daily (<c>FREQ = D</c>) exchange-rate series
/// from the IMF Exchange Rates (<c>ER</c>) dataflow; the seeded series are USD/SDR-anchored domestic-currency-per-USD
/// rates. Only pairs present in <see cref="ImfRateProviderOptions.SeriesMap" /> (or their reverse, served by the base
/// inverse-lookup fallback) are serviceable; an unmapped pair returns no data. Extend the map to add more pairs.
/// </para>
/// <para>
/// <strong>Keyless.</strong> The IMF service requires no API key. <strong>HttpClient ownership.</strong> The
/// constructor that takes only options builds and owns an <see cref="HttpClient" /> configured with the options'
/// <see cref="WebRateProviderOptions.UserAgent" /> and <see cref="WebRateProviderOptions.HttpTimeout" />, disposing it
/// with the provider. The constructor that takes an <see cref="HttpClient" /> uses the caller-supplied client as-is;
/// this is the path the dependency-injection package uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var imf = new ImfRateProvider(new ImfRateProviderOptions());
/// await imf.LoadPairAsync("USD", "GBP", new DateOnly(2023, 1, 1), new DateOnly(2023, 3, 1));
///
/// RateLookupResult gbp = imf.GetRate("USD", "GBP", new DateOnly(2023, 1, 1));
///]]>
/// </code>
/// </example>
public sealed class ImfRateProvider
    : PairWebRateProvider<ImfSeriesInfo>
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "IMF";

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by an <see cref="HttpClient" />
    /// the provider creates and owns, configured from the supplied options.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records downloads and on-demand network fetches. <see langword="null" /> selects a no-op logger.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public ImfRateProvider(ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by the IMF endpoints, queried with
    /// the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records downloads and on-demand network fetches. <see langword="null" /> selects a no-op logger.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public ImfRateProvider(HttpClient httpClient, ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by an explicit pair source, used
    /// for testing.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects a no-op logger.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal ImfRateProvider(IPairRateSource<ImfSeriesInfo> source, ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class from an owned client, building the pair
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private ImfRateProvider(ImfRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(new ImfRateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class, the shared core all public and internal
    /// constructors funnel through.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private ImfRateProvider(
        IPairRateSource<ImfSeriesInfo> source,
        ImfRateProviderOptions options,
        HttpClient? ownedHttpClient,
        ILogger? logger,
        TimeProvider? timeProvider)
        : base(source, options, logger, ownedHttpClient, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string ProviderId => ProviderName;

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.IO_KeyNotFound_ImfRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default pair source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new pair source over the IMF endpoints.</returns>
    private static ImfRateSource CreateSource(HttpClient httpClient, ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return new ImfRateSource(httpClient, options);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }
}
