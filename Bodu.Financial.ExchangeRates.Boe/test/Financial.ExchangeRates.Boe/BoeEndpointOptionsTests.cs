// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeEndpointOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies the connection configuration carried by <see cref="BoeEndpointOptions" />.
/// </summary>
[TestClass]
public class BoeEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the defaults target the Bank of England IADB path with a working timeout and user-agent.
    /// </summary>
    [TestMethod]
    public void Defaults_ShouldTargetIadb()
    {
        BoeEndpointOptions endpoint = new();

        Assert.AreEqual(new Uri("https://www.bankofengland.co.uk/boeapps/database/"), endpoint.BaseUrl);
        Assert.AreEqual("_iadb-fromshowcolumns.asp", endpoint.QueryPath);
        Assert.AreEqual(TimeSpan.FromSeconds(30), endpoint.HttpTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.UserAgent));
    }

    /// <summary>
    /// Verifies that a request URL embeds the series codes and the inclusive date range.
    /// </summary>
    [TestMethod]
    public void BuildRequestUrl_ShouldEmbedSeriesAndDates()
    {
        BoeEndpointOptions endpoint = new();

        Uri url = endpoint.BuildRequestUrl(["XUDLUSS", "XUDLERS"], new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        string text = url.ToString();
        Assert.IsTrue(text.Contains("_iadb-fromshowcolumns.asp", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("XUDLUSS", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("XUDLERS", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("01%2FJan%2F2023", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("31%2FJan%2F2023", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that building a URL with <see langword="null" /> series codes throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void BuildRequestUrl_WhenSeriesCodesNull_ShouldThrowArgumentNullException()
    {
        BoeEndpointOptions endpoint = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = endpoint.BuildRequestUrl(null!, new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });
    }

    /// <summary>
    /// Verifies that the options reject a missing base URL through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBaseUrlIsNull_ShouldThrowArgumentException()
    {
        BoeEndpointOptions endpoint = new() { BaseUrl = null! };

        _ = Assert.ThrowsExactly<ArgumentException>(() => endpoint.Validate());
    }

    /// <summary>
    /// Verifies that the options reject an empty query path through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenQueryPathIsBlank_ShouldThrowArgumentException()
    {
        BoeEndpointOptions endpoint = new() { QueryPath = "   " };

        _ = Assert.ThrowsExactly<ArgumentException>(() => endpoint.Validate());
    }

    /// <summary>
    /// Verifies that the provider options surface a missing endpoint base URL through their aggregate validation.
    /// </summary>
    [TestMethod]
    public void ProviderOptionsValidate_WhenEndpointBaseUrlIsNull_ShouldThrowArgumentException()
    {
        BoeExchangeRateOptions options = new();
        options.Endpoint.BaseUrl = null!;

        _ = Assert.ThrowsExactly<ArgumentException>(() => options.Validate());
    }

    /// <summary>
    /// Verifies that the endpoint defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        BoeEndpointOptions endpoint = new();

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive endpoint HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        BoeEndpointOptions endpoint = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
