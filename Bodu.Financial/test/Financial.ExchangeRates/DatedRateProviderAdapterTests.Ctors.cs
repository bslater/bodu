// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderAdapterTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class DatedRateProviderAdapterTests
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
                _ = new DatedRateProviderAdapter(null!, s_d1, RateLookupOptions.Exact);
            },
            "inner");
    }

    /// <summary>
    /// Verifies that the constructor rejects invalid options.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsInvalid_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider inner = new([]);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            RateLookupOptions bad = new(RateDateResolution.Exact, 1);
            _ = new DatedRateProviderAdapter(inner, s_d1, bad);
        });
    }
}
