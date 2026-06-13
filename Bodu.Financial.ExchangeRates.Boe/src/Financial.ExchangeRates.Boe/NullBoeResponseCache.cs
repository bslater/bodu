// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullBoeResponseCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// An <see cref="IBoeResponseCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullBoeResponseCache
    : IBoeResponseCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullBoeResponseCache" /> class.
    /// </summary>
    private NullBoeResponseCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <returns>The singleton <see cref="NullBoeResponseCache" />.</returns>
    public static NullBoeResponseCache Instance { get; } = new();

    /// <inheritdoc />
    public bool TryGet(DateOnly startDate, DateOnly endDate, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        bytes = null;
        return false;
    }

    /// <inheritdoc />
    public void Store(DateOnly startDate, DateOnly endDate, byte[] bytes)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
