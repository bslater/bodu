// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaXlsRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an RBA era's rate table by downloading its <c>.xls</c> file (via a workbook cache) and parsing it with the
/// BIFF8 reader.
/// </summary>
internal sealed class RbaXlsRateTableSource
    : IRbaRateTableSource
{
    /// <summary>The HTTP client used to download era files.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base URL, refresh interval, and alias map.</summary>
    private readonly RbaRateProviderOptions _options;

    /// <summary>The workbook byte cache.</summary>
    private readonly IRbaWorkbookCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaXlsRateTableSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download era files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="cache">The workbook byte cache.</param>
    internal RbaXlsRateTableSource(HttpClient httpClient, RbaRateProviderOptions options, IRbaWorkbookCache cache)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(cache);

        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<RbaRateTable> GetTableAsync(RbaEra era, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(era);

        byte[] bytes = await GetWorkbookBytesAsync(era, cancellationToken).ConfigureAwait(false);

        using MemoryStream stream = new(bytes, writable: false);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, _options);
    }

    /// <summary>
    /// Returns the era's workbook bytes from the cache, downloading and caching them on a miss.
    /// </summary>
    /// <param name="era">The era whose workbook is required.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the workbook bytes.</returns>
    private async ValueTask<byte[]> GetWorkbookBytesAsync(RbaEra era, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(era, _options.CurrentEraRefreshInterval, out byte[]? cached))
            return cached;

        Uri url = new(_options.BaseUrl, era.FileName);
        byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        _cache.Store(era, bytes);

        return bytes;
    }
}
