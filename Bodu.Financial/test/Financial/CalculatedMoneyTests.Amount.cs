// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Amount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney" /> carries high-precision <see cref="decimal" /> rather than an exact
    /// rational: one third stored and read back equals the 28-29 digit <see cref="decimal" /> quotient, not the exact
    /// mathematical value (which would require the <c>Fraction</c>-based exact APIs).
    /// </summary>
    [TestMethod]
    public void Amount_WhenStoringOneThird_ShouldBeDecimalPrecisionNotExactRational()
    {
        CalculatedMoney third = new CalculatedMoney(1m, "USD") / 3m;

        Assert.AreEqual(1m / 3m, third.Amount);
        Assert.AreNotEqual(1m, third.Amount * 3m);
    }
}
