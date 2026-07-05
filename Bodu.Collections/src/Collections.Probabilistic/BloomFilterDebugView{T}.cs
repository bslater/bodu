// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="BloomFilter{T}" />, surfacing its geometry and the false-positive
/// rate implied by the current fill.
/// </summary>
/// <typeparam name="T">Specifies the type of elements tracked by the filter.</typeparam>
internal sealed class BloomFilterDebugView<T>
    where T : notnull
{
    /// <summary>The <see cref="BloomFilter{T}" /> instance being debugged.</summary>
    private readonly BloomFilter<T> _filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="BloomFilterDebugView{T}" /> class.
    /// </summary>
    /// <param name="filter">The filter instance to inspect. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="filter" /> is <see langword="null" />.</exception>
    public BloomFilterDebugView(BloomFilter<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>
    /// Gets the number of addressable bits in the filter.
    /// </summary>
    public int BitCount => _filter.BitCount;

    /// <summary>
    /// Gets the false-positive probability the filter was sized for.
    /// </summary>
    public double DesignFalsePositiveRate => _filter.DesignFalsePositiveRate;

    /// <summary>
    /// Gets the false-positive probability implied by the current fill of the filter.
    /// </summary>
    public double EstimatedFalsePositiveRate => _filter.EstimatedFalsePositiveRate;

    /// <summary>
    /// Gets the number of distinct elements the filter was sized for.
    /// </summary>
    public int ExpectedItems => _filter.ExpectedItems;

    /// <summary>
    /// Gets the number of bit positions probed per element.
    /// </summary>
    public int HashCount => _filter.HashCount;
}
