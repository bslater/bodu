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

    /// <summary>The labels of the eras requested so far, in request order, guarded by its own lock.</summary>
    private readonly List<string> _requestedEras = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRbaRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the workbook.</param>
    public FixtureRbaRateTableSource(RbaRateProviderOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets or sets the last date the simulated source has published, or <see langword="null" /> to serve every
    /// observation; an observation dated after it is withheld, as a source withholds a day it has not yet published.
    /// </summary>
    /// <value>The last published date, or <see langword="null" />.</value>
    public DateOnly? PublishedThrough { get; set; }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetTableCallCount { get; private set; }

    /// <summary>
    /// Gets a snapshot of the labels of the eras requested so far, in request order.
    /// </summary>
    /// <value>The requested era labels.</value>
    public IReadOnlyList<string> RequestedEras
    {
        get
        {
            lock (_requestedEras)
            {
                return [.. _requestedEras];
            }
        }
    }

    /// <inheritdoc />
    public ValueTask<RbaRateTable> GetTableAsync(RbaEraWorkbook era, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;
        lock (_requestedEras)
        {
            _requestedEras.Add(era.Label);
        }

        using MemoryStream stream = RbaFixtures.OpenStream(era.FileName);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        RbaRateTable table = RbaRateWorkbookParser.Parse(workbook, _options);
        if (PublishedThrough is { } published)
            table = new RbaRateTable(table.Series, table.Rows.Where(row => row.Date <= published).ToList());

        return ValueTask.FromResult(table);
    }
}
