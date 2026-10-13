// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureEcbRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses an embedded <c>eurofxref</c> fixture for every feed, recording how many times it is
/// invoked.
/// </summary>
internal sealed class FixtureEcbRateTableSource
    : IEcbRateTableSource
{
    /// <summary>The options used when parsing the feed.</summary>
    private readonly EcbRateProviderOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureEcbRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the feed.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public FixtureEcbRateTableSource(EcbRateProviderOptions options, string fileName = EcbFixtures.Sample)
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
    public ValueTask<EcbRateTable> GetTableAsync(EcbRateFeed feed, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = EcbFixtures.OpenStream(_fileName);
        EcbRateTable table = EcbRateXmlParser.Parse(stream, _options);
        if (PublishedThrough is { } published)
            table = new EcbRateTable(table.Observations.Where(observation => observation.Date <= published).ToList());

        return ValueTask.FromResult(table);
    }
}
