// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachePartitionStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Decides how a pair's cached rows are split across files by date: a single file for the whole pair, or one file per
/// calendar year, month, or day, or a caller-supplied scheme.
/// </summary>
/// <remarks>
/// <para>
/// A strategy maps each rate's <see cref="CachedExchangeRate.Date" /> to a path-safe <em>partition key</em> (the file
/// name stem) and reports the inclusive date range that key spans. The file cache uses the key to route each row to its
/// file and the range to split a recorded coverage window at partition boundaries, so a window that crosses, say, a
/// month boundary is stored as one sub-window per month and re-merged losslessly on read.
/// </para>
/// <para>
/// Use the built-in <see cref="Single" />, <see cref="Yearly" />, <see cref="Monthly" />, and <see cref="Daily" />
/// strategies, or <see cref="Custom" /> to supply your own keying and ranging — for example fortnightly or
/// fiscal-quarter partitions.
/// </para>
/// </remarks>
public sealed class ExchangeRateCachePartitionStrategy
{
    /// <summary>The delegate mapping a date to its partition key.</summary>
    private readonly Func<DateOnly, string> _keySelector;

    /// <summary>The delegate mapping a date to the inclusive range of the partition that contains it.</summary>
    private readonly Func<DateOnly, (DateOnly Start, DateOnly End)> _rangeSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateCachePartitionStrategy" /> class.
    /// </summary>
    /// <param name="isPartitioned">
    /// <see langword="true" /> when the strategy splits a pair across multiple files; <see langword="false" /> for a
    /// single file per pair.
    /// </param>
    /// <param name="keySelector">The delegate mapping a date to its partition key.</param>
    /// <param name="rangeSelector">The delegate mapping a date to the inclusive range of its partition.</param>
    private ExchangeRateCachePartitionStrategy(
        bool isPartitioned,
        Func<DateOnly, string> keySelector,
        Func<DateOnly, (DateOnly Start, DateOnly End)> rangeSelector)
    {
        IsPartitioned = isPartitioned;
        _keySelector = keySelector;
        _rangeSelector = rangeSelector;
    }

    /// <summary>
    /// Gets the strategy that keeps every row for a pair in a single file.
    /// </summary>
    /// <value>The single-file strategy.</value>
    public static ExchangeRateCachePartitionStrategy Single { get; } =
        new(false, static _ => string.Empty, static _ => (DateOnly.MinValue, DateOnly.MaxValue));

    /// <summary>
    /// Gets the strategy that splits a pair into one file per calendar year, keyed <c>yyyy</c>.
    /// </summary>
    /// <value>The yearly partition strategy.</value>
    public static ExchangeRateCachePartitionStrategy Yearly { get; } =
        new(
            true,
            static date => date.Year.ToString("D4", CultureInfo.InvariantCulture),
            static date => (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31)));

    /// <summary>
    /// Gets the strategy that splits a pair into one file per calendar month, keyed <c>yyyy-MM</c>.
    /// </summary>
    /// <value>The monthly partition strategy.</value>
    public static ExchangeRateCachePartitionStrategy Monthly { get; } =
        new(
            true,
            static date => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            static date => (
                new DateOnly(date.Year, date.Month, 1),
                new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))));

    /// <summary>
    /// Gets the strategy that splits a pair into one file per calendar day, keyed <c>yyyy-MM-dd</c>.
    /// </summary>
    /// <value>The daily partition strategy.</value>
    public static ExchangeRateCachePartitionStrategy Daily { get; } =
        new(
            true,
            static date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            static date => (date, date));

    /// <summary>
    /// Gets a value indicating whether the strategy splits a pair's rows across multiple files.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when rows are partitioned into per-key files; <see langword="false" /> when the whole
    /// pair lives in one file.
    /// </value>
    public bool IsPartitioned { get; }

    /// <summary>
    /// Creates a custom partition strategy from a key selector and a matching range selector.
    /// </summary>
    /// <param name="keySelector">Maps a date to its path-safe partition key.</param>
    /// <param name="rangeSelector">Maps a date to the inclusive date range its partition spans.</param>
    /// <returns>A partition strategy driven by the supplied delegates.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="keySelector" /> or <paramref name="rangeSelector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The two delegates must agree: every date the <paramref name="rangeSelector" /> reports for a partition must map
    /// to the same key under <paramref name="keySelector" />, otherwise coverage windows split at the wrong boundaries.
    /// The returned key must be a valid file-name stem; the file cache does not sanitize it.
    /// </remarks>
    public static ExchangeRateCachePartitionStrategy Custom(
        Func<DateOnly, string> keySelector,
        Func<DateOnly, (DateOnly Start, DateOnly End)> rangeSelector)
    {
        ThrowHelper.ThrowIfNull(keySelector);
        ThrowHelper.ThrowIfNull(rangeSelector);

        return new ExchangeRateCachePartitionStrategy(true, keySelector, rangeSelector);
    }

    /// <summary>
    /// Returns the partition key for the file that holds rows observed on <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date to map.</param>
    /// <returns>
    /// The partition key — a path-safe file-name stem, or the empty string for <see cref="Single" />.
    /// </returns>
    public string GetPartitionKey(DateOnly date) =>
        _keySelector(date);

    /// <summary>
    /// Returns the inclusive date range spanned by the partition that contains <paramref name="date" />.
    /// </summary>
    /// <param name="date">A date within the partition.</param>
    /// <returns>The inclusive <c>(Start, End)</c> range of the containing partition.</returns>
    public (DateOnly Start, DateOnly End) GetPartitionRange(DateOnly date) =>
        _rangeSelector(date);
}
