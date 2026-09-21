// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LateJoinerNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> decorator that parks the <em>first</em> reader inside its lookup, after that
/// lookup has already observed its result, so a test can pin the single-flight late-joiner interleaving deterministically.
/// </summary>
/// <remarks>
/// <para>
/// The stampede window the parked reader reproduces is narrow and timing-dependent in production: a caller reads the
/// cache and misses, a second caller computes the same year and removes its completed flight, and only then does the
/// first caller reach the coalescing dictionary — finding no flight to join even though the year is now cached.
/// </para>
/// <para>
/// Holding the first reader at exactly that point turns the race into a fixed sequence: the test releases it only once
/// the winning computation has stored its entry and retired its flight. Every later read passes straight through, so
/// the winner's own lookup and any re-read performed inside a flight are unaffected.
/// </para>
/// </remarks>
internal sealed class LateJoinerNotableDateCache
    : INotableDateCache, IDisposable
{
    /// <summary>The cache the decorator delegates every operation to.</summary>
    private readonly INotableDateCache _inner;

    /// <summary>Signalled once the first lookup has run and is parked.</summary>
    private readonly ManualResetEventSlim _parked = new(initialState: false);

    /// <summary>Signalled by the test to let the parked lookup return.</summary>
    private readonly ManualResetEventSlim _released = new(initialState: false);

    /// <summary>The number of lookups started.</summary>
    private int _reads;

    /// <summary>
    /// Initializes a new instance of the <see cref="LateJoinerNotableDateCache" /> class.
    /// </summary>
    /// <param name="inner">The cache to delegate to.</param>
    public LateJoinerNotableDateCache(INotableDateCache inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Blocks until the first lookup has run and parked.
    /// </summary>
    /// <param name="timeout">How long to wait.</param>
    /// <returns><see langword="true" /> if the reader parked within <paramref name="timeout" />.</returns>
    public bool WaitUntilParked(TimeSpan timeout) => _parked.Wait(timeout);

    /// <summary>
    /// Releases the parked lookup.
    /// </summary>
    public void Release() => _released.Set();

    /// <inheritdoc />
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        // Resolve against the inner cache first: the parked reader must carry forward the result it saw *before* the
        // winner stored anything, which is what makes it a late joiner rather than a plain second caller.
        NotableDateCacheEntry? entry = _inner.GetYear(territory, year, resourceVersion, ttl, asOf);

        if (Interlocked.Increment(ref _reads) == 1)
        {
            _parked.Set();
            _released.Wait(TimeSpan.FromSeconds(30));
        }

        return entry;
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf) =>
        _inner.StoreYear(entry, ttl, asOf);

    /// <inheritdoc />
    public void Clear() => _inner.Clear();

    /// <inheritdoc />
    public void Dispose()
    {
        _parked.Dispose();
        _released.Dispose();
        (_inner as IDisposable)?.Dispose();
    }
}
