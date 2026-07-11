// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves FRED (Federal Reserve Bank of St. Louis) exchange rates as <see cref="ExchangeRate" /> values, implementing
/// the Bodu.Financial provider contracts over the FRED series-observations JSON REST endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="PairWebRateProvider{TSeries}" />, which supplies the per-pair coverage
/// tracking, single-flight coalescing, fetch-and-accumulate orchestration, and diagnostic logging shared by every
/// pair-based web source; this type contributes only the FRED identity and exception text. A pair is fetched by
/// resolving it to a single FRED series through <see cref="FredRateProviderOptions.SeriesMap" /> and reading that
/// series' observations. Use <see cref="WebRateProvider.LoadPairAsync" /> to warm a pair's in-memory store.
/// </para>
/// <para>
/// <strong>Per-series mapping.</strong> FRED publishes each foreign-exchange rate as an independent time series quoted
/// in a fixed direction. A pair with no mapped series yields no data rather than a fetch; the reverse of a mapped pair
/// is served by the base class's inverse-lookup fallback. Add mappings through
/// <see cref="FredRateProviderOptions.SeriesMap" /> to support additional pairs.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured with the options' <see cref="WebRateProviderOptions.UserAgent" /> and
/// <see cref="WebRateProviderOptions.HttpTimeout" />, disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the dependency-injection package
/// uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var fred = new FredRateProvider(new FredRateProviderOptions { ApiKey = "…" });
/// await fred.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
///
/// RateLookupResult usd = fred.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
///]]>
/// </code>
/// </example>
public sealed class FredRateProvider
    : PairWebRateProvider<FredSeriesInfo>
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "FRED";

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProvider" /> class backed by an <see cref="HttpClient" />
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
    public FredRateProvider(FredRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProvider" /> class backed by the FRED endpoint, queried
    /// with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
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
    public FredRateProvider(HttpClient httpClient, FredRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProvider" /> class backed by an explicit pair source, used
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
    internal FredRateProvider(IPairRateSource<FredSeriesInfo> source, FredRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProvider" /> class from an owned client, building the pair
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private FredRateProvider(FredRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(new FredRateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProvider" /> class, the shared core all public and internal
    /// constructors funnel through.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private FredRateProvider(
        IPairRateSource<FredSeriesInfo> source,
        FredRateProviderOptions options,
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
        string.Format(CultureInfo.CurrentCulture, FredResourceStrings.IO_KeyNotFound_FredRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default pair source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new pair source over the FRED endpoint.</returns>
    private static FredRateSource CreateSource(HttpClient httpClient, FredRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return new FredRateSource(httpClient, options);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(FredRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }
}
