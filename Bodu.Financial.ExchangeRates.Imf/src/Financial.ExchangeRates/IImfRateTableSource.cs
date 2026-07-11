// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IImfRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the parsed rate table for a month of the IMF Representative Exchange Rates report, abstracting over how the
/// underlying report file is obtained and decoded.
/// </summary>
/// <remarks>
/// The shipped implementation downloads and parses the monthly tab-separated report. The abstraction leaves room for a
/// test source to produce the same normalized table without a network request.
/// </remarks>
internal interface IImfRateTableSource
{
    /// <summary>
    /// Obtains and parses the rate table for the specified report month.
    /// </summary>
    /// <param name="month">The report month to obtain.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the parsed <see cref="ImfRateTable" />.</returns>
    ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default);
}
