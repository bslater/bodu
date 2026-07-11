// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an IMF report month's rate table by downloading its tab-separated report file (via a report cache) and
/// parsing it.
/// </summary>
internal sealed class ImfReportTableSource
    : IImfRateTableSource
{
    /// <summary>The HTTP client used to download report files.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, report path, refresh interval, and currency-name map.</summary>
    private readonly ImfRateProviderOptions _options;

    /// <summary>The report byte cache.</summary>
    private readonly IImfReportCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfReportTableSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download report files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="cache">The report byte cache.</param>
    internal ImfReportTableSource(HttpClient httpClient, ImfRateProviderOptions options, IImfReportCache cache)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(cache);

        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(month);

        byte[] bytes = await GetReportBytesAsync(month, cancellationToken).ConfigureAwait(false);

        using MemoryStream stream = new(bytes, writable: false);
        return ImfReportParser.Parse(stream, _options);
    }

    /// <summary>
    /// Returns the report's bytes from the cache, downloading and caching them on a miss.
    /// </summary>
    /// <param name="month">The report month whose file is required.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the report bytes.</returns>
    private async ValueTask<byte[]> GetReportBytesAsync(ImfReportMonth month, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(month, _options.RefreshInterval, out byte[]? cached))
            return cached;

        Uri url = new(
            _options.BaseAddress,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{_options.ReportPath}?SelectDate={month.SelectDate:yyyy-MM-dd}&reportType={_options.ReportType}&tsvflag=Y"));
        byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        _cache.Store(month, bytes);

        return bytes;
    }
}
