// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRbaWorkbookCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IRbaWorkbookCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullRbaWorkbookCache
    : IRbaWorkbookCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullRbaWorkbookCache" /> class.
    /// </summary>
    private NullRbaWorkbookCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <value>The singleton <see cref="NullRbaWorkbookCache" />.</value>
    public static NullRbaWorkbookCache Instance { get; } = new();

    /// <inheritdoc />
    public bool TryGet(RbaEraWorkbook era, TimeSpan currentEraRefreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        bytes = null;
        return false;
    }

    /// <inheritdoc />
    public void Store(RbaEraWorkbook era, byte[] bytes)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
