// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MidpointRoundingStrategyTests.ToEven.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MidpointRoundingStrategyTests
{
    /// <summary>
    /// Verifies that the shared <see cref="MidpointRoundingStrategy.ToEven" /> instance rounds midpoints to even.
    /// </summary>
    [TestMethod]
    public void ToEven_WhenRoundingMidpoint_ShouldRoundToEven()
    {
        Assert.AreEqual(1.22m, MidpointRoundingStrategy.ToEven.Round(1.225m, 2));
        Assert.AreEqual(MidpointRounding.ToEven, MidpointRoundingStrategy.ToEven.Mode);
    }
}
