// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBoeExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the parsed rate table for a Bank of England date range, abstracting over how the underlying data is
/// obtained and decoded.
/// </summary>
/// <remarks>
/// The shipped implementation queries the IADB CSV endpoint. The abstraction leaves room for an alternative source (for
/// example, a cached extract or an alternative BoE feed) to produce the same normalized table without changing the
/// provider.
/// </remarks>
internal interface IBoeExchangeRateTableSource
{
    /// <summary>
    /// Obtains and parses the rate table for the inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the parsed <see cref="BoeExchangeRateTable" />.</returns>
    ValueTask<BoeExchangeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
