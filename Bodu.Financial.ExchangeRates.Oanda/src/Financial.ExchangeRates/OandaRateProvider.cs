// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves OANDA (Historical Currency Converter) exchange rates as <see cref="ExchangeRate" /> values, implementing the
/// Bodu.Financial provider contracts over the public OANDA Rates history JSON service.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="PairWebRateProvider{TSeries}" />, which supplies the per-pair coverage
/// tracking, single-flight coalescing, fetch-and-accumulate orchestration, and diagnostic logging shared by every
/// pair-based web source; this type contributes only the OANDA identity and the OANDA-specific exception text. OANDA
/// serves arbitrary pairs through its rate-history endpoint, so any pair of ISO codes can be requested directly. Use
/// <see cref="WebRateProvider.LoadPairAsync" /> to warm a pair's in-memory store.
/// </para>
/// <para>
/// <strong>History depth.</strong> The anonymous endpoint serves only a rolling recent window (roughly the last 180
/// days); requesting an earlier start date returns only what the feed publishes. The provider advertises this through
/// <see cref="WebRateProvider.HistoryAvailability" />, so a caller can resolve the earliest date worth
/// requesting before issuing a lookup.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured with the options' <see cref="WebRateProviderOptions.UserAgent" /> and
/// <see cref="WebRateProviderOptions.HttpTimeout" /> (the OANDA endpoint rejects requests without a
/// recognizable user agent), disposing it with the provider. The constructor that takes an <see cref="HttpClient" />
/// uses the caller-supplied client as-is, leaving its configuration and lifetime to the caller; this is the path the
/// dependency-injection package uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var oanda = new OandaRateProvider(new OandaRateProviderOptions());
/// var today = DateOnly.FromDateTime(DateTime.UtcNow);
/// await oanda.LoadPairAsync("AUD", "USD", today.AddDays(-30), today);
///
/// RateLookupResult usd = oanda.GetRate("AUD", "USD", today.AddDays(-1));
///]]>
/// </code>
/// </example>
public sealed class OandaRateProvider
    : PairWebRateProvider<OandaSeriesInfo>
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "OANDA";

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaRateProvider" /> class backed by an
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
    public OandaRateProvider(OandaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaRateProvider" /> class backed by the OANDA endpoint,
    /// queried with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue history requests.</param>
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
    public OandaRateProvider(HttpClient httpClient, OandaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaRateProvider" /> class backed by an explicit pair
    /// source, used for testing.
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
    internal OandaRateProvider(IPairRateSource<OandaSeriesInfo> source, OandaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaRateProvider" /> class from an owned client, building
    /// the source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private OandaRateProvider(OandaRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(new OandaHistorySource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private OandaRateProvider(
        IPairRateSource<OandaSeriesInfo> source,
        OandaRateProviderOptions options,
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
        string.Format(CultureInfo.CurrentCulture, OandaResourceStrings.IO_KeyNotFound_OandaRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default history source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue history requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new history source.</returns>
    private static OandaHistorySource CreateSource(HttpClient httpClient, OandaRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return new OandaHistorySource(httpClient, options);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(OandaRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }
}
