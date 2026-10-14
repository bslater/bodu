// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WindowedChartHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test message handler that answers a chart request as the Yahoo Finance endpoint does: with only the daily bars
/// whose timestamps fall in the requested window, from <c>period1</c> inclusive to <c>period2</c> exclusive.
/// </summary>
internal sealed class WindowedChartHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>The JSON text of the chart <c>meta</c> object served with every response.</summary>
    private readonly string _metaJson;

    /// <summary>The bars the simulated chart holds, as Unix-second timestamps and closes.</summary>
    private readonly IReadOnlyList<(long Timestamp, decimal Close)> _bars;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowedChartHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="metaJson">The JSON text of the chart <c>meta</c> object.</param>
    /// <param name="bars">The bars the simulated chart holds.</param>
    public WindowedChartHttpMessageHandler(string metaJson, IReadOnlyList<(long Timestamp, decimal Close)> bars)
    {
        _metaJson = metaJson;
        _bars = bars;
    }

    /// <summary>
    /// Gets the URI of the most recent request, or <see langword="null" /> before the first.
    /// </summary>
    /// <value>The last request URI.</value>
    public Uri? LastRequestUri { get; private set; }

    /// <summary>
    /// Reads an integer query parameter from a request URI.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="name">The parameter name.</param>
    /// <returns>The parameter's value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parameter is absent.</exception>
    public static long ReadQueryValue(Uri uri, string name)
    {
        foreach (string part in uri.Query.TrimStart('?').Split('&'))
        {
            int separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(part[..separator], name, StringComparison.Ordinal))
                return long.Parse(part[(separator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException($"The request has no '{name}' query parameter.");
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri uri = request.RequestUri ?? throw new InvalidOperationException("The request has no URI.");
        LastRequestUri = uri;

        long period1 = ReadQueryValue(uri, "period1");
        long period2 = ReadQueryValue(uri, "period2");
        (long Timestamp, decimal Close)[] served = _bars.Where(bar => bar.Timestamp >= period1 && bar.Timestamp < period2).ToArray();

        string timestamps = string.Join(",", served.Select(bar => bar.Timestamp.ToString(CultureInfo.InvariantCulture)));
        string closes = string.Join(",", served.Select(bar => bar.Close.ToString(CultureInfo.InvariantCulture)));
        string json =
            "{ \"chart\": { \"result\": [ { \"meta\": " + _metaJson + ", \"timestamp\": [" + timestamps
            + "], \"indicators\": { \"quote\": [ { \"close\": [" + closes + "] } ] } } ], \"error\": null } }";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }
}
