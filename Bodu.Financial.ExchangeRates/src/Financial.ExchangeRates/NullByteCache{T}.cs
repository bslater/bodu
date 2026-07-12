// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullByteCache{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IByteCache{TKey}" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
/// <typeparam name="TKey">The type identifying a cached download unit.</typeparam>
public sealed class NullByteCache<TKey>
    : IByteCache<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullByteCache{TKey}" /> class.
    /// </summary>
    private NullByteCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <value>The singleton <see cref="NullByteCache{TKey}" />.</value>
    public static NullByteCache<TKey> Instance { get; } = new();

    /// <inheritdoc />
    public bool TryGet(TKey key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        bytes = null;
        return false;
    }

    /// <inheritdoc />
    public void Store(TKey key, byte[] bytes)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
