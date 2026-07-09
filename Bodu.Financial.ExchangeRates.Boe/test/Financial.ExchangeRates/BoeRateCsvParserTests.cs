// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateCsvParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="BoeRateCsvParser" /> decodes the IADB CSV layout correctly and rejects responses
/// without the expected header.
/// </summary>
[TestClass]
public partial class BoeRateCsvParserTests
{
    /// <summary>
    /// Parses the embedded sample response with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static BoeRateTable ParseSample() =>
        BoeRateCsvParser.Parse(BoeFixtures.OpenStream(BoeFixtures.Sample), new BoeRateProviderOptions());
}
