// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureRbaRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses an embedded workbook fixture for every era, recording how many times it is invoked.
/// </summary>
internal sealed class FixtureRbaRateTableSource
    : IRbaRateTableSource
{
    /// <summary>The options used when parsing the workbook.</summary>
    private readonly RbaRateProviderOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRbaRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the workbook.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public FixtureRbaRateTableSource(RbaRateProviderOptions options, string fileName = RbaFixtures.Sample)
    {
        _options = options;
        _fileName = fileName;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetTableCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<RbaRateTable> GetTableAsync(RbaEra era, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = RbaFixtures.OpenStream(_fileName);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return ValueTask.FromResult(RbaRateWorkbookParser.Parse(workbook, _options));
    }
}
