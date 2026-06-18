// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.ThrowIfMoneyBagRoundingPolicyUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialThrowHelperTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="MoneyBagConversionRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfMoneyBagRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfMoneyBagRoundingPolicyUndefined((MoneyBagConversionRoundingPolicy)99));
}
