// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderAdapter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Adapts an <see cref="IDatedExchangeRateProvider" /> to the simpler timeless <see cref="IExchangeRateProvider" />
/// surface by pinning a fixed valuation date and lookup options.
/// </summary>
/// <remarks>
/// <para>
/// Use this adapter when an existing consumer already accepts <see cref="IExchangeRateProvider" /> but the rates should
/// nevertheless come from a date-aware source — for example, a fixed reporting-period end-date used to convert many
/// amounts consistently throughout a single accounting workflow.
/// </para>
/// </remarks>
public sealed class DatedExchangeRateProviderAdapter : IExchangeRateProvider
{
    /// <summary>
    /// The underlying dated provider that resolves the actual rate.
    /// </summary>
    private readonly IDatedExchangeRateProvider _inner;

    /// <summary>
    /// The fixed valuation date supplied to <see cref="_inner" /> on every lookup.
    /// </summary>
    private readonly DateOnly _date;

    /// <summary>
    /// The fixed lookup options supplied to <see cref="_inner" /> on every lookup.
    /// </summary>
    private readonly ExchangeRateLookupOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatedExchangeRateProviderAdapter" /> class.
    /// </summary>
    /// <param name="inner">The underlying dated provider to delegate to.</param>
    /// <param name="date">The valuation date pinned to every lookup.</param>
    /// <param name="options">The lookup options pinned to every lookup.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="options" /> contains an undefined enum value or a negative tolerance.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="options" /> specifies <see cref="ExchangeRateDateResolution.Exact" /> with a non-zero
    /// tolerance.
    /// </exception>
    public DatedExchangeRateProviderAdapter(
        IDatedExchangeRateProvider inner,
        DateOnly date,
        ExchangeRateLookupOptions options)
    {
        ThrowHelper.ThrowIfNull(inner);
        options.Validate();

        _inner = inner;
        _date = date;
        _options = options;
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode) =>
        _inner.GetRate(fromIsoCode, toIsoCode, _date, _options).Rate.Rate;
}
