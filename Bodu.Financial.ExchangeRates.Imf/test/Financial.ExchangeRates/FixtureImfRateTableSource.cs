// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureImfRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses the embedded April 2026 report for month 2026-04 and returns an empty table for
/// every other month, recording how many times it is invoked.
/// </summary>
internal sealed class FixtureImfRateTableSource
    : IImfRateTableSource
{
    /// <summary>The options used when parsing the report.</summary>
    private readonly ImfRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureImfRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the report.</param>
    public FixtureImfRateTableSource(ImfRateProviderOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetTableCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        if (month.Year == 2026 && month.Month == 4)
        {
            using MemoryStream stream = new(ImfFixtures.ReadBytes(ImfFixtures.Rep202604), writable: false);
            return ValueTask.FromResult(ImfReportParser.Parse(stream, _options));
        }

        return ValueTask.FromResult(new ImfRateTable(Array.Empty<ImfRateObservation>()));
    }
}
