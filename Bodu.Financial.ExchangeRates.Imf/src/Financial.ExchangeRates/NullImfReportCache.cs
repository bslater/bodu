// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullImfReportCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IImfReportCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullImfReportCache
    : IImfReportCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullImfReportCache" /> class.
    /// </summary>
    private NullImfReportCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <value>The singleton <see cref="NullImfReportCache" />.</value>
    public static NullImfReportCache Instance { get; } = new();

    /// <inheritdoc />
    public bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        bytes = null;
        return false;
    }

    /// <inheritdoc />
    public void Store(ImfReportMonth month, byte[] bytes)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
