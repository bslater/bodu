// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves XE.com exchange rates as <see cref="ExchangeRate" /> values, implementing the Bodu.Financial provider
/// contracts over the XE charting-rates JSON service.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="PairWebExchangeRateProvider{TSeries}" />, which supplies the per-pair coverage
/// tracking, single-flight coalescing, fetch-and-accumulate orchestration, and diagnostic logging shared by every
/// pair-based web source; this type contributes only the XE identity and the XE-specific exception text. XE serves
/// arbitrary pairs through its charting-rates endpoint, so any pair of ISO codes can be requested directly. Use
/// <see cref="PairWebExchangeRateProvider{TSeries}.LoadPairAsync" /> to warm a pair's in-memory store.
/// </para>
/// <para>
/// <strong>Authorization.</strong> The charting-rates endpoint requires an <c>Authorization: Basic</c> token that is
/// not published as a stable credential. The provider acquires it automatically by scanning the script chunks the XE
/// website publishes for the credential, caches it, and refreshes it when the endpoint rejects it. This depends on the
/// XE website's current structure and is inherently brittle.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured with the options' <see cref="WebExchangeRateProviderOptions.UserAgent" /> and
/// <see cref="WebExchangeRateProviderOptions.HttpTimeout" />, disposing it with the provider. The constructor that
/// takes an <see cref="HttpClient" /> uses the caller-supplied client as-is, leaving its configuration and lifetime to
/// the caller; this is the path the dependency-injection package uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var xe = new XeExchangeRateProvider(new XeExchangeRateOptions());
/// await xe.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
///
/// ExchangeRateLookupResult usd = xe.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
///]]>
/// </code>
/// </example>
public sealed class XeExchangeRateProvider
    : PairWebExchangeRateProvider<XeSeriesInfo>
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "XE";

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateProvider" /> class backed by an
    /// <see cref="HttpClient" /> the provider creates and owns, configured from the supplied options.
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
    public XeExchangeRateProvider(XeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateProvider" /> class backed by the XE endpoint, queried
    /// with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue charting-rates and token-acquisition requests.</param>
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
    public XeExchangeRateProvider(HttpClient httpClient, XeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateProvider" /> class backed by an explicit pair source,
    /// used for testing.
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
    internal XeExchangeRateProvider(IExchangeRatePairSource<XeSeriesInfo> source, XeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateProvider" /> class from an owned client, building the
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private XeExchangeRateProvider(XeExchangeRateOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XeExchangeRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private XeExchangeRateProvider(
        IExchangeRatePairSource<XeSeriesInfo> source,
        XeExchangeRateOptions options,
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
        string.Format(CultureInfo.CurrentCulture, XeResourceStrings.IO_KeyNotFound_XeRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default charting-rates source — and the scraping token provider it depends on — from the supplied
    /// client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue charting-rates and token-acquisition requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new charting-rates source.</returns>
    private static XeChartingRatesSource CreateSource(HttpClient httpClient, XeExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        XeScrapingAuthTokenProvider tokenProvider = new(httpClient, options);
        return new XeChartingRatesSource(httpClient, options, tokenProvider);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(XeExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return ExchangeRateHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }
}
