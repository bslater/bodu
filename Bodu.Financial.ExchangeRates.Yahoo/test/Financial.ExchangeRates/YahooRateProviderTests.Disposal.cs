// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderTests.Disposal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the <see cref="HttpClient" /> ownership and disposal contract of <see cref="YahooRateProvider" />:
/// the provider disposes only a client it created, never a caller-supplied one.
/// </summary>
public partial class YahooRateProviderTests
{
    /// <summary>
    /// Verifies that disposing a provider built from a caller-supplied client does not dispose that client.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenClientSupplied_ShouldNotDisposeSuppliedClient()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooRateProvider provider = new(client, new YahooRateProviderOptions());

        provider.Dispose();

        // The caller still owns the supplied client; disposing the provider must leave it usable.
        byte[] bytes = await client.GetByteArrayAsync(new Uri("https://query1.finance.yahoo.com/probe"));
        Assert.IsNotEmpty(bytes);
    }

    /// <summary>
    /// Verifies that a provider that owns its client can be constructed and disposed, and that disposal is idempotent.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenClientOwned_ShouldBeIdempotent()
    {
        YahooRateProvider provider = new(new YahooRateProviderOptions());

        provider.Dispose();
        provider.Dispose();
    }
}
