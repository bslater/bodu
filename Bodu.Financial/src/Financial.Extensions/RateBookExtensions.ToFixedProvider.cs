// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookExtensions.ToFixedProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Extensions;

public static partial class RateBookExtensions
{
    /// <summary>
    /// Wraps the book in an immutable <see cref="FixedDatedRateProvider" />, deriving the provider selection
    /// from the book's single provider per pair.
    /// </summary>
    /// <param name="book">The immutable book to wrap.</param>
    /// <returns>A provider resolving rates from <paramref name="book" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="book" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="book" /> contains two providers for the same pair; use
    /// <see cref="ToFixedProvider(RateBook, IEnumerable{string})" /> to disambiguate.
    /// </exception>
    public static FixedDatedRateProvider ToFixedProvider(this RateBook book)
    {
        ThrowHelper.ThrowIfNull(book);

        return new FixedDatedRateProvider(book);
    }

    /// <summary>
    /// Wraps the book in an immutable <see cref="FixedDatedRateProvider" /> applying an explicit
    /// provider-priority list per pair.
    /// </summary>
    /// <param name="book">The immutable book to wrap.</param>
    /// <param name="providerPriority">
    /// The ordered set of providers consulted for every pair. The first provider in this list that has a matching
    /// series for the pair wins; providers absent from the list are unreachable through the returned provider.
    /// </param>
    /// <returns>A provider resolving rates from <paramref name="book" /> under the supplied priority.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="book" /> or <paramref name="providerPriority" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerPriority" /> is empty or contains a null/whitespace entry.
    /// </exception>
    public static FixedDatedRateProvider ToFixedProvider(this RateBook book, IEnumerable<string> providerPriority)
    {
        ThrowHelper.ThrowIfNull(book);

        return new FixedDatedRateProvider(book, providerPriority);
    }
}
