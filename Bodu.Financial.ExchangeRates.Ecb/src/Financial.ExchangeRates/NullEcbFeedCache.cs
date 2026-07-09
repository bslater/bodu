// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullEcbFeedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IEcbFeedCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullEcbFeedCache
    : IEcbFeedCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullEcbFeedCache" /> class.
    /// </summary>
    private NullEcbFeedCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <value>The singleton <see cref="NullEcbFeedCache" />.</value>
    public static NullEcbFeedCache Instance { get; } = new();

    /// <inheritdoc />
    public bool TryGet(EcbRateFeed feed, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        bytes = null;
        return false;
    }

    /// <inheritdoc />
    public void Store(EcbRateFeed feed, byte[] bytes)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
