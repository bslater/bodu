// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistorySource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an OFX spot-rate history by issuing a GET against the configured history endpoint and parsing the JSON
/// response.
/// </summary>
/// <remarks>
/// <para>
/// The request addresses the history window through inclusive Unix-millisecond range bounds in the path, and the parser
/// additionally restricts the parsed observations to the request's inclusive range as a defensive measure. The
/// <c>User-Agent</c> the OFX endpoint requires is configured on the <see cref="HttpClient" /> (by the provider when it
/// owns the client, or by the caller when the client is supplied), not per request.
/// </para>
/// <para>
/// OFX rejects a <c>ToDate</c> that lies in its future, so the end bound is capped at the current instant (less
/// <see cref="OfxExchangeRateOptions.FutureClampSkew" />). This keeps the request timezone-agnostic: whatever calendar
/// date a caller derived from its local clock, the source never asks OFX for anything past "now". A request whose whole
/// window begins after the current instant is answered with an empty result without issuing an HTTP call.
/// </para>
/// </remarks>
internal sealed class OfxSpotRateHistorySource
    : IExchangeRatePairSource<OfxSeriesInfo>
{
    /// <summary>The HTTP client used to issue history requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, history path, and query parameters.</summary>
    private readonly OfxExchangeRateOptions _options;

    /// <summary>The time source used to resolve the current instant that bounds the request's end date.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxSpotRateHistorySource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue history requests.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    internal OfxSpotRateHistorySource(HttpClient httpClient, OfxExchangeRateOptions options, TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<OfxSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        // OFX rejects a ToDate in its future, so cap the end bound at the current instant (less any configured skew
        // margin). The start of the requested day and the capped end delimit what is actually fetched.
        long nowMs = (_timeProvider.GetUtcNow() - _options.FutureClampSkew).ToUnixTimeMilliseconds();
        long startMs = ToUnixMilliseconds(request.StartDate, TimeOnly.MinValue);
        long endMs = Math.Min(ToUnixMilliseconds(request.EndDate, TimeOnly.MaxValue), nowMs);

        // The whole requested window begins after "now"; OFX has nothing to return, so skip the call entirely.
        if (startMs > nowMs)
            return EmptyResult(request);

        Uri url = BuildRequestUri(request, startMs, endMs);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return OfxSpotRateHistoryResponseParser.Parse(json, request, _options);
    }

    /// <summary>
    /// Builds the absolute history request URI from the options, request, and computed Unix-millisecond range bounds.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <param name="startMs">The inclusive range start, as Unix milliseconds.</param>
    /// <param name="endMs">The inclusive range end, as Unix milliseconds, already capped at the current instant.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(ExchangeRatePairRequest request, long startMs, long endMs)
    {
        // The path is built from validated ISO letters and numeric bounds substituted into a fixed template, so it is composed directly.
        string path = _options.BuildPath(request.Pair.From.ToString(), request.Pair.To.ToString(), startMs, endMs);

        UriBuilder builder = new(new Uri(_options.BaseAddress, path))
        {
            Query = string.Format(
                CultureInfo.InvariantCulture,
                "DecimalPlaces={0}&ReportingInterval={1}&format=json",
                _options.DecimalPlaces,
                _options.ReportingInterval),
        };

        return builder.Uri;
    }

    /// <summary>
    /// Builds an empty result carrying the request's pair and series metadata, used when the requested window lies
    /// entirely in the future.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <returns>An empty <see cref="PairRateData{TSeries}" /> for the request's pair.</returns>
    private static PairRateData<OfxSeriesInfo> EmptyResult(ExchangeRatePairRequest request)
    {
        OfxSeriesInfo series = new(request.Pair, request.Pair.To.ToString());
        return new PairRateData<OfxSeriesInfo>(request.Pair, Array.Empty<ExchangeRateObservation>(), series);
    }

    /// <summary>
    /// Converts a date and time-of-day to a Unix-millisecond timestamp interpreted in UTC.
    /// </summary>
    /// <param name="date">The calendar date.</param>
    /// <param name="time">The time-of-day within the date.</param>
    /// <returns>The corresponding Unix-millisecond timestamp.</returns>
    private static long ToUnixMilliseconds(DateOnly date, TimeOnly time) =>
        new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
