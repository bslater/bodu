// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeLiveTokenRetrievalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Performs a live, network-dependent validation that <see cref="XeScrapingAuthTokenProvider" /> can still retrieve a
/// usable authorization credential from the current XE.com bundle.
/// </summary>
/// <remarks>
/// This test issues real HTTP requests to <c>xe.com</c>, so it is tagged <c>Stress</c> to keep it out of the Smoke, BVT,
/// and Regression tiers, and is intended to be run on demand from a machine with outbound access to xe.com. Run it with
/// a name filter, for example:
/// <c>dotnet test Bodu.Financial.ExchangeRates.Xe/test/Bodu.Financial.ExchangeRates.Xe.Test.csproj --filter
/// "FullyQualifiedName~GetTokenAsync_AgainstLiveXe"</c>.
/// </remarks>
[TestClass]
public class XeLiveTokenRetrievalTests
{
    /// <summary>
    /// Verifies that scraping the live XE website yields a non-empty Basic credential that decodes to a
    /// <c>user:password</c> pair, confirming the multi-chunk discovery and extraction work against XE's current bundle.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Stress)]
    public async Task GetTokenAsync_AgainstLiveXe_ShouldReturnUsableBasicCredential()
    {
        XeExchangeRateOptions options = new();
        using HttpClient client = new();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);

        XeScrapingAuthTokenProvider provider = new(client, options);

        string token = await provider.GetTokenAsync(forceRefresh: false);

        Assert.IsFalse(string.IsNullOrEmpty(token), "no authorization token was scraped from the XE website");

        string credential = Encoding.ASCII.GetString(Convert.FromBase64String(token));
        Assert.IsTrue(credential.Contains(':', StringComparison.Ordinal), "the decoded credential is not in user:password form");

        // Surface the shape without printing the secret itself, so a run shows the scrape succeeded.
        Console.WriteLine($"Scraped XE credential: {credential.Length} chars, base64 token length {token.Length}.");
    }
}
