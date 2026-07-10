// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves Yahoo Finance exchange rates as <see cref="ExchangeRate" /> values, implementing the Bodu.Financial provider
/// contracts over the Yahoo Finance <c>v8/finance/chart</c> JSON REST service.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="PairWebRateProvider{TSeries}" />, which supplies the per-pair coverage
/// tracking, single-flight coalescing, fetch-and-accumulate orchestration, and diagnostic logging shared by every
/// pair-based web source; this type contributes only the Yahoo identity, the ticker-based log label, and the
/// Yahoo-specific exception text. Yahoo serves arbitrary pairs through the <c>{FROM}{TO}=X</c> ticker convention, so
/// any pair of ISO codes can be requested directly. Use <see cref="WebRateProvider.LoadPairAsync" /> to warm a pair's
/// in-memory store.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured with the options' <see cref="WebRateProviderOptions.UserAgent" /> and
/// <see cref="WebRateProviderOptions.HttpTimeout" /> (the Yahoo endpoint answers requests without a recognizable user
/// agent with <c>429 Too Many Requests</c>), disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is, leaving its configuration and lifetime to the
/// caller; this is the path the dependency-injection package uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var yahoo = new YahooRateProvider(new YahooRateProviderOptions());
/// await yahoo.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
///
/// RateLookupResult aud = yahoo.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
///]]>
/// </code>
/// </example>
public sealed class YahooRateProvider
    : PairWebRateProvider<YahooSeriesInfo>
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "Yahoo";

    /// <summary>The provider options, retained for the ticker-based log label.</summary>
    private readonly YahooRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooRateProvider" /> class backed by an <see cref="HttpClient" />
    /// the provider creates and owns, configured from the supplied options.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records chart downloads and on-demand network fetches. <see langword="null" /> selects a no-op
    /// logger.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public YahooRateProvider(YahooRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooRateProvider" /> class backed by the Yahoo Finance chart
    /// endpoint, queried with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records chart downloads and on-demand network fetches. <see langword="null" /> selects a no-op
    /// logger.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public YahooRateProvider(HttpClient httpClient, YahooRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooRateProvider" /> class backed by an explicit pair source, used
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
    internal YahooRateProvider(IPairRateSource<YahooSeriesInfo> source, YahooRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooRateProvider" /> class from an owned client, building the pair
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private YahooRateProvider(YahooRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(new YahooChartRateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private YahooRateProvider(
        IPairRateSource<YahooSeriesInfo> source,
        YahooRateProviderOptions options,
        HttpClient? ownedHttpClient,
        ILogger? logger,
        TimeProvider? timeProvider)
        : base(source, options, logger, ownedHttpClient, timeProvider)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override string ProviderId => ProviderName;

    /// <inheritdoc />
    protected override string FormatPairForLog(CurrencyPair pair) =>
        _options.BuildSymbol(pair.From.ToString(), pair.To.ToString());

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.IO_KeyNotFound_YahooRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default pair source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new pair source over the Yahoo Finance chart endpoint.</returns>
    private static YahooChartRateSource CreateSource(HttpClient httpClient, YahooRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return new YahooChartRateSource(httpClient, options);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(YahooRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout, options.MaxResponseContentBufferSize);
    }
}
