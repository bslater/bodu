// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateXmlParserTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateXmlParserTests
{
    /// <summary>
    /// The most a feed may cost to parse, in bytes allocated per byte of feed. Loading the feed into a document tree
    /// before reading it cost about 8.8; reading it forward costs about 3.9.
    /// </summary>
    private const int MaxAllocatedBytesPerFeedByte = 6;

    /// <summary>
    /// Verifies that a long feed is read forward, without first being loaded into a document tree of every element in
    /// it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFeedIsLong_ShouldReadItWithoutLoadingADocument()
    {
        byte[] feed = Encoding.UTF8.GetBytes(LongFeed(days: 2000, currencies: 30));
        EcbRateProviderOptions options = new();
        using (MemoryStream warmUp = new(Encoding.UTF8.GetBytes(LongFeed(days: 2, currencies: 2)), writable: false))
            _ = EcbRateXmlParser.Parse(warmUp, options);

        using MemoryStream stream = new(feed, writable: false);
        long before = GC.GetAllocatedBytesForCurrentThread();
        EcbRateTable table = EcbRateXmlParser.Parse(stream, options);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.HasCount(60_000, table.Observations);
        Assert.IsLessThan(
            (long)feed.Length * MaxAllocatedBytesPerFeedByte,
            allocated,
            $"Parsing a {feed.Length}-byte feed allocated {allocated} bytes.");
    }

    /// <summary>
    /// Builds a feed document of the given number of days, newest first, each quoting the given number of currencies.
    /// </summary>
    /// <param name="days">The number of days.</param>
    /// <param name="currencies">The number of currencies quoted each day.</param>
    /// <returns>The feed XML.</returns>
    private static string LongFeed(int days, int currencies)
    {
        string[] codes = ["USD", "JPY", "GBP", "CHF", "AUD", "CAD", "NZD", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BGN", "TRY", "ISK", "ZAR", "BRL", "CNY", "HKD", "IDR", "ILS", "INR", "KRW", "MXN", "MYR", "PHP", "SGD", "THB"];
        StringBuilder xml = new();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Append("<gesmes:Envelope xmlns:gesmes=\"http://www.gesmes.org/xml/2002-08-01\" xmlns=\"http://www.ecb.int/vocabulary/2002-08-01/eurofxref\"><Cube>");
        DateOnly first = new(2000, 1, 3);
        for (var day = days - 1; day >= 0; day--)
        {
            xml.Append("<Cube time=\"").Append(first.AddDays(day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\">");
            for (var c = 0; c < currencies; c++)
                xml.Append("<Cube currency=\"").Append(codes[c]).Append("\" rate=\"1.").Append((day * 7 + c).ToString(CultureInfo.InvariantCulture)).Append("\"/>");

            xml.Append("</Cube>");
        }

        xml.Append("</Cube></gesmes:Envelope>");
        return xml.ToString();
    }
}
