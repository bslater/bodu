// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRbaExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Provides the parsed rate table for an RBA era, abstracting over how the underlying file is obtained and decoded.
/// </summary>
/// <remarks>
/// The shipped implementation downloads and parses the RBA <c>.xls</c> files. The abstraction leaves room for an
/// alternative source (for example, a CSV feed) to produce the same normalized table without changing the provider.
/// </remarks>
internal interface IRbaExchangeRateTableSource
{
    /// <summary>
    /// Obtains and parses the rate table for the specified era.
    /// </summary>
    /// <param name="era">The era to obtain.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the parsed <see cref="RbaExchangeRateTable" />.</returns>
    ValueTask<RbaExchangeRateTable> GetTableAsync(RbaEra era, CancellationToken cancellationToken = default);
}
