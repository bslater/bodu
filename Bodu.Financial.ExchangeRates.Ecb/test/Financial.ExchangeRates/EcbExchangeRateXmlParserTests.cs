// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="EcbExchangeRateXmlParser" /> decodes the ECB <c>eurofxref</c> XML layout correctly and
/// rejects malformed or empty feeds.
/// </summary>
[TestClass]
public partial class EcbExchangeRateXmlParserTests
{
    /// <summary>
    /// The ECB <c>eurofxref</c> namespace declarations shared by the inline fixtures.
    /// </summary>
    private const string Namespaces =
        "xmlns:gesmes=\"http://www.gesmes.org/xml/2002-08-01\" xmlns=\"http://www.ecb.int/vocabulary/2002-08-01/eurofxref\"";

    /// <summary>
    /// Parses the embedded sample feed with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static EcbExchangeRateTable ParseSample() =>
        EcbExchangeRateXmlParser.Parse(EcbFixtures.OpenStream(EcbFixtures.Sample), new EcbExchangeRateOptions());

    /// <summary>
    /// Parses inline XML with default options.
    /// </summary>
    /// <param name="xml">The XML to parse.</param>
    /// <returns>The parsed table.</returns>
    private static EcbExchangeRateTable Parse(string xml) =>
        EcbExchangeRateXmlParser.Parse(ToStream(xml), new EcbExchangeRateOptions());

    /// <summary>
    /// Materializes XML text as a UTF-8 stream.
    /// </summary>
    /// <param name="xml">The XML text.</param>
    /// <returns>A readable stream over the encoded XML.</returns>
    private static MemoryStream ToStream(string xml) =>
        new(Encoding.UTF8.GetBytes(xml), writable: false);
}
