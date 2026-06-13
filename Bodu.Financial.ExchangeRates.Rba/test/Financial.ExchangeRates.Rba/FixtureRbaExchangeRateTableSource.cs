// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureRbaExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Binary;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// A test table source that parses the embedded sample workbook for every era, recording how many times it is invoked.
/// </summary>
internal sealed class FixtureRbaExchangeRateTableSource
    : IRbaExchangeRateTableSource
{
    /// <summary>
    /// The options used when parsing the workbook.
    /// </summary>
    private readonly RbaExchangeRateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRbaExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the workbook.</param>
    public FixtureRbaExchangeRateTableSource(RbaExchangeRateOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <returns>The invocation count.</returns>
    public int GetTableCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<RbaExchangeRateTable> GetTableAsync(RbaEra era, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = RbaFixtures.OpenStream(RbaFixtures.Sample);
        var workbook = Biff8WorkbookReader.Open(stream);
        return ValueTask.FromResult(RbaExchangeRateWorkbookParser.Parse(workbook, _options));
    }
}
