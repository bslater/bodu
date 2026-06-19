// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.NamedMultiplyAndDivide.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that the named <see cref="CalculatedMoney.Multiply(decimal)" /> and
    /// <see cref="CalculatedMoney.Divide(decimal)" /> methods preserve full precision and match the operators.
    /// </summary>
    [TestMethod]
    public void NamedMultiplyAndDivide_WhenInvoked_ShouldPreserveFullPrecision()
    {
        var value = new CalculatedMoney(1m, "USD");

        Assert.AreEqual(value * 3m, value.Multiply(3m));
        Assert.AreEqual(value / 3m, value.Divide(3m));
        Assert.AreEqual(1m / 3m, value.Divide(3m).Amount);
    }
}
