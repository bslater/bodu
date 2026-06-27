// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AverageStrategyTests
{
    /// <summary>
    /// Verifies that the constructor rejects a blank provider label.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLabelIsBlank_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AverageStrategy("  ");
        });

        Assert.AreEqual("providerLabel", ex.ParamName);
    }
}
