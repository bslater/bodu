// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The in-memory projection of a pair's persisted state: its cached rate rows together with its coverage windows,
/// already parsed from the stored invariant text into the value types the cache surface operates on.
/// </summary>
/// <remarks>
/// The two collections are independent halves of the pair's state: a write through one path preserves the other half so
/// that recording coverage never drops rows and storing rows never drops coverage.
/// </remarks>
internal readonly struct PairState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PairState" /> struct.
    /// </summary>
    /// <param name="entries">The cached rate rows for the pair.</param>
    /// <param name="coverage">
    /// The coverage windows for the pair, each carrying its inclusive start and end dates and the instant it was
    /// fetched.
    /// </param>
    public PairState(
        IReadOnlyList<CachedExchangeRate> entries,
        IReadOnlyList<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> coverage)
    {
        Entries = entries;
        Coverage = coverage;
    }

    /// <summary>
    /// Gets an empty state carrying no rows and no coverage windows, returned when a pair has nothing persisted or a
    /// read fails.
    /// </summary>
    /// <value>An empty <see cref="PairState" />.</value>
    public static PairState Empty { get; } = new(
        Array.Empty<CachedExchangeRate>(),
        Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>());

    /// <summary>
    /// Gets the cached rate rows for the pair.
    /// </summary>
    /// <value>The cached rate rows.</value>
    public IReadOnlyList<CachedExchangeRate> Entries { get; }

    /// <summary>
    /// Gets the coverage windows for the pair.
    /// </summary>
    /// <returns>
    /// The coverage windows, each carrying its inclusive start and end dates and the instant it was fetched.
    /// </returns>
    public IReadOnlyList<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> Coverage { get; }
}
