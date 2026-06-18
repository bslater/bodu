// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Subtraction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that subtracting amounts in different currencies throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = new CalculatedMoney(1m, "USD") - new CalculatedMoney(1m, "EUR"));
    }
}
