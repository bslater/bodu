// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MidpointRoundingStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MidpointRoundingStrategy" /> rounds according to its configured midpoint mode.
/// </summary>
[TestClass]
public class MidpointRoundingStrategyTests
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
