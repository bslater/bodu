// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateTableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="EcbRateTable" />, the normalized form of a parsed ECB feed.
/// </summary>
[TestClass]
public sealed partial class EcbRateTableTests
{
    /// <summary>
    /// Counts the rates that are dated earlier than a rate already yielded for the same pair: each such rate is an
    /// insertion ahead of the days its series already holds, rather than an append.
    /// </summary>
    /// <param name="rates">The rates in the order a load ingests them.</param>
    /// <returns>The number of rates out of date order within their pair.</returns>
    private static int CountOutOfOrder(IEnumerable<ExchangeRate> rates)
    {
        Dictionary<CurrencyPair, DateOnly> latest = new();
        int outOfOrder = 0;
        foreach (ExchangeRate rate in rates)
        {
            if (latest.TryGetValue(rate.Pair, out DateOnly seen) && rate.Date < seen)
                outOfOrder++;
            else
                latest[rate.Pair] = rate.Date;
        }

        return outOfOrder;
    }

    /// <summary>
    /// Builds a feed document listing the given number of consecutive days, newest first, each quoting two currencies.
    /// </summary>
    /// <param name="days">The number of days.</param>
    /// <returns>The feed XML.</returns>
    private static string NewestFirstFeed(int days)
    {
        System.Text.StringBuilder xml = new();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Append("<gesmes:Envelope xmlns:gesmes=\"http://www.gesmes.org/xml/2002-08-01\" xmlns=\"http://www.ecb.int/vocabulary/2002-08-01/eurofxref\"><Cube>");
        DateOnly first = new(2020, 1, 1);
        for (int day = days - 1; day >= 0; day--)
        {
            xml.Append("<Cube time=\"").Append(first.AddDays(day).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)).Append("\">");
            xml.Append("<Cube currency=\"USD\" rate=\"1.1\"/><Cube currency=\"JPY\" rate=\"130.5\"/></Cube>");
        }

        xml.Append("</Cube></gesmes:Envelope>");
        return xml.ToString();
    }
}
