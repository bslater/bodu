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

    /// <summary>The report byte cache, seen through the byte-cache contract the shared download helper reads.</summary>
    private readonly ImfReportByteCache _cache;

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
        _cache = new ImfReportByteCache(cache);
    }

    /// <inheritdoc />
    public async ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(month);

        (byte[] bytes, DateTimeOffset? cachedAtUtc) = await CachedDownload.GetOrDownloadAsync(
            _cache,
            month,
            _options.RefreshInterval,
            _httpClient,
            () => new Uri(
                _options.BaseAddress,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{_options.ReportPath}?SelectDate={month.SelectDate:yyyy-MM-dd}&reportType={_options.ReportType}&tsvflag=Y")),
            cancellationToken).ConfigureAwait(false);

        ImfRateTable table = ImfReportParser.Parse(bytes, _options);
        table.CachedAtUtc = cachedAtUtc;
        return table;
    }
}
