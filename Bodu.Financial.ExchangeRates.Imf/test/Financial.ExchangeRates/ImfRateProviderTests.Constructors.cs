// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.Constructors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that the client the provider builds and owns applies the configured
    /// <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" />.
    /// </summary>
    [TestMethod]
    public void CreateOwnedClient_WhenMaxResponseContentBufferSizeSet_ShouldApplyItToTheClient()
    {
        var options = new ImfRateProviderOptions { EnableDiskCache = false, MaxResponseContentBufferSize = 1_048_576 };

        using HttpClient client = ImfRateProvider.CreateOwnedClient(options);

        Assert.AreEqual(1_048_576, client.MaxResponseContentBufferSize);
    }
}
