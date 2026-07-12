// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="ImfReportParser" /> maps the IMF Representative Exchange Rates report to the expected
/// USD-anchored observations or exceptions, asserting against the real embedded April 2026 fixture.
/// </summary>
[TestClass]
public sealed class ImfReportParserTests
{
    /// <summary>
    /// Parses the embedded April 2026 report and returns the single observation for a pair on a date.
    /// </summary>
    /// <param name="quoteIso">The quote-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <returns>The matching observation.</returns>
    private static ImfRateObservation Observation(string quoteIso, DateOnly date)
    {
        ImfRateProviderOptions options = new();
        ImfRateTable table = ImfReportParser.Parse(ImfFixtures.ReadText(ImfFixtures.Rep202604), options);

        return table.Observations.Single(o => o.CurrencyCode == quoteIso && o.Date == date);
    }

    /// <summary>
    /// Verifies that a no-suffix currency (units per US dollar) is parsed as its direct value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDirectQuote_ShouldReturnValueAsIs()
    {
        ImfRateObservation jpy = Observation("JPY", new DateOnly(2026, 4, 1));

        Assert.AreEqual(158.84m, jpy.Rate);
    }

    /// <summary>
    /// Verifies that a value carrying a thousands separator is parsed correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueHasThousandsSeparator_ShouldParseValue()
    {
        ImfRateObservation krw = Observation("KRW", new DateOnly(2026, 4, 1));

        Assert.AreEqual(1530.50m, krw.Rate);
    }

    /// <summary>
    /// Verifies that a <c>(1)</c>-marked currency (US dollars per unit) is inverted to units per US dollar.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUsdPerUnitQuote_ShouldReturnReciprocal()
    {
        ImfRateObservation eur = Observation("EUR", new DateOnly(2026, 4, 1));

        Assert.AreEqual(1m / 1.1605m, eur.Rate);
    }

    /// <summary>
    /// Verifies that a missing cell (<c>NA</c>) yields no observation while a later dated cell for the same currency
    /// resolves, inverted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCellMissing_ShouldSkipDateButResolveOthers()
    {
        ImfRateProviderOptions options = new();
        ImfRateTable table = ImfReportParser.Parse(ImfFixtures.ReadText(ImfFixtures.Rep202604), options);

        bool hasApril1 = table.Observations.Any(o => o.CurrencyCode == "AUD" && o.Date == new DateOnly(2026, 4, 1));
        ImfRateObservation april2 = table.Observations.Single(o => o.CurrencyCode == "AUD" && o.Date == new DateOnly(2026, 4, 2));

        Assert.IsFalse(hasApril1, "AUD 2026-04-01 is NA and should be absent");
        Assert.AreEqual(1m / 0.6879m, april2.Rate);
    }

    /// <summary>
    /// Verifies that a date from the report's second (continued) block resolves, confirming multi-block header handling.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDateInSecondBlock_ShouldResolve()
    {
        ImfRateObservation jpy = Observation("JPY", new DateOnly(2026, 4, 16));

        Assert.AreEqual(158.76m, jpy.Rate);
    }

    /// <summary>
    /// Verifies that the US dollar anchor row is skipped and not emitted as a USD/USD observation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnchorRow_ShouldBeSkipped()
    {
        ImfRateProviderOptions options = new();
        ImfRateTable table = ImfReportParser.Parse(ImfFixtures.ReadText(ImfFixtures.Rep202604), options);

        Assert.IsFalse(table.Observations.Any(o => o.CurrencyCode == "USD"));
    }

    /// <summary>
    /// Verifies that the parsed table exposes each distinct quote currency as a USD-anchored series.
    /// </summary>
    [TestMethod]
    public void Parse_WhenReportParsed_ShouldExposeUsdAnchoredSeries()
    {
        ImfRateProviderOptions options = new();
        ImfRateTable table = ImfReportParser.Parse(ImfFixtures.ReadText(ImfFixtures.Rep202604), options);

        ImfSeriesInfo jpy = table.GetSeriesInfo().Single(s => s.QuoteIsoCode == "JPY");

        Assert.AreEqual(CurrencyCode.USD, jpy.Pair.From);
        Assert.AreEqual(CurrencyCode.JPY, jpy.Pair.To);
    }

    /// <summary>
    /// Verifies that content lacking any recognizable header row is translated into an
    /// <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoHeader_ShouldThrowExchangeRateFormatException()
    {
        ImfRateProviderOptions options = new();
        string content = Encoding.UTF8.GetString(ImfFixtures.ReadBytes(ImfFixtures.ErrorNoTsv));

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = ImfReportParser.Parse(content, options);
        });
    }
}
