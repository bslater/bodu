// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureBoeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses an embedded IADB CSV fixture for every range, recording how many times it is
/// invoked.
/// </summary>
internal sealed class FixtureBoeRateTableSource
    : IBoeRateTableSource
{
    /// <summary>The options used when parsing the response.</summary>
    private readonly BoeRateProviderOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureBoeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the response.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public FixtureBoeRateTableSource(BoeRateProviderOptions options, string fileName = BoeFixtures.Sample)
    {
        _options = options;
        _fileName = fileName;
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
    public ValueTask<BoeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = BoeFixtures.OpenStream(_fileName);
        BoeRateTable table = BoeRateCsvParser.Parse(stream, _options);
        if (PublishedThrough is { } published)
            table = new BoeRateTable(table.Observations.Where(observation => observation.Date <= published).ToList(), table.Series);

        return ValueTask.FromResult(table);
    }
}
