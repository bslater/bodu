// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProviderTests.Constructors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the construction surface of <see cref="FredRateProvider" />, in particular the overload where the
/// provider creates and owns its own <see cref="HttpClient" />.
/// </summary>
[TestClass]
public sealed partial class FredRateProviderTests
{
    /// <summary>
    /// Verifies that the options-only overload constructs a usable provider, exercising the path where the provider
    /// builds and owns its HTTP client. No request is issued.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenOptionsOnly_ShouldConstructProviderOwningItsClient()
    {
        using var provider = new FredRateProvider(new FredRateProviderOptions { ApiKey = "test-key" });

        Assert.IsInstanceOfType<IDatedRateProvider>(provider);
    }

    /// <summary>
    /// Verifies that the owned-client provider releases its client on disposal without faulting.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenProviderOwnsItsClient_ShouldNotThrow()
    {
        var provider = new FredRateProvider(new FredRateProviderOptions { ApiKey = "test-key" });

        provider.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Verifies that the options-only overload rejects <see langword="null" /> options.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FredRateProvider((FredRateProviderOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the options-only overload validates its options before creating the owned client, so a missing
    /// api key fails at construction rather than at the first request.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenApiKeyMissing_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new FredRateProvider(new FredRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that the caller-supplied-client overload rejects a <see langword="null" /> client.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHttpClientIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FredRateProvider((HttpClient)null!, new FredRateProviderOptions { ApiKey = "test-key" });
        });

        Assert.AreEqual("httpClient", ex.ParamName);
    }
}
