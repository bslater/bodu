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

    /// <inheritdoc />
    public ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        if (month.Year == 2026 && month.Month == 4)
        {
            using MemoryStream stream = new(ImfFixtures.ReadBytes(ImfFixtures.Rep202604), writable: false);
            ImfRateTable table = ImfReportParser.Parse(stream, _options);
            if (PublishedThrough is { } published)
                table = new ImfRateTable(table.Observations.Where(observation => observation.Date <= published).ToList());

            return ValueTask.FromResult(table);
        }

        return ValueTask.FromResult(new ImfRateTable(Array.Empty<ImfRateObservation>()));
    }
}
