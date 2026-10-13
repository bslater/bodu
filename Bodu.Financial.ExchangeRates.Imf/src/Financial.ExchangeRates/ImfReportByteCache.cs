// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportByteCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Presents an <see cref="IImfReportCache" /> as the <see cref="IByteCache{TKey}" /> the shared download helper reads,
/// forwarding every call unchanged.
/// </summary>
internal sealed class ImfReportByteCache
    : IByteCache<ImfReportMonth>
{
    /// <summary>The report cache every call is forwarded to.</summary>
    private readonly IImfReportCache _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfReportByteCache" /> class.
    /// </summary>
    /// <param name="inner">The report cache to forward to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    internal ImfReportByteCache(IImfReportCache inner)
    {
        ThrowHelper.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc />
    public bool TryGet(ImfReportMonth key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes) =>
        _inner.TryGet(key, refreshInterval, out bytes);

    /// <inheritdoc />
    public bool TryGet(ImfReportMonth key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes, out DateTimeOffset? storedAtUtc) =>
        _inner.TryGet(key, refreshInterval, out bytes, out storedAtUtc);

    /// <inheritdoc />
    public void Store(ImfReportMonth key, byte[] bytes) =>
        _inner.Store(key, bytes);
}
