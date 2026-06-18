// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{
    /// <summary>
    /// Verifies that the constructor rejects a malformed ISO code.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenIsoCodeShapeInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new CalculatedMoney(1m, "us"));
    }
}
