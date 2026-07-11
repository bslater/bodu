// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies a single calendar month of the IMF Representative Exchange Rates report, the download unit the
/// <see cref="ImfRateProvider" /> fetches and caches.
/// </summary>
/// <remarks>
/// The IMF publishes one monthly report covering every reported currency for each business day of the month. A month is
/// therefore the natural download unit: fetching it warms the store for every currency and every business day at once.
/// The report for a closed month is immutable, which the on-disk cache exploits to keep past months indefinitely.
/// </remarks>
public sealed class ImfReportMonth
    : IEquatable<ImfReportMonth>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfReportMonth" /> class.
    /// </summary>
    /// <param name="year">The four-digit calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is not in the range 1 through 12.
    /// </exception>
    public ImfReportMonth(int year, int month)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);

        Year = year;
        Month = month;
    }

    /// <summary>
    /// Gets the four-digit calendar year of the report.
    /// </summary>
    /// <value>The calendar year.</value>
    public int Year { get; }

    /// <summary>
    /// Gets the calendar month of the report.
    /// </summary>
    /// <value>The calendar month, from 1 (January) through 12 (December).</value>
    public int Month { get; }

    /// <summary>
    /// Gets the coalescing and cache key for the month.
    /// </summary>
    /// <value>The month in <c>yyyy-MM</c> form, for example <c>2026-04</c>.</value>
    public string Key => string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}");

    /// <summary>
    /// Gets the cache file name for the month's report.
    /// </summary>
    /// <value>The file name, for example <c>imf_rep_2026-04.tsv</c>.</value>
    public string FileName => string.Create(CultureInfo.InvariantCulture, $"imf_rep_{Year:D4}-{Month:D2}.tsv");

    /// <summary>
    /// Gets the date used to select the month's report on the IMF endpoint: the last calendar day of the month.
    /// </summary>
    /// <value>The last day of the report month.</value>
    public DateOnly SelectDate => new DateOnly(Year, Month, 1).AddMonths(1).AddDays(-1);

    /// <inheritdoc />
    public bool Equals(ImfReportMonth? other) =>
        other is not null && Year == other.Year && Month == other.Month;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as ImfReportMonth);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Year, Month);

    /// <inheritdoc />
    public override string ToString() =>
        Key;
}
