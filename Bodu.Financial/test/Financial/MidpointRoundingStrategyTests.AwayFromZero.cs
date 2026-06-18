// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MidpointRoundingStrategyTests.AwayFromZero.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MidpointRoundingStrategyTests
{

    /// <summary>
    /// Verifies that the shared <see cref="MidpointRoundingStrategy.AwayFromZero" /> instance rounds midpoints away
    /// from zero.
    /// </summary>
    [TestMethod]
    public void AwayFromZero_WhenRoundingMidpoint_ShouldRoundAway()
    {
        Assert.AreEqual(1.23m, MidpointRoundingStrategy.AwayFromZero.Round(1.225m, 2));
        Assert.AreEqual(-1.23m, MidpointRoundingStrategy.AwayFromZero.Round(-1.225m, 2));
    }
}
