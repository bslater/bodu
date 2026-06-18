// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney.IsZero" /> and <see cref="CalculatedMoney.Sign" /> report the amount's
    /// zero-ness and sign.
    /// </summary>
    [TestMethod]
    public void Properties_WhenInspectingAmount_ShouldReportZeroAndSign()
    {
        Assert.IsTrue(new CalculatedMoney(0m, "USD").IsZero);
        Assert.IsFalse(new CalculatedMoney(1m, "USD").IsZero);

        Assert.AreEqual(1, new CalculatedMoney(2m, "USD").Sign);
        Assert.AreEqual(-1, new CalculatedMoney(-2m, "USD").Sign);
        Assert.AreEqual(0, new CalculatedMoney(0m, "USD").Sign);
    }
}
