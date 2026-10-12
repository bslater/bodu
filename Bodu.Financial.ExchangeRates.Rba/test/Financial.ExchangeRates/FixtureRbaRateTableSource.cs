// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureRbaRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses each era's synthetic workbook, served by <see cref="RbaFixtures" />, recording how
/// many times it is invoked.
/// </summary>
internal sealed class FixtureRbaRateTableSource
    : IRbaRateTableSource
{
    /// <summary>The options used when parsing the workbook.</summary>
    private readonly RbaRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRbaRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the workbook.</param>
    public FixtureRbaRateTableSource(RbaRateProviderOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetTableCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<RbaRateTable> GetTableAsync(RbaEraWorkbook era, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = RbaFixtures.OpenStream(era.FileName);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return ValueTask.FromResult(RbaRateWorkbookParser.Parse(workbook, _options));
    }
}
