// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeRateProviderTests.Constructors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class XeRateProviderTests
{
    /// <summary>
    /// Verifies that the public options-only constructor builds an owned <see cref="HttpClient" /> from valid options
    /// and disposes cleanly without contacting the network.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGivenValidOptions_ShouldConstructAndDisposeOwnedClient()
    {
        using var provider = new XeRateProvider(new XeRateProviderOptions());

        Assert.IsNotNull(provider);
    }

    /// <summary>
    /// Verifies that the public options-only constructor throws <see cref="ArgumentNullException" /> when the supplied
    /// options are <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XeRateProvider((XeRateProviderOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the public options-only constructor throws <see cref="ArgumentException" /> when the supplied
    /// options fail validation.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsInvalid_ShouldThrowArgumentException()
    {
        XeRateProviderOptions options = new() { ChartingRatesPath = string.Empty };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XeRateProvider(options);
        });
    }
}
