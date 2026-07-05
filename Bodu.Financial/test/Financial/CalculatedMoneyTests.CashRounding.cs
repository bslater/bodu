// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.CashRounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies that <see cref="CalculatedMoney.RoundToMoney(MonetaryContext)" /> applies the context's cash-rounding
/// policy at settlement.
/// </summary>
public partial class CalculatedMoneyTests
{
    /// <summary>
    /// Verifies that enabling <see cref="CashRoundingPolicy.CurrencyCashIncrement" /> on the context snaps the settled
    /// amount to the currency's physical cash increment (CHF: 0.05), rather than leaving the policy inert.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundToMoney_WhenCashRoundingPolicyEnabled_ShouldSnapToCashIncrement()
    {
        MonetaryContext context = MonetaryContext.Default with { CashRounding = CashRoundingPolicy.CurrencyCashIncrement };

        Money settled = new CalculatedMoney(1.02m, CurrencyCode.CHF).RoundToMoney(context);

        Assert.AreEqual(1.00m, settled.Amount,
            "The cash-rounding policy must snap the settled amount to the nearest 0.05 cash increment.");
    }

    /// <summary>
    /// Verifies that the default cash-rounding policy (<see cref="CashRoundingPolicy.None" />) leaves the settled
    /// amount at minor-unit precision, so enabling the policy is what changes the result.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundToMoney_WhenCashRoundingPolicyDefault_ShouldNotSnap()
    {
        Money settled = new CalculatedMoney(1.02m, CurrencyCode.CHF).RoundToMoney();

        Assert.AreEqual(1.02m, settled.Amount);
    }
}
