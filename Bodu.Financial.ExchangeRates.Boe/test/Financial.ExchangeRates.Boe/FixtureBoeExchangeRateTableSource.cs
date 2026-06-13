// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureBoeExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// A test table source that parses an embedded IADB CSV fixture for every range, recording how many times it is invoked.
/// </summary>
internal sealed class FixtureBoeExchangeRateTableSource
    : IBoeExchangeRateTableSource
{
    /// <summary>
    /// The options used when parsing the response.
    /// </summary>
    private readonly BoeExchangeRateOptions _options;

    /// <summary>
    /// The embedded fixture file name to parse.
    /// </summary>
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureBoeExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the response.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public FixtureBoeExchangeRateTableSource(BoeExchangeRateOptions options, string fileName = BoeFixtures.Sample)
    {
        _options = options;
        _fileName = fileName;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <returns>The invocation count.</returns>
    public int GetTableCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<BoeExchangeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = BoeFixtures.OpenStream(_fileName);
        return ValueTask.FromResult(BoeExchangeRateCsvParser.Parse(stream, _options));
    }
}
