// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Adapts an <see cref="IDatedRateProvider" /> to the simpler timeless <see cref="IRateProvider" />
/// surface by pinning a fixed valuation date and lookup options.
/// </summary>
/// <remarks>
/// <para>
/// Use this adapter when an existing consumer already accepts <see cref="IRateProvider" /> but the rates should
/// nevertheless come from a date-aware source — for example, a fixed reporting-period end-date used to convert many
/// amounts consistently throughout a single accounting workflow.
/// </para>
/// </remarks>
public sealed class DatedRateProviderAdapter
    : IRateProvider
{
    /// <summary>The underlying dated provider that resolves the actual rate.</summary>
    private readonly IDatedRateProvider _inner;

    /// <summary>The fixed valuation date supplied to <see cref="_inner" /> on every lookup.</summary>
    private readonly DateOnly _date;

    /// <summary>The fixed (non-null) lookup options supplied to <see cref="_inner" /> on every lookup.</summary>
    private readonly RateLookupOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatedRateProviderAdapter" /> class.
    /// </summary>
    /// <param name="inner">The underlying dated provider to delegate to.</param>
    /// <param name="date">The valuation date pinned to every lookup.</param>
    /// <param name="options">
    /// The lookup options pinned to every lookup. <see langword="null" /> is treated as
    /// <see cref="RateLookupOptions.Exact" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="options" /> contains an undefined enum value or a negative tolerance.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="options" /> specifies <see cref="RateDateResolution.Exact" /> with a non-zero
    /// tolerance.
    /// </exception>
    public DatedRateProviderAdapter(
        IDatedRateProvider inner,
        DateOnly date,
        RateLookupOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(inner);
        options ??= RateLookupOptions.Exact;
        options.Validate();

        _inner = inner;
        _date = date;
        _options = options;
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode) =>
        _inner.GetRate(fromIsoCode, toIsoCode, _date, _options).Rate.Rate;
}
