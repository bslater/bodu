// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateCsvParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies that <see cref="BoeExchangeRateCsvParser" /> decodes the IADB CSV layout correctly and rejects responses
/// without the expected header.
/// </summary>
[TestClass]
public partial class BoeExchangeRateCsvParserTests
{
    /// <summary>
    /// Parses the embedded sample response with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static BoeExchangeRateTable ParseSample() =>
        BoeExchangeRateCsvParser.Parse(BoeFixtures.OpenStream(BoeFixtures.Sample), new BoeExchangeRateOptions());
}
