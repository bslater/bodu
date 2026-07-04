// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowingBoeExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that throws a supplied exception from every fetch, used to observe the provider's
/// failure-logging and rethrow behavior.
/// </summary>
internal sealed class ThrowingBoeExchangeRateTableSource
    : IBoeExchangeRateTableSource
{
    /// <summary>The exception thrown by every fetch.</summary>
    private readonly Exception _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowingBoeExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="exception">The exception to throw from every fetch.</param>
    public ThrowingBoeExchangeRateTableSource(Exception exception)
    {
        _exception = exception;
    }

    /// <inheritdoc />
    public ValueTask<BoeExchangeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        throw _exception;
}
