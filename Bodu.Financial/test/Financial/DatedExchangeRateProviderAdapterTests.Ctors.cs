// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderAdapterTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class DatedExchangeRateProviderAdapterTests
{

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> inner provider.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInnerIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new DatedExchangeRateProviderAdapter(null!, s_d1, ExchangeRateLookupOptions.Exact);
            },
            "inner");
    }

    /// <summary>
    /// Verifies that the constructor rejects invalid options.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsInvalid_ShouldThrowArgumentException()
    {
        FixedDatedExchangeRateProvider inner = new([]);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ExchangeRateLookupOptions bad = new(ExchangeRateDateResolution.Exact, 1);
            _ = new DatedExchangeRateProviderAdapter(inner, s_d1, bad);
        });
    }
}
