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
/// The request addresses the history window through inclusive Unix-millisecond range bounds in the path, and the parser
/// additionally restricts the parsed observations to the request's inclusive range as a defensive measure. The
/// <c>User-Agent</c> the OFX endpoint requires is configured on the <see cref="HttpClient" /> (by the provider when it
/// owns the client, or by the caller when the client is supplied), not per request.
/// </remarks>
internal sealed class OfxSpotRateHistorySource
    : IExchangeRatePairSource<OfxSeriesInfo>
{
    /// <summary>The HTTP client used to issue history requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, history path, and query parameters.</summary>
    private readonly OfxExchangeRateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxSpotRateHistorySource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue history requests.</param>
    /// <param name="options">The provider options.</param>
    internal OfxSpotRateHistorySource(HttpClient httpClient, OfxExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<OfxSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        Uri url = BuildRequestUri(request);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return OfxSpotRateHistoryResponseParser.Parse(json, request, _options);
    }

    /// <summary>
    /// Builds the absolute history request URI from the options and request.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(ExchangeRatePairRequest request)
    {
        // OFX addresses the range through inclusive Unix-millisecond path bounds: the start at the beginning of the
        // start date and the end at the last millisecond of the end date, both interpreted in UTC.
        long startMs = ToUnixMilliseconds(request.StartDate, TimeOnly.MinValue);
        long endMs = ToUnixMilliseconds(request.EndDate, TimeOnly.MaxValue);

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
    /// Converts a date and time-of-day to a Unix-millisecond timestamp interpreted in UTC.
    /// </summary>
    /// <param name="date">The calendar date.</param>
    /// <param name="time">The time-of-day within the date.</param>
    /// <returns>The corresponding Unix-millisecond timestamp.</returns>
    private static long ToUnixMilliseconds(DateOnly date, TimeOnly time) =>
        new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
