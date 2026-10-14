// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptionsTests.BuildRequestUrl.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that a request URL resolves the query path against the base address and embeds the series codes and
    /// the inclusive date range.
    /// </summary>
    [TestMethod]
    public void BuildRequestUrl_ShouldEmbedSeriesAndDates()
    {
        BoeRateProviderOptions options = new();

        Uri url = options.BuildRequestUrl(["XUDLUSS", "XUDLERS"], new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        string text = url.ToString();
        Assert.AreEqual("https://www.bankofengland.co.uk/boeapps/database/_iadb-fromshowcolumns.asp", url.GetLeftPart(UriPartial.Path));
        Assert.IsTrue(text.Contains("XUDLUSS", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("XUDLERS", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("01%2FJan%2F2023", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("31%2FJan%2F2023", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that multiple series codes are joined with a literal comma separator (each code escaped
    /// individually) rather than escaping the joined string, which would percent-encode the separators.
    /// </summary>
    [TestMethod]
    public void BuildRequestUrl_WhenMultipleSeriesCodes_ShouldJoinWithLiteralComma()
    {
        BoeRateProviderOptions options = new();

        Uri url = options.BuildRequestUrl(["XUDLUSS", "XUDLERS"], new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        string text = url.ToString();
        Assert.IsTrue(
            text.Contains("SeriesCodes=XUDLUSS,XUDLERS", StringComparison.Ordinal),
            "series codes are joined with a literal comma");
        Assert.IsFalse(
            text.Contains("%2C", StringComparison.OrdinalIgnoreCase),
            "the list separator comma is not percent-encoded");
    }

    /// <summary>
    /// Verifies that building a URL with <see langword="null" /> series codes throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void BuildRequestUrl_WhenSeriesCodesNull_ShouldThrowArgumentNullException()
    {
        BoeRateProviderOptions options = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = options.BuildRequestUrl(null!, new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });
    }
}
