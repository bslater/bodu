// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureEcbExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source that parses an embedded <c>eurofxref</c> fixture for every feed, recording how many times it is
/// invoked.
/// </summary>
internal sealed class FixtureEcbExchangeRateTableSource
    : IEcbExchangeRateTableSource
{
    /// <summary>The options used when parsing the feed.</summary>
    private readonly EcbExchangeRateOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureEcbExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the feed.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public FixtureEcbExchangeRateTableSource(EcbExchangeRateOptions options, string fileName = EcbFixtures.Sample)
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
    public ValueTask<EcbExchangeRateTable> GetTableAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default)
    {
        GetTableCallCount++;

        using MemoryStream stream = EcbFixtures.OpenStream(_fileName);
        return ValueTask.FromResult(EcbExchangeRateXmlParser.Parse(stream, _options));
    }
}
