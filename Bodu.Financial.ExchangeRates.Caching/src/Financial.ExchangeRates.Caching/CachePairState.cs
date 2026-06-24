// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachePairState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The complete persisted state for a single currency pair: the cached rate rows together with the coverage windows
/// that record which date ranges were actually fetched. Both are carried as one immutable unit so a backend reads and
/// writes them atomically.
/// </summary>
/// <remarks>
/// The two collections are independent. Rate rows answer "what is the rate for this date"; coverage windows answer "was
/// this date ever fetched". A range lookup must consult coverage rather than the span of the rate rows, because a
/// sparse set of rows (rates exist only on trading days) can span a window without every interior day having been
/// fetched.
/// </remarks>
internal sealed class CachePairState
{
    /// <summary>The shared empty state, returned by a backend when a pair has neither rows nor coverage recorded.</summary>
    private static readonly CachePairState s_empty =
        new(Array.Empty<CachedExchangeRate>(), Array.Empty<CoverageWindow>());

    /// <summary>
    /// Initializes a new instance of the <see cref="CachePairState" /> class.
    /// </summary>
    /// <param name="entries">The cached rate rows for the pair.</param>
    /// <param name="coverage">The coverage windows recording which date ranges were fetched for the pair.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries" /> or <paramref name="coverage" /> is <see langword="null" />.
    /// </exception>
    public CachePairState(IReadOnlyList<CachedExchangeRate> entries, IReadOnlyList<CoverageWindow> coverage)
    {
        ThrowHelper.ThrowIfNull(entries);
        ThrowHelper.ThrowIfNull(coverage);

        Entries = entries;
        Coverage = coverage;
    }

    /// <summary>
    /// Gets the shared empty state, carrying no rate rows and no coverage windows.
    /// </summary>
    /// <value>An immutable state with empty <see cref="Entries" /> and <see cref="Coverage" />.</value>
    public static CachePairState Empty => s_empty;

    /// <summary>
    /// Gets the cached rate rows for the pair.
    /// </summary>
    /// <value>The cached rate rows, possibly empty.</value>
    public IReadOnlyList<CachedExchangeRate> Entries { get; }

    /// <summary>
    /// Gets the coverage windows recording which date ranges were fetched for the pair.
    /// </summary>
    /// <value>The coverage windows, possibly empty.</value>
    public IReadOnlyList<CoverageWindow> Coverage { get; }
}
