// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.Round.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MonetaryContextTests
{

    /// <summary>
    /// Verifies that the rounding strategy honours the configured midpoint mode.
    /// </summary>
    [TestMethod]
    public void Round_WhenAwayFromZeroStrategy_ShouldRoundMidpointAway()
    {
        MonetaryContext context = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        Assert.AreEqual(1.23m, context.Round(1.225m, 2));
        Assert.AreEqual(1.22m, MonetaryContext.Default.Round(1.225m, 2));
    }
}
