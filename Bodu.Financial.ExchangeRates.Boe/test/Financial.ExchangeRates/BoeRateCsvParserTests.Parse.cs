// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateCsvParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateCsvParserTests
{
    /// <summary>
    /// Verifies that parsing the sample response yields one observation per present rate cell.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleResponse_ShouldYieldOneObservationPerCell()
    {
        BoeRateTable table = ParseSample();

        Assert.HasCount(6, table.Observations);
    }

    /// <summary>
    /// Verifies that a parsed cell carries the daily spot rate quoted against the pound on its date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleResponse_ShouldReadDateCurrencyAndRate()
    {
        BoeRateTable table = ParseSample();

        BoeRateObservation usd = table.Observations
            .Single(o => o.Date == new DateOnly(2023, 1, 3) && o.CurrencyCode == "USD");

        Assert.AreEqual(1.2065m, usd.Rate);
    }

    /// <summary>
    /// Verifies that an empty rate cell is skipped while the rest of the row is read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCellEmpty_ShouldSkipCell()
    {
        string csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLJYS\"\n" +
            "\"03 Jan 2023\",\"\",\"159.10\"\n";

        BoeRateTable table = BoeRateCsvParser.Parse(csv, new BoeRateProviderOptions());

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a column whose series code is not configured is ignored.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColumnNotConfigured_ShouldIgnoreColumn()
    {
        string csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLZZZ\"\n" +
            "\"03 Jan 2023\",\"1.2065\",\"9.9999\"\n";

        BoeRateTable table = BoeRateCsvParser.Parse(csv, new BoeRateProviderOptions());

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("USD", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a response lacking the <c>DATE</c> header is reported as a format failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderMissing_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = BoeRateCsvParser.Parse("<html><body>Service unavailable</body></html>", new BoeRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that parsing a response's bytes yields the observations parsing its text yields.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenBytes_ShouldYieldWhatParsingTheTextYields()
    {
        BoeRateProviderOptions options = new();
        byte[] bytes = BoeFixtures.ReadBytes(BoeFixtures.Sample);

        BoeRateTable table = BoeRateCsvParser.Parse((ReadOnlySpan<byte>)bytes, options);

        CollectionAssert.AreEqual(BoeRateCsvParser.Parse(BoeFixtures.ReadText(BoeFixtures.Sample), options).Observations.ToList(), table.Observations.ToList());
    }

    /// <summary>
    /// Verifies that a UTF-8 byte-order mark leading a response's bytes is not read as part of its header.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBytesStartWithByteOrderMark_ShouldIgnoreIt()
    {
        BoeRateProviderOptions options = new();
        byte[] bytes = [.. Encoding.UTF8.GetPreamble(), .. BoeFixtures.ReadBytes(BoeFixtures.Sample)];

        BoeRateTable table = BoeRateCsvParser.Parse((ReadOnlySpan<byte>)bytes, options);

        CollectionAssert.AreEqual(ParseSample().Observations.ToList(), table.Observations.ToList());
    }

    /// <summary>
    /// Verifies that a response's bytes in UTF-16, marked by its byte-order mark, read as they do through a stream.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBytesAreUtf16_ShouldReadThemAsAStreamReaderDecodesThem()
    {
        BoeRateProviderOptions options = new();
        byte[] bytes = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(BoeFixtures.ReadText(BoeFixtures.Sample))];

        BoeRateTable table = BoeRateCsvParser.Parse((ReadOnlySpan<byte>)bytes, options);

        CollectionAssert.AreEqual(ParseSample().Observations.ToList(), table.Observations.ToList());
    }

    /// <summary>
    /// Verifies that bytes lacking the <c>DATE</c> header are reported as a format failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBytesLackHeader_ShouldThrowExchangeRateFormatException()
    {
        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = BoeRateCsvParser.Parse("<html><body>Service unavailable</body></html>"u8, new BoeRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that parsing bytes without options throws <see cref="ArgumentNullException" /> naming the options.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenBytesWithoutOptions_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoeRateCsvParser.Parse(BoeFixtures.ReadBytes(BoeFixtures.Sample).AsSpan(), null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
